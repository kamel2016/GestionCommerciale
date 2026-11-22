using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

/// <summary>
/// S10b (numérotation des BL) : un compteur en retard sur les BL existants (jamais initialisé : le BL-2026-00001 date d'avant la table de
/// séquences) ne doit plus faire échouer la création. CreerBrouillonAsync saute les numéros déjà pris (limite de 50 essais, message clair),
/// valide le compteur AVEC le BL (même transaction), n'enregistre rien en cas d'échec, et traduit un doublon de numéro détecté à
/// l'enregistrement (course) en message clair au lieu d'une exception. Sous SQLite, le compteur est simulé par une vraie ligne de
/// SequencesDocument écrite dans la transaction du service ; le vrai MERGE n'est prouvé que sur SQL Server (SqlServerBonLivraisonTests).
/// </summary>
public class BonLivraisonNumeroTests : ArticleTestBase
{
    private BonLivraisonService Service() => new(Db, new StockService(Db));

    private async Task<(int CommandeId, int LigneId)> CommandeAsync(int quantite = 10)
    {
        var client = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Client numéro" };
        var article = new Article { Reference = "NUM-" + Guid.NewGuid().ToString("N")[..6], Libelle = "Câble N", Quantite = 1000, PrixVenteHT = 2.000m, TauxTVA = 19m };
        Db.Clients.Add(client);
        Db.Articles.Add(article);
        await Db.SaveChangesAsync();
        var commande = new CommandeClient { NumeroCommande = "CC-NUM-" + Guid.NewGuid().ToString("N")[..5], ClientId = client.Id, Statut = StatutCommande.Confirmee };
        commande.LignesCommande.Add(new LigneCommandeClient { ArticleId = article.ID, Quantite = quantite, PrixUnitaire = 2.000m, TauxTVA = 19m });
        commande.RecalculerTotaux();
        Db.CommandesClient.Add(commande);
        await Db.SaveChangesAsync();
        var ligne = commande.LignesCommande.Single().Id;
        Db.ChangeTracker.Clear();
        return (commande.Id, ligne);
    }

    private async Task BlsExistantsAsync(int de, int a, int exercice = 2026)
    {
        for (var n = de; n <= a; n++)
            Db.BonsLivraison.Add(new BonLivraison { Numero = $"BL-{exercice}-{n:D5}", Statut = StatutBonLivraison.Annule });
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
    }

    private int _appels;

    /// <summary>Simule NumerotationService : incrémente une VRAIE ligne de SequencesDocument dans la transaction courante (persistée seulement si le BL l'est).</summary>
    private Func<Task<string>> GenerateurAvecCompteur(int exercice = 2026) => async () =>
    {
        _appels++;
        var seq = await Db.SequencesDocument.FirstOrDefaultAsync(s => s.TypeDocument == "BL" && s.Exercice == exercice);
        if (seq == null) { seq = new SequenceDocument { TypeDocument = "BL", Exercice = exercice }; Db.SequencesDocument.Add(seq); }
        seq.DernierNumero++;
        await Db.SaveChangesAsync();
        return $"BL-{exercice}-{seq.DernierNumero:D5}";
    };

    private Task<(BonLivraison? Bl, string? Erreur)> CreerAsync(int commandeId, int ligneId, Func<Task<string>> generateur)
        => Service().CreerBrouillonAsync(commandeId, DateTime.Today, null, new (int?, int)[] { (ligneId, 1) }, generateur);

    private async Task<int?> CompteurAsync()
    {
        Db.ChangeTracker.Clear();
        return (await Db.SequencesDocument.AsNoTracking().FirstOrDefaultAsync(s => s.TypeDocument == "BL" && s.Exercice == 2026))?.DernierNumero;
    }

    // ------------------------------------------------------------------ B : saut des numéros déjà pris

    [Fact]
    public async Task Un_compteur_absent_et_des_bl_existants_donnent_le_premier_numero_libre_et_le_compteur_est_valide_avec_le_bl()
    {
        var (commande, ligne) = await CommandeAsync();
        await BlsExistantsAsync(1, 3);                                             // BL-2026-00001 à 00003 existent, aucune ligne de compteur

        var (bl, erreur) = await CreerAsync(commande, ligne, GenerateurAvecCompteur());

        Assert.Null(erreur);
        Assert.Equal("BL-2026-00004", bl!.Numero);                                 // avant : « BL-2026-00001 » en doublon, erreur 500
        Assert.Equal(4, _appels);                                                  // 3 numéros pris sautés, le 4e est libre
        Assert.Equal(4, await CompteurAsync());                                    // le compteur avance AVEC le BL : le prochain sera 00005
        Assert.Equal(1, await Db.BonsLivraison.CountAsync(b => b.CommandeClientId == commande));
    }

    [Fact]
    public async Task Un_numero_libre_du_premier_coup_ne_declenche_aucun_saut()
    {
        var (commande, ligne) = await CommandeAsync();

        var (bl, erreur) = await CreerAsync(commande, ligne, GenerateurAvecCompteur());

        Assert.Null(erreur);
        Assert.Equal("BL-2026-00001", bl!.Numero);
        Assert.Equal(1, _appels);
        Assert.Equal(1, await CompteurAsync());
    }

    [Fact]
    public async Task Apres_cinquante_numeros_pris_la_creation_est_refusee_avec_un_message_clair_et_rien_n_est_enregistre()
    {
        var (commande, ligne) = await CommandeAsync();
        await BlsExistantsAsync(1, 50);                                            // 50 numéros pris : le compteur simulé les parcourt tous

        var (bl, erreur) = await CreerAsync(commande, ligne, GenerateurAvecCompteur());

        Assert.Null(bl);
        Assert.Contains("50", erreur);
        Assert.Contains("numéro de bon de livraison libre", erreur);
        Assert.Equal(BonLivraisonService.MaxEssaisNumero, _appels);
        Assert.Equal(50, BonLivraisonService.MaxEssaisNumero);
        Assert.Equal(0, await Db.BonsLivraison.CountAsync(b => b.CommandeClientId == commande));   // aucun BL
        Assert.Null(await CompteurAsync());                                        // le compteur avancé pendant les essais est annulé avec la transaction
    }

    // ------------------------------------------------------------------ C : doublon détecté à l'enregistrement (course)

    [Fact]
    public async Task Un_numero_pris_entre_le_controle_et_l_enregistrement_donne_un_message_clair_sans_exception_ni_bl()
    {
        var (commande, ligne) = await CommandeAsync();
        using var autre = AutreSession();
        // Juste avant l'enregistrement du BL, une autre création prend le même numéro.
        Interception.Action = async () =>
        {
            autre.BonsLivraison.Add(new BonLivraison { Numero = "BL-2026-00077", Statut = StatutBonLivraison.Brouillon });
            await autre.SaveChangesAsync();
        };

        var (bl, erreur) = await CreerAsync(commande, ligne, () => Task.FromResult("BL-2026-00077"));

        Assert.Null(bl);                                                           // avant : DbUpdateException non attrapée (erreur 500)
        Assert.Contains("BL-2026-00077", erreur);
        Assert.Contains("déjà utilisé", erreur);
        Assert.Contains("Aucun bon de livraison n'a été créé", erreur);
        Assert.Equal(0, await Db.BonsLivraison.CountAsync(b => b.CommandeClientId == commande));
        // Remarque : sous SQLite, l'« autre session » partage la connexion et donc la transaction du service : son BL est annulé avec elle.
        // La coexistence réelle des deux BL (celui de l'autre session reste en base) est prouvée sur SQL Server (S10e-c).
    }

    [Fact]
    public async Task Apres_un_refus_de_numero_une_nouvelle_tentative_reussit()
    {
        var (commande, ligne) = await CommandeAsync();
        using var autre = AutreSession();
        Interception.Action = async () =>
        {
            autre.BonsLivraison.Add(new BonLivraison { Numero = "BL-2026-00077", Statut = StatutBonLivraison.Brouillon });
            await autre.SaveChangesAsync();
        };
        var (refuse, _) = await CreerAsync(commande, ligne, () => Task.FromResult("BL-2026-00077"));
        Assert.Null(refuse);

        var (bl, erreur) = await CreerAsync(commande, ligne, () => Task.FromResult("BL-2026-00078"));

        Assert.Null(erreur);
        Assert.Equal("BL-2026-00078", bl!.Numero);
    }
}
