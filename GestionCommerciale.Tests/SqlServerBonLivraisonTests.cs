using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace GestionCommerciale.Tests;

/// <summary>
/// S10b sur un VRAI SQL Server (base LocalDB jetable créée par Migrate(), supprimée ensuite), seulement avec GC_TESTS_SQLSERVER=1.
/// Une transaction seule NE ferme PAS la course sur le contrôle de quantité (isolation READ COMMITTED : deux sessions lisent le même
/// cumul avant qu'aucune n'insère). Ce test impose un entrelacement DÉTERMINISTE : la session A contrôle puis, juste avant d'enregistrer,
/// la session B tente la même création ; si le verrou de la commande fonctionne, B est bloquée jusqu'à la fin de A puis refusée ;
/// sans verrou, B terminerait pendant la fenêtre et la quantité commandée serait dépassée.
/// </summary>
[Trait("Category", "SqlServer")]
public class SqlServerBonLivraisonTests : IClassFixture<BaseJetableFixture>
{
    private readonly BaseJetableFixture _base;
    private readonly ITestOutputHelper _sortie;

    public SqlServerBonLivraisonTests(BaseJetableFixture baseJetable, ITestOutputHelper sortie) { _base = baseJetable; _sortie = sortie; }

    private async Task<(int CommandeId, int LigneId)> PreparerAsync(int quantite)
    {
        using var db = _base.NouveauContexte();
        var client = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Client course BL" };
        var article = new Article { Reference = "S10B-" + Guid.NewGuid().ToString("N")[..8], Libelle = "Article course", Quantite = 100, PrixVenteHT = 2.000m, TauxTVA = 19m };
        db.Clients.Add(client);
        db.Articles.Add(article);
        await db.SaveChangesAsync();
        var commande = new CommandeClient { NumeroCommande = "CC-S10B-" + Guid.NewGuid().ToString("N")[..5], ClientId = client.Id, Statut = StatutCommande.Confirmee };
        commande.LignesCommande.Add(new LigneCommandeClient { ArticleId = article.ID, Quantite = quantite, PrixUnitaire = 2.000m, TauxTVA = 19m });
        commande.RecalculerTotaux();
        db.CommandesClient.Add(commande);
        await db.SaveChangesAsync();
        return (commande.Id, commande.LignesCommande.Single().Id);
    }

    [SqlServerFact]
    public async Task S10b_a_deux_creations_simultanees_ne_depassent_jamais_la_quantite_commandee()
    {
        _base.AssurerBase();
        var (commandeId, ligneId) = await PreparerAsync(quantite: 10);

        var interception = new ActionAvantEnregistrement();
        using var sessionA = _base.NouveauContexte(interception);
        Task<(BonLivraison? Bl, string? Erreur)>? tacheB = null;
        var bTermineePendantLaFenetre = false;
        interception.Action = async () =>
        {
            tacheB = Task.Run(async () =>
            {
                using var sessionB = _base.NouveauContexte();
                return await new BonLivraisonService(sessionB, new StockService(sessionB)).CreerBrouillonAsync(
                    commandeId, DateTime.Today, null, new (int?, int)[] { (ligneId, 10) }, () => Task.FromResult("BL-S10B-B"));
            });
            // Fenêtre : avec un verrou correct, B ne peut PAS aboutir tant que A n'a pas terminé.
            bTermineePendantLaFenetre = await Task.WhenAny(tacheB, Task.Delay(2500)) == tacheB;
        };

        var a = await new BonLivraisonService(sessionA, new StockService(sessionA)).CreerBrouillonAsync(
            commandeId, DateTime.Today, null, new (int?, int)[] { (ligneId, 10) }, () => Task.FromResult("BL-S10B-A"));
        var b = await tacheB!;

        _sortie.WriteLine($"[S10b-a] B terminée pendant la fenêtre d'attente : {bTermineePendantLaFenetre} ; A : {(a.Bl != null ? "créé" : a.Erreur)} ; B : {(b.Bl != null ? "créé" : b.Erreur)}");
        Assert.False(bTermineePendantLaFenetre);                   // B était bloquée par le verrou de A
        Assert.Null(a.Erreur);
        Assert.NotNull(a.Bl);
        Assert.Null(b.Bl);
        Assert.Contains("reste disponible 0", b.Erreur);           // vue du résultat de A une fois le verrou libéré
        using var lecture = _base.NouveauContexte();
        var total = await lecture.LignesBonLivraison.Where(l => l.LigneCommandeClientId == ligneId && l.BonLivraison!.Statut != StatutBonLivraison.Annule).SumAsync(l => l.Quantite);
        Assert.Equal(10, total);                                    // jamais 20
    }

    [SqlServerFact]
    public async Task S10b_b_la_creation_puis_la_validation_d_un_bl_normal_fonctionnent_sur_sql_server()
    {
        _base.AssurerBase();
        var (commandeId, ligneId) = await PreparerAsync(quantite: 10);
        using var db = _base.NouveauContexte();
        var service = new BonLivraisonService(db, new StockService(db));

        var (bl, erreur) = await service.CreerBrouillonAsync(commandeId, DateTime.Today, null, new (int?, int)[] { (ligneId, 4) }, () => Task.FromResult("BL-S10B-N"));
        Assert.Null(erreur);
        var (valide, erreurValidation) = await service.ValiderAsync(bl!.ID);

        Assert.True(valide, erreurValidation);
        using var lecture = _base.NouveauContexte();
        Assert.Equal(4, (await lecture.LignesCommandeClient.AsNoTracking().SingleAsync(l => l.Id == ligneId)).QuantiteLivree);
    }

    [SqlServerFact]
    public async Task S10b_c_creations_et_validation_en_parallele_sur_la_meme_commande_sans_interblocage_ni_depassement()
    {
        _base.AssurerBase();
        for (var tour = 1; tour <= 3; tour++)                       // plusieurs tours : l'ordre d'arrivée des sessions varie
        {
            var (commandeId, ligneId) = await PreparerAsync(quantite: 10);
            int bl1;
            using (var db = _base.NouveauContexte())
            {
                var (brouillon, erreur) = await new BonLivraisonService(db, new StockService(db)).CreerBrouillonAsync(
                    commandeId, DateTime.Today, null, new (int?, int)[] { (ligneId, 6) }, () => Task.FromResult($"BL-S10C-{tour}-1"));
                Assert.Null(erreur);
                bl1 = brouillon!.ID;
            }

            // En parallèle : valider le BL de 6, et créer deux BL de 4 (6 + 4 + 4 = 14 > 10 : un seul des deux peut exister)
            Task<(bool, string?)> Valider() => Task.Run(async () =>
            {
                using var db = _base.NouveauContexte();
                return await new BonLivraisonService(db, new StockService(db)).ValiderAsync(bl1);
            });
            Task<(BonLivraison? Bl, string? Erreur)> Creer(string numero) => Task.Run(async () =>
            {
                using var db = _base.NouveauContexte();
                return await new BonLivraisonService(db, new StockService(db)).CreerBrouillonAsync(
                    commandeId, DateTime.Today, null, new (int?, int)[] { (ligneId, 4) }, () => Task.FromResult(numero));
            });

            var tv = Valider(); var t2 = Creer($"BL-S10C-{tour}-2"); var t3 = Creer($"BL-S10C-{tour}-3");
            await Task.WhenAll(tv, t2, t3).WaitAsync(TimeSpan.FromSeconds(90));    // un interblocage ferait échouer ici (délai dépassé ou victime de deadlock)

            var (validation, erreurValidation) = await tv;
            var (bl2, e2) = await t2;
            var (bl3, e3) = await t3;
            _sortie.WriteLine($"[S10b-c tour {tour}] validation : {(validation ? "ok" : erreurValidation)} ; BL2 : {(bl2 != null ? "créé" : e2)} ; BL3 : {(bl3 != null ? "créé" : e3)}");
            Assert.True(validation, erreurValidation);
            Assert.Equal(1, new[] { bl2, bl3 }.Count(b => b != null));               // exactement un des deux
            Assert.Contains("reste disponible 0", e2 ?? e3);

            using var lecture = _base.NouveauContexte();
            var total = await lecture.LignesBonLivraison.Where(l => l.LigneCommandeClientId == ligneId && l.BonLivraison!.Statut != StatutBonLivraison.Annule).SumAsync(l => l.Quantite);
            Assert.Equal(10, total);                                                  // jamais plus que la quantité commandée
            Assert.Equal(6, (await lecture.LignesCommandeClient.AsNoTracking().SingleAsync(l => l.Id == ligneId)).QuantiteLivree);   // seule la validation a livré
        }
    }
}
