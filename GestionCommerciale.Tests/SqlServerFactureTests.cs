using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit.Abstractions;

namespace GestionCommerciale.Tests;

/// <summary>
/// S10 sur un VRAI SQL Server (base LocalDB jetable créée par Migrate(), donc avec la migration S10Facturation ; supprimée ensuite),
/// seulement avec GC_TESTS_SQLSERVER=1. Prouve ce que SQLite ne peut pas prouver : colonnes de montants à 3 décimales relues sans
/// troncature (avant S10 : 1,005 relu 1,01), et l'index unique filtré IX_Factures_BonLivraisonId (une seule facture non annulée par BL).
/// </summary>
[Trait("Category", "SqlServer")]
public class SqlServerFactureTests : IClassFixture<BaseJetableFixture>
{
    private readonly BaseJetableFixture _base;
    private readonly ITestOutputHelper _sortie;

    public SqlServerFactureTests(BaseJetableFixture baseJetable, ITestOutputHelper sortie) { _base = baseJetable; _sortie = sortie; }

    /// <summary>Paramètres neutres (FODEC 1 %, TVA hors FODEC, timbre 1,000) + un article, une commande et un BL validé lié (3 x 0,335 à 20 %).</summary>
    private async Task<int> PreparerBlAsync(bool articleFodec = false)
    {
        using var db = _base.NouveauContexte();
        var p = await db.ParametresSociete.FirstOrDefaultAsync();
        if (p == null) { p = new ParametresSociete { RaisonSociale = "Société test" }; db.ParametresSociete.Add(p); }
        p.TauxFodec = 1m; p.BaseTvaInclutFodec = false; p.TimbreFiscal = 1.000m; p.RetenueActive = false;
        var client = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Client SQL" };
        var article = new Article { Reference = "S10-" + Guid.NewGuid().ToString("N")[..8], Libelle = "Decimal", Quantite = 10, PrixVenteHT = 9.999m, TauxTVA = 7m, Fodec = articleFodec };
        db.Clients.Add(client);
        db.Articles.Add(article);
        await db.SaveChangesAsync();
        var commande = new CommandeClient { NumeroCommande = "CC-S10-" + Guid.NewGuid().ToString("N")[..5], ClientId = client.Id, Statut = StatutCommande.Confirmee };
        commande.LignesCommande.Add(new LigneCommandeClient { ArticleId = article.ID, Quantite = 3, PrixUnitaire = 0.335m, TauxTVA = 20m });
        commande.RecalculerTotaux();
        db.CommandesClient.Add(commande);
        await db.SaveChangesAsync();
        var bl = new BonLivraison { Numero = "BL-S10-" + Guid.NewGuid().ToString("N")[..5], ClientId = client.Id, CommandeClientId = commande.Id, Statut = StatutBonLivraison.Valide };
        bl.Lignes.Add(new LigneBonLivraison { ArticleId = article.ID, Quantite = 3, PrixUnitaire = 0.335m, LigneCommandeClientId = commande.LignesCommande.Single().Id });
        db.BonsLivraison.Add(bl);
        await db.SaveChangesAsync();
        return bl.ID;
    }

    [SqlServerFact]
    public async Task S10a_les_montants_de_facture_a_3_decimales_sont_relus_sans_troncature()
    {
        _base.AssurerBase();
        var blId = await PreparerBlAsync(articleFodec: true);

        int factureId;
        using (var db = _base.NouveauContexte())
            factureId = (await new FactureService(db, null!).CreerDepuisBonLivraisonAsync(blId)).Id;

        using (var db = _base.NouveauContexte())
        {
            var f = await db.Factures.AsNoTracking().Include(x => x.Lignes).SingleAsync(x => x.Id == factureId);
            _sortie.WriteLine($"[S10a] relu : HT {f.TotalHt} ; FODEC {f.MontantFodec} ; TVA {f.TotalTva} ; TTC {f.TotalTtc} ; ligne {f.Lignes.Single().PrixUnitaireHt} / {f.Lignes.Single().TotalHt} ; règle {f.RegleCalcul}");
            // à la main : ligne 3 x 0,335 = 1,005 ; FODEC 1 % de 1,005 = 0,01005 -> 0,010 ; TVA 20 % de 1,005 = 0,201 ; TTC = 1,005 + 0,010 + 0,201 + timbre 1,000 = 2,216
            Assert.Equal(1.005m, f.TotalHt);                                // avant S10 : relu 1,01
            Assert.Equal(0.010m, f.MontantFodec);
            Assert.Equal(0.201m, f.TotalTva);                               // avant S10 : relu 0,20
            Assert.Equal(1.000m, f.TimbreFiscal);
            Assert.Equal(2.216m, f.TotalTtc);
            Assert.Equal(0.335m, f.Lignes.Single().PrixUnitaireHt);
            Assert.Equal(1.005m, f.Lignes.Single().TotalHt);
            Assert.True(f.Lignes.Single().SoumiseFodec);
            Assert.Equal(2, f.RegleCalcul);
        }
    }

    [SqlServerFact]
    public async Task S10b_l_index_unique_refuse_la_seconde_facture_non_annulee_d_un_meme_bl_avec_le_vrai_numero_d_erreur()
    {
        _base.AssurerBase();
        var blId = await PreparerBlAsync();

        // La session B contrôle puis, JUSTE AVANT son enregistrement, la session A crée la facture du même BL.
        var interception = new ActionAvantEnregistrement();
        using var sessionB = _base.NouveauContexte(interception);
        interception.Action = async () =>
        {
            using var sessionA = _base.NouveauContexte();
            await new FactureService(sessionA, null!).CreerDepuisBonLivraisonAsync(blId);
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new FactureService(sessionB, null!).CreerDepuisBonLivraisonAsync(blId));

        var sql = BaseJetableFixture.ExtraireSqlException(exception.InnerException!);
        _sortie.WriteLine($"[S10b] message : {exception.Message} ; SqlException n° {sql?.Number} : {sql?.Message}");
        Assert.Contains("déjà facturé", exception.Message);
        Assert.NotNull(sql);
        Assert.Contains(sql!.Number, new[] { 2601, 2627 });
        Assert.Contains("IX_Factures_BonLivraisonId", sql.Message);
        using var lecture = _base.NouveauContexte();
        Assert.Equal(1, await lecture.Factures.CountAsync(f => f.BonLivraisonId == blId));
    }

    [SqlServerFact]
    public async Task S10c_une_facture_annulee_ne_bloque_pas_une_nouvelle_facture_du_meme_bl()
    {
        _base.AssurerBase();
        var blId = await PreparerBlAsync();
        using (var db = _base.NouveauContexte())
        {
            var premiere = await new FactureService(db, null!).CreerDepuisBonLivraisonAsync(blId);
            Assert.True((await new FactureService(db, null!).AnnulerAsync(premiere.Id)).Item1);
        }

        using (var db = _base.NouveauContexte())
            await new FactureService(db, null!).CreerDepuisBonLivraisonAsync(blId);          // l'index filtré l'autorise

        using var lecture = _base.NouveauContexte();
        Assert.Equal(2, await lecture.Factures.CountAsync(f => f.BonLivraisonId == blId));
        Assert.Equal(1, await lecture.Factures.CountAsync(f => f.BonLivraisonId == blId && f.Statut != StatutFacture.Annulee));
    }

    [SqlServerFact]
    public async Task S10d_la_migration_marque_les_factures_existantes_regle_1_et_conserve_leurs_montants_inchanges()
    {
        _base.AssurerBase();                                                         // base migrée jusqu'à S10Facturation
        const string avantS10 = "20261118231853_UniciteCodeBarre";

        int articleId;
        using (var db = _base.NouveauContexte())
        {
            // Retour à l'état d'AVANT S10 (teste aussi la méthode Down), puis une facture « ancienne règle » validée, montants à 2 décimales.
            await db.GetService<IMigrator>().MigrateAsync(avantS10);
            var article = new Article { Reference = "S10D-" + Guid.NewGuid().ToString("N")[..8], Libelle = "Ancien", Quantite = 1 };
            db.Articles.Add(article);
            await db.SaveChangesAsync();
            articleId = article.ID;
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO Factures (Numero, [Date], ClientId, BonLivraisonId, Statut, RemiseGlobalePourcent, TotalHt, MontantFodec, TotalTva, TimbreFiscal, TotalTtc, " +
                "RetenueSourceApplicable, MontantRetenueSource, NetAPayer, MontantRegle, StatutPaiement, TauxRetenueSource) " +
                "VALUES (N'FAC-2026-00099', SYSDATETIME(), NULL, NULL, 1, 0, 1.01, 0.01, 0.20, 1.00, 2.22, 0, 0, 2.22, 0, 0, 0)");
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO LignesFacture (FactureId, ArticleId, Designation, Quantite, PrixUnitaireHt, RemisePourcent, TauxTva, TotalHt) " +
                "SELECT Id, {0}, N'Ancienne', 3, 0.34, 0, 20, 1.01 FROM Factures WHERE Numero = N'FAC-2026-00099'", articleId);
        }

        using (var db = _base.NouveauContexte())
            await db.Database.MigrateAsync();                                       // applique S10Facturation sur des données existantes

        using (var db = _base.NouveauContexte())
        {
            var f = await db.Factures.AsNoTracking().Include(x => x.Lignes).SingleAsync(x => x.Numero == "FAC-2026-00099");
            _sortie.WriteLine($"[S10d] après migration : règle {f.RegleCalcul} ; HT {f.TotalHt} ; FODEC {f.MontantFodec} ; TVA {f.TotalTva} ; TTC {f.TotalTtc} ; ligne soumise FODEC {f.Lignes.Single().SoumiseFodec}");
            Assert.Equal(1, f.RegleCalcul);                                          // règle d'avant S10 : jamais recalculée
            Assert.Equal((1.01m, 0.01m, 0.20m, 1.00m, 2.22m), (f.TotalHt, f.MontantFodec, f.TotalTva, f.TimbreFiscal, f.TotalTtc));   // valeurs conservées à l'identique
            Assert.Equal((0.34m, 1.01m), (f.Lignes.Single().PrixUnitaireHt, f.Lignes.Single().TotalHt));
            Assert.False(f.Lignes.Single().SoumiseFodec);
            Assert.False((await db.ParametresSociete.AsNoTracking().FirstOrDefaultAsync())?.BaseTvaInclutFodec ?? false);   // défaut : comportement actuel (exclu)
        }
    }
}
