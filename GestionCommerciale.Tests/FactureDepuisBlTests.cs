using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

/// <summary>
/// S10 : la facture est créée depuis les lignes du BL, avec le prix, la TVA et la remise de la LIGNE DE COMMANDE liée (figés), jamais
/// ceux de l'article du jour ; montants par CalculTotaux (un seul mode d'arrondi) ; FODEC seulement sur les articles marqués ; timbre une
/// fois par facture ; contrôles (BL validé, non déjà facturé, lignes liées). Toutes les valeurs attendues sont calculées à la main.
/// Sous SQLite : ne prouve pas le comportement de SQL Server (voir SqlServerFactureDecimalTests pour les colonnes).
/// </summary>
public class FactureDepuisBlTests : ArticleTestBase
{
    // ------------------------------------------------------------------ outils

    private FactureService CreerService(ApplicationDbContext? db = null) => new(db ?? Db, null!);   // la séquence ne sert qu'à la validation

    private async Task ParametresAsync(decimal tauxFodec = 1m, bool baseTvaInclutFodec = false, decimal timbre = 1.000m)
    {
        var p = await Db.ParametresSociete.FirstOrDefaultAsync();
        if (p == null) { p = new ParametresSociete { RaisonSociale = "Société test" }; Db.ParametresSociete.Add(p); }
        p.TauxFodec = tauxFodec; p.BaseTvaInclutFodec = baseTvaInclutFodec; p.TimbreFiscal = timbre; p.RetenueActive = false;
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
    }

    private async Task<int> ClientAsync()
    {
        var client = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Client facture" };
        Db.Clients.Add(client);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return client.Id;
    }

    private async Task<int> ArticleAsync(string prixVente, decimal tva, bool fodec = false)
    {
        var article = new Article
        {
            Reference = "FD-" + Guid.NewGuid().ToString("N")[..8], Libelle = "Article " + prixVente, Quantite = 1000,
            PrixVenteHT = decimal.Parse(prixVente, System.Globalization.CultureInfo.InvariantCulture), TauxTVA = tva, Fodec = fodec
        };
        Db.Articles.Add(article);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return article.ID;
    }

    private async Task ChangerArticleAsync(int articleId, string prix, decimal tva)
    {
        var a = await Db.Articles.SingleAsync(x => x.ID == articleId);
        a.PrixVenteHT = decimal.Parse(prix, System.Globalization.CultureInfo.InvariantCulture);
        a.TauxTVA = tva;
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
    }

    /// <summary>Commande avec ses lignes (prix et taux figés sur la ligne, comme Create) ; totaux calculés par RecalculerTotaux.</summary>
    private async Task<(int CommandeId, List<int> LigneIds)> CommandeAsync(int clientId, decimal remiseGlobale, params (int ArticleId, int Quantite, string Prix, decimal Tva)[] lignes)
    {
        var commande = new CommandeClient
        {
            NumeroCommande = "CC-FD-" + Guid.NewGuid().ToString("N")[..6], ClientId = clientId, Statut = StatutCommande.Confirmee, TauxRemiseGlobale = remiseGlobale
        };
        foreach (var (articleId, quantite, prix, tva) in lignes)
            commande.LignesCommande.Add(new LigneCommandeClient
            {
                ArticleId = articleId, Quantite = quantite, TauxTVA = tva,
                PrixUnitaire = decimal.Parse(prix, System.Globalization.CultureInfo.InvariantCulture)
            });
        commande.RecalculerTotaux();
        Db.CommandesClient.Add(commande);
        await Db.SaveChangesAsync();
        var ids = commande.LignesCommande.OrderBy(l => l.Id).Select(l => l.Id).ToList();
        Db.ChangeTracker.Clear();
        return (commande.Id, ids);
    }

    /// <summary>BL validé livrant les quantités données pour chaque ligne de commande (même ordre que CommandeAsync).</summary>
    private async Task<int> BlAsync(int commandeId, int clientId, StatutBonLivraison statut, params int[] quantites)
    {
        var commande = await Db.CommandesClient.AsNoTracking().Include(c => c.LignesCommande).SingleAsync(c => c.Id == commandeId);
        var bl = new BonLivraison { Numero = "BL-FD-" + Guid.NewGuid().ToString("N")[..6], ClientId = clientId, CommandeClientId = commandeId, Statut = statut };
        var lignes = commande.LignesCommande.OrderBy(l => l.Id).ToList();
        for (var i = 0; i < lignes.Count; i++)
            if (quantites[i] > 0)
                bl.Lignes.Add(new LigneBonLivraison { ArticleId = lignes[i].ArticleId, LigneCommandeClientId = lignes[i].Id, Quantite = quantites[i], PrixUnitaire = lignes[i].PrixUnitaire });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return bl.ID;
    }

    private async Task<Facture> FacturerAsync(int blId, ApplicationDbContext? db = null)
    {
        var contexte = db ?? Db;
        var f = await CreerService(contexte).CreerDepuisBonLivraisonAsync(blId);
        contexte.ChangeTracker.Clear();
        return await contexte.Factures.AsNoTracking().Include(x => x.Lignes).SingleAsync(x => x.Id == f.Id);
    }

    // ------------------------------------------------------------------ prix figés de la commande

    [Fact]
    public async Task La_facture_depuis_un_bl_donne_les_memes_chiffres_que_la_commande()
    {
        await ParametresAsync(tauxFodec: 1m);                         // aucun article marqué : FODEC 0
        var client = await ClientAsync();
        var a = await ArticleAsync("0.335", 20m);
        var b = await ArticleAsync("10.500", 19m);
        var (commande, _) = await CommandeAsync(client, 0m, (a, 3, "0.335", 20m), (b, 2, "10.500", 19m));
        var bl = await BlAsync(commande, client, StatutBonLivraison.Valide, 3, 2);

        var f = await FacturerAsync(bl);

        // à la main : lignes 1,005 (20 %) et 21,000 (19 %) ; HT 22,005 ; TVA 0,201 + 3,990 = 4,191 ; timbre 1,000 ; TTC 27,196
        Assert.Equal(new[] { 1.005m, 21.000m }, f.Lignes.OrderBy(l => l.Id).Select(l => l.TotalHt).ToArray());
        Assert.Equal(new[] { 0.335m, 10.500m }, f.Lignes.OrderBy(l => l.Id).Select(l => l.PrixUnitaireHt).ToArray());
        Assert.Equal(new[] { 20m, 19m }, f.Lignes.OrderBy(l => l.Id).Select(l => l.TauxTva).ToArray());
        Assert.Equal((22.005m, 0m, 4.191m, 1.000m, 27.196m), (f.TotalHt, f.MontantFodec, f.TotalTva, f.TimbreFiscal, f.TotalTtc));
        Assert.Equal(2, f.RegleCalcul);
    }

    [Fact]
    public async Task Le_prix_et_la_tva_de_l_article_modifies_apres_la_commande_ne_changent_pas_la_facture()
    {
        await ParametresAsync();
        var client = await ClientAsync();
        var a = await ArticleAsync("0.335", 20m);
        var b = await ArticleAsync("10.500", 19m);
        var (commande, _) = await CommandeAsync(client, 0m, (a, 3, "0.335", 20m), (b, 2, "10.500", 19m));
        var bl = await BlAsync(commande, client, StatutBonLivraison.Valide, 3, 2);
        await ChangerArticleAsync(a, "0.500", 7m);                    // les articles changent APRÈS la commande et le BL
        await ChangerArticleAsync(b, "12.000", 19m);

        var f = await FacturerAsync(bl);

        Assert.Equal(22.005m, f.TotalHt);                             // avant S10 : 25,500
        Assert.Equal(4.191m, f.TotalTva);                             // avant S10 : 4,665
        Assert.Equal(27.196m, f.TotalTtc);                            // avant S10 : 31,165
    }

    [Fact]
    public async Task Le_prix_de_la_facture_vient_de_la_ligne_de_commande_pas_de_la_ligne_de_bl()
    {
        await ParametresAsync();
        var client = await ClientAsync();
        var a = await ArticleAsync("10.500", 19m);
        var (commande, _) = await CommandeAsync(client, 0m, (a, 2, "9.000", 19m));   // prix FIGÉ de la commande (l'article vaut 10,500 aujourd'hui)
        var bl = await BlAsync(commande, client, StatutBonLivraison.Valide, 2);
        var ligneBl = await Db.LignesBonLivraison.SingleAsync(l => l.BonLivraisonId == bl);
        ligneBl.PrixUnitaire = 1.000m;                                 // prix de BL falsifié
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var f = await FacturerAsync(bl);

        Assert.Equal(18.000m, f.TotalHt);                             // 2 x 9,000 (ligne de commande) : ni 1,000 (BL falsifié) ni 21,000 (article du jour)
    }

    [Fact]
    public async Task Zero_virgule_cent_cinquante_a_7_pour_cent_donne_la_meme_tva_en_commande_et_en_facture()
    {
        await ParametresAsync();
        var client = await ClientAsync();
        var a = await ArticleAsync("0.150", 7m);
        var (commande, _) = await CommandeAsync(client, 0m, (a, 1, "0.150", 7m));
        var c = await Db.CommandesClient.AsNoTracking().SingleAsync(x => x.Id == commande);
        Assert.Equal(0.011m, c.MontantTVA);                           // AwayFromZero en commande

        var f = await FacturerAsync(await BlAsync(commande, client, StatutBonLivraison.Valide, 1));

        Assert.Equal(0.011m, f.TotalTva);                             // avant S10 : 0,010 (ToEven)
        Assert.Equal(0.150m + 0.011m + 1.000m, f.TotalTtc);           // 1,161
    }

    // ------------------------------------------------------------------ plusieurs taux, remise, FODEC, timbre

    [Theory]
    [InlineData(false, "18.360", "128.080")]   // TVA hors FODEC
    [InlineData(true, "18.476", "128.196")]    // TVA incluant le FODEC
    public async Task Plusieurs_taux_remise_globale_fodec_sur_les_seuls_articles_marques_et_timbre(bool baseTvaInclutFodec, string tvaAttendue, string ttcAttendu)
    {
        // A (FODEC) 10 x 6,000 à 19 % = 60,000 ; B 4 x 10,000 à 19 % = 40,000 ; C (FODEC) 5 x 4,000 à 7 % = 20,000 ; remise globale 10 %
        // brut 120,000 ; remise 12,000 répartie : (7 %, marqué) 2,000 ; (19 %, non marqué) 4,000 ; (19 %, marqué) 12 - 6 = 6,000
        // nets : 18,000 ; 36,000 ; 54,000 ; HT net 108,000 ; FODEC 1 % : 7 % : 0,180 ; 19 % : 0,540 ; total 0,720
        // hors FODEC : TVA 7 % : 1,260 ; 19 % : 90 x 19 % = 17,100 ; TVA 18,360 ; TTC = 108 + 0,72 + 18,36 + timbre 1 = 128,080
        // avec FODEC : 7 % : 18,18 x 7 % = 1,2726 -> 1,273 ; 19 % : 90,54 x 19 % = 17,2026 -> 17,203 ; TVA 18,476 ; TTC 128,196
        await ParametresAsync(tauxFodec: 1m, baseTvaInclutFodec: baseTvaInclutFodec, timbre: 1.000m);
        var client = await ClientAsync();
        var a = await ArticleAsync("6.000", 19m, fodec: true);
        var b = await ArticleAsync("10.000", 19m, fodec: false);
        var c = await ArticleAsync("4.000", 7m, fodec: true);
        var (commande, _) = await CommandeAsync(client, 10m, (a, 10, "6.000", 19m), (b, 4, "10.000", 19m), (c, 5, "4.000", 7m));
        var bl = await BlAsync(commande, client, StatutBonLivraison.Valide, 10, 4, 5);

        var f = await FacturerAsync(bl);

        Assert.Equal(10m, f.RemiseGlobalePourcent);                   // reprise de la commande
        Assert.Equal(108.000m, f.TotalHt);
        Assert.Equal(0.720m, f.MontantFodec);
        Assert.Equal(decimal.Parse(tvaAttendue, System.Globalization.CultureInfo.InvariantCulture), f.TotalTva);
        Assert.Equal(1.000m, f.TimbreFiscal);
        Assert.Equal(decimal.Parse(ttcAttendu, System.Globalization.CultureInfo.InvariantCulture), f.TotalTtc);
        Assert.Equal(new[] { true, false, true }, f.Lignes.OrderBy(l => l.Id).Select(l => l.SoumiseFodec).ToArray());
    }

    [Fact]
    public async Task Aucun_article_marque_donne_un_fodec_nul_meme_avec_un_taux_fodec_dans_les_parametres()
    {
        await ParametresAsync(tauxFodec: 1m);                         // avant S10 : 1 % appliqué à tout le HT
        var client = await ClientAsync();
        var a = await ArticleAsync("100.000", 19m);
        var (commande, _) = await CommandeAsync(client, 0m, (a, 1, "100.000", 19m));

        var f = await FacturerAsync(await BlAsync(commande, client, StatutBonLivraison.Valide, 1));

        Assert.Equal(0m, f.MontantFodec);
        Assert.Equal(100.000m + 19.000m + 1.000m, f.TotalTtc);        // 120,000
    }

    // ------------------------------------------------------------------ BL partiel puis second BL : un timbre par facture

    [Fact]
    public async Task Un_bl_partiel_puis_un_second_bl_donnent_deux_factures_coherentes_avec_un_timbre_chacune()
    {
        await ParametresAsync();
        var client = await ClientAsync();
        var a = await ArticleAsync("2.000", 19m);
        var (commande, _) = await CommandeAsync(client, 0m, (a, 10, "2.000", 19m));    // 20,000
        var bl1 = await BlAsync(commande, client, StatutBonLivraison.Valide, 4);
        var bl2 = await BlAsync(commande, client, StatutBonLivraison.Valide, 6);

        var f1 = await FacturerAsync(bl1);
        var f2 = await FacturerAsync(bl2);

        // f1 : HT 8,000 ; TVA 1,520 ; timbre 1,000 ; TTC 10,520 ; f2 : HT 12,000 ; TVA 2,280 ; timbre 1,000 ; TTC 15,280
        Assert.Equal((8.000m, 1.520m, 1.000m, 10.520m), (f1.TotalHt, f1.TotalTva, f1.TimbreFiscal, f1.TotalTtc));
        Assert.Equal((12.000m, 2.280m, 1.000m, 15.280m), (f2.TotalHt, f2.TotalTva, f2.TimbreFiscal, f2.TotalTtc));
        Assert.Equal(20.000m, f1.TotalHt + f2.TotalHt);               // somme des HT = HT de la commande
    }

    // ------------------------------------------------------------------ contrôles

    [Theory]
    [InlineData(StatutBonLivraison.Brouillon)]
    [InlineData(StatutBonLivraison.Annule)]
    public async Task Un_bl_brouillon_ou_annule_est_refuse(StatutBonLivraison statut)
    {
        await ParametresAsync();
        var client = await ClientAsync();
        var a = await ArticleAsync("2.000", 19m);
        var (commande, _) = await CommandeAsync(client, 0m, (a, 2, "2.000", 19m));
        var bl = await BlAsync(commande, client, statut, 2);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreerService().CreerDepuisBonLivraisonAsync(bl));

        Assert.Contains("validé", exception.Message);
        Assert.Equal(0, await Db.Factures.CountAsync());
    }

    [Fact]
    public async Task Un_bl_deja_facture_est_refuse_mais_redevient_facturable_si_la_facture_est_annulee()
    {
        await ParametresAsync();
        var client = await ClientAsync();
        var a = await ArticleAsync("2.000", 19m);
        var (commande, _) = await CommandeAsync(client, 0m, (a, 2, "2.000", 19m));
        var bl = await BlAsync(commande, client, StatutBonLivraison.Valide, 2);
        var premiere = await FacturerAsync(bl);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreerService().CreerDepuisBonLivraisonAsync(bl));
        Assert.Contains("déjà facturé", exception.Message);
        Assert.Equal(1, await Db.Factures.CountAsync());

        Db.ChangeTracker.Clear();
        (await Db.Factures.SingleAsync(f => f.Id == premiere.Id)).Statut = StatutFacture.Annulee;
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        var seconde = await FacturerAsync(bl);                         // autorisé : la première facture est annulée
        Assert.NotEqual(premiere.Id, seconde.Id);
    }

    [Fact]
    public async Task Un_bl_dont_une_ligne_n_est_pas_liee_a_une_ligne_de_commande_est_refuse_avec_un_message_clair()
    {
        await ParametresAsync();
        var client = await ClientAsync();
        var a = await ArticleAsync("2.000", 19m);
        var bl = new BonLivraison { Numero = "BL-NL-" + Guid.NewGuid().ToString("N")[..5], ClientId = client, Statut = StatutBonLivraison.Valide };
        bl.Lignes.Add(new LigneBonLivraison { ArticleId = a, Quantite = 2, PrixUnitaire = 2.000m, LigneCommandeClientId = null });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreerService().CreerDepuisBonLivraisonAsync(bl.ID));

        Assert.Contains("non liée", exception.Message);
        Assert.Equal(0, await Db.Factures.CountAsync());
    }

    [Fact]
    public async Task Un_bl_inexistant_est_refuse_avec_un_message_clair()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreerService().CreerDepuisBonLivraisonAsync(9999));

        Assert.Contains("introuvable", exception.Message);
    }

    [Fact]
    public async Task Deux_creations_simultanees_sur_le_meme_bl_une_seule_reussit()
    {
        await ParametresAsync();
        var client = await ClientAsync();
        var a = await ArticleAsync("2.000", 19m);
        var (commande, _) = await CommandeAsync(client, 0m, (a, 2, "2.000", 19m));
        var bl = await BlAsync(commande, client, StatutBonLivraison.Valide, 2);

        // La session 1 contrôle puis, JUSTE AVANT son enregistrement, la session 2 crée la facture du même BL.
        using var session2 = AutreSession();
        Interception.Action = async () => await CreerService(session2).CreerDepuisBonLivraisonAsync(bl);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreerService().CreerDepuisBonLivraisonAsync(bl));

        Assert.Contains("déjà facturé", exception.Message);           // l'index unique a refusé la seconde, traduit en message clair
        Db.ChangeTracker.Clear();
        Assert.Equal(1, await Db.Factures.CountAsync(f => f.BonLivraisonId == bl));
    }

    // ------------------------------------------------------------------ factures déjà validées (règle 1) : jamais recalculées

    private async Task<int> FactureRegle1Async(StatutFacture statut)
    {
        var f = new Facture
        {
            RegleCalcul = 1, Statut = statut, Numero = statut == StatutFacture.Validee ? "FAC-2026-00001" : null,
            RemiseGlobalePourcent = 0m, TotalHt = 1.01m, MontantFodec = 0.01m, TotalTva = 0.20m, TimbreFiscal = 1.00m, TotalTtc = 2.22m, NetAPayer = 2.22m
        };
        f.Lignes.Add(new LigneFacture { ArticleId = await ArticleAsync("0.335", 20m), Designation = "Ancienne", Quantite = 3, PrixUnitaireHt = 0.34m, TauxTva = 20m, TotalHt = 1.01m });
        Db.Factures.Add(f);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return f.Id;
    }

    [Fact]
    public async Task Une_facture_validee_de_regle_1_n_est_jamais_recalculee_meme_si_les_parametres_societe_changent()
    {
        var id = await FactureRegle1Async(StatutFacture.Validee);
        await ParametresAsync(tauxFodec: 5m, baseTvaInclutFodec: true, timbre: 9.000m);       // les paramètres changent
        var service = CreerService();
        var p = await Db.ParametresSociete.AsNoTracking().FirstAsync();

        var facture = await Db.Factures.Include(f => f.Lignes).SingleAsync(f => f.Id == id);
        service.Recalculer(facture, p);                                                          // appel direct
        var (ok, _) = await service.ValiderAsync(id);                                           // déjà validée : refusée
        await Db.SaveChangesAsync();

        Assert.False(ok);
        Db.ChangeTracker.Clear();
        var relue = await Db.Factures.AsNoTracking().SingleAsync(f => f.Id == id);
        Assert.Equal((1.01m, 0.01m, 0.20m, 1.00m, 2.22m), (relue.TotalHt, relue.MontantFodec, relue.TotalTva, relue.TimbreFiscal, relue.TotalTtc));
    }

    [Fact]
    public async Task Un_brouillon_de_regle_1_garde_ses_montants_a_la_validation()
    {
        var id = await FactureRegle1Async(StatutFacture.Brouillon);
        await ParametresAsync(tauxFodec: 5m, timbre: 9.000m);
        var service = new FactureService(Db, new SequenceBidon());

        var (ok, erreur) = await service.ValiderAsync(id);

        Assert.True(ok, erreur);
        Db.ChangeTracker.Clear();
        var relue = await Db.Factures.AsNoTracking().SingleAsync(f => f.Id == id);
        Assert.Equal(StatutFacture.Validee, relue.Statut);
        Assert.Equal((1.01m, 0.01m, 0.20m, 1.00m, 2.22m), (relue.TotalHt, relue.MontantFodec, relue.TotalTva, relue.TimbreFiscal, relue.TotalTtc));
    }

    private sealed class SequenceBidon : GestionCommerciale.Services.Interface.ISequenceService
    {
        public Task<string> ProchainNumeroAsync(string type, int exercice) => Task.FromResult($"{type}-{exercice}-00001");
    }

    // ------------------------------------------------------------------ dette connue S11

    [Fact]
    public void DETTE_CONNUE_S11_le_reste_a_payer_deduit_deux_fois_la_retenue_a_la_source()
    {
        // TTC 110,000 ; retenue 10,000 ; NetAPayer = 110 - 10 = 100,000 (déjà net de la retenue).
        // ResteAPayer = NetAPayer - retenue - règlements = 100 - 10 - 0 = 90,000 : la retenue est déduite UNE SECONDE fois.
        // Le bon reste à payer serait 100,000. Hors périmètre de S10 : ce test DOCUMENTE le comportement actuel et devra être
        // réécrit quand S11 corrigera le calcul.
        var f = new Facture { TotalTtc = 110.000m, MontantRetenueSource = 10.000m, NetAPayer = 100.000m, MontantRegle = 0m };

        Assert.Equal(90.000m, f.ResteAPayer);
    }
}
