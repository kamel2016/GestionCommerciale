using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using CreateBl = GestionCommerciale.Pages.BonsLivraison.CreateModel;
using DetailsCommande = GestionCommerciale.Pages.Commande_Client.DetailsModel;

namespace GestionCommerciale.Tests;

/// <summary>
/// S10b (correctif de navigation) : la page « Nouveau bon de livraison » propose les commandes ÉLIGIBLES (statut Confirmée, En préparation
/// ou Expédiée, avec du reste à livrer par cumul des BL non annulés, brouillons inclus) au lieu de rediriger silencieusement ; les refus
/// utilisent la clé TempData lue par la liste des commandes ; le bouton de la fiche commande suit la même règle que le serveur.
/// </summary>
public class BonLivraisonSelectionTests : ArticleTestBase
{
    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private BonLivraisonService Service() => new(Db, new StockService(Db));

    private static (PageContext Contexte, TempDataDictionary Temp) Contexte()
    {
        var http = new DefaultHttpContext();
        var action = new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return (new PageContext(action), new TempDataDictionary(http, new FauxTempData()));
    }

    private async Task<(int CommandeId, int LigneA, int LigneB, int ClientId)> CommandeAsync(
        StatutCommande statut = StatutCommande.Confirmee, int quantiteA = 10, int quantiteB = 5, string nomClient = "Client sélection")
    {
        var client = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = nomClient };
        Db.Clients.Add(client);
        var a = new Article { Reference = "SEL-" + Guid.NewGuid().ToString("N")[..6], Libelle = "Câble A", Quantite = 1000, PrixVenteHT = 6.500m, TauxTVA = 19m };
        var b = new Article { Reference = "SEL-" + Guid.NewGuid().ToString("N")[..6], Libelle = "Prise B", Quantite = 1000, PrixVenteHT = 3.250m, TauxTVA = 7m };
        Db.Articles.AddRange(a, b);
        await Db.SaveChangesAsync();
        var commande = new CommandeClient { NumeroCommande = "CC-SEL-" + Guid.NewGuid().ToString("N")[..5], ClientId = client.Id, Statut = statut };
        commande.LignesCommande.Add(new LigneCommandeClient { ArticleId = a.ID, Quantite = quantiteA, PrixUnitaire = 6.500m, TauxTVA = 19m });
        commande.LignesCommande.Add(new LigneCommandeClient { ArticleId = b.ID, Quantite = quantiteB, PrixUnitaire = 3.250m, TauxTVA = 7m });
        commande.RecalculerTotaux();
        Db.CommandesClient.Add(commande);
        await Db.SaveChangesAsync();
        var ids = commande.LignesCommande.OrderBy(l => l.Id).Select(l => l.Id).ToList();
        Db.ChangeTracker.Clear();
        return (commande.Id, ids[0], ids[1], client.Id);
    }

    /// <summary>BL directement en base (brouillon par défaut) prenant les quantités données sur les lignes A et B.</summary>
    private async Task<int> BlAsync(int commandeId, int clientId, int ligneA, int ligneB, int quantiteA, int quantiteB, StatutBonLivraison statut = StatutBonLivraison.Brouillon)
    {
        var lignes = await Db.LignesCommandeClient.AsNoTracking().Where(l => l.CommandeClientId == commandeId).ToDictionaryAsync(l => l.Id);
        var bl = new BonLivraison { Numero = "BL-SEL-" + Guid.NewGuid().ToString("N")[..6], ClientId = clientId, CommandeClientId = commandeId, Statut = statut };
        if (quantiteA > 0) bl.Lignes.Add(new LigneBonLivraison { ArticleId = lignes[ligneA].ArticleId, LigneCommandeClientId = ligneA, Quantite = quantiteA, PrixUnitaire = 6.500m });
        if (quantiteB > 0) bl.Lignes.Add(new LigneBonLivraison { ArticleId = lignes[ligneB].ArticleId, LigneCommandeClientId = ligneB, Quantite = quantiteB, PrixUnitaire = 3.250m });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return bl.ID;
    }

    private CreateBl PageCreation()
    {
        var (contexte, temp) = Contexte();
        return new CreateBl(Db, new NumerotationService(Db), Service()) { PageContext = contexte, TempData = temp };
    }

    // ------------------------------------------------------------------ CommandesEligiblesAsync : statuts

    [Theory]
    [InlineData(StatutCommande.Confirmee, true)]
    [InlineData(StatutCommande.EnPreparation, true)]
    [InlineData(StatutCommande.Expediee, true)]
    [InlineData(StatutCommande.EnAttente, false)]
    [InlineData(StatutCommande.Livree, false)]
    [InlineData(StatutCommande.Annulee, false)]
    public async Task Seules_les_commandes_confirmees_en_preparation_ou_expediees_sont_proposees(StatutCommande statut, bool propose)
    {
        var (commande, _, _, _) = await CommandeAsync(statut);

        var eligibles = await Service().CommandesEligiblesAsync();

        Assert.Equal(propose, eligibles.Any(c => c.Id == commande));
        Assert.Equal(propose, await Service().PeutCreerBonAsync(commande));
    }

    // ------------------------------------------------------------------ reste par cumul

    [Fact]
    public async Task Une_commande_proposee_indique_son_client_son_statut_ses_lignes_a_livrer_et_son_reste_total()
    {
        var (commande, _, _, _) = await CommandeAsync(StatutCommande.EnPreparation, quantiteA: 10, quantiteB: 5, nomClient: "Société Alpha");

        var c = Assert.Single(await Service().CommandesEligiblesAsync());

        Assert.Equal(commande, c.Id);
        Assert.Equal("Société Alpha", c.NomClient);
        Assert.Equal(StatutCommande.EnPreparation, c.Statut);
        Assert.Equal(2, c.NombreLignesAvecReste);
        Assert.Equal(15, c.ResteTotal);                                           // 10 + 5
    }

    [Fact]
    public async Task Un_bl_brouillon_est_compte_dans_le_reste()
    {
        var (commande, ligneA, ligneB, client) = await CommandeAsync(quantiteA: 10, quantiteB: 5);
        await BlAsync(commande, client, ligneA, ligneB, 4, 0);                     // brouillon de 4 sur la ligne A

        var c = Assert.Single(await Service().CommandesEligiblesAsync());

        Assert.Equal(11, c.ResteTotal);                                           // (10 - 4) + 5
        Assert.Equal(2, c.NombreLignesAvecReste);
    }

    [Fact]
    public async Task Une_commande_entierement_couverte_par_des_bl_dont_un_brouillon_n_est_plus_proposee()
    {
        var (commande, ligneA, ligneB, client) = await CommandeAsync(quantiteA: 10, quantiteB: 5);
        await BlAsync(commande, client, ligneA, ligneB, 10, 0, StatutBonLivraison.Valide);
        await BlAsync(commande, client, ligneA, ligneB, 0, 5);                     // brouillon qui couvre la ligne B

        Assert.Empty(await Service().CommandesEligiblesAsync());
        Assert.False(await Service().PeutCreerBonAsync(commande));
    }

    [Fact]
    public async Task Un_bl_annule_n_est_pas_compte_et_la_commande_redevient_proposee()
    {
        var (commande, ligneA, ligneB, client) = await CommandeAsync(quantiteA: 10, quantiteB: 5);
        await BlAsync(commande, client, ligneA, ligneB, 10, 5, StatutBonLivraison.Annule);

        var c = Assert.Single(await Service().CommandesEligiblesAsync());

        Assert.Equal(15, c.ResteTotal);
        Assert.True(await Service().PeutCreerBonAsync(commande));
    }

    [Fact]
    public async Task QuantiteLivree_est_ignoree_le_reste_vient_uniquement_des_lignes_de_bl()
    {
        var (commande, ligneA, ligneB, _) = await CommandeAsync(quantiteA: 10, quantiteB: 5);
        (await Db.LignesCommandeClient.SingleAsync(l => l.Id == ligneA)).QuantiteLivree = 10;
        (await Db.LignesCommandeClient.SingleAsync(l => l.Id == ligneB)).QuantiteLivree = 5;
        await Db.SaveChangesAsync();                                               // valeurs sans aucune ligne de BL pour les justifier
        Db.ChangeTracker.Clear();

        var c = Assert.Single(await Service().CommandesEligiblesAsync());

        Assert.Equal(15, c.ResteTotal);
        Assert.True(await Service().PeutCreerBonAsync(commande));
    }

    [Fact]
    public async Task Les_commandes_proposees_sont_triees_de_la_plus_recente_a_la_plus_ancienne()
    {
        var (premiere, _, _, _) = await CommandeAsync();
        var (seconde, _, _, _) = await CommandeAsync();

        var ids = (await Service().CommandesEligiblesAsync()).Select(c => c.Id).ToList();

        Assert.Equal(new[] { seconde, premiere }, ids);                           // même date : l'identifiant le plus grand d'abord
    }

    [Fact]
    public async Task Une_commande_inexistante_ne_permet_pas_de_creer_un_bl()
        => Assert.False(await Service().PeutCreerBonAsync(987654));

    // ------------------------------------------------------------------ page de création : sélection et clés TempData

    [Fact]
    public async Task Sans_commande_choisie_la_page_affiche_la_liste_des_commandes_eligibles_au_lieu_de_rediriger()
    {
        var (commande, _, _, _) = await CommandeAsync(StatutCommande.EnPreparation);
        await CommandeAsync(StatutCommande.EnAttente);                              // non éligible : absente de la liste
        var page = PageCreation();

        var resultat = await page.OnGetAsync(null);

        Assert.IsType<PageResult>(resultat);
        Assert.Equal(commande, Assert.Single(page.CommandesEligibles).Id);
        Assert.False(page.TempData.ContainsKey("Error"));
        Assert.False(page.TempData.ContainsKey("ErrorMessage"));
    }

    [Fact]
    public async Task Sans_commande_choisie_et_sans_commande_eligible_la_page_s_affiche_avec_une_liste_vide()
    {
        var page = PageCreation();

        var resultat = await page.OnGetAsync(null);

        Assert.IsType<PageResult>(resultat);
        Assert.Empty(page.CommandesEligibles);
    }

    [Theory]
    [InlineData(StatutCommande.EnAttente, "En attente")]
    [InlineData(StatutCommande.Livree, "Livrée")]
    [InlineData(StatutCommande.Annulee, "Annulée")]
    public async Task Une_commande_non_eligible_redirige_vers_la_liste_des_commandes_avec_la_cle_lue_par_cette_liste(StatutCommande statut, string libelle)
    {
        var (commande, _, _, _) = await CommandeAsync(statut);
        var page = PageCreation();

        var resultat = await page.OnGetAsync(commande);

        var redirection = Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal("/Commande_Client/Index", redirection.PageName);
        Assert.Contains(libelle, (string)page.TempData["Error"]!);                // clé « Error » : celle que Commande_Client/Index affiche
        Assert.Contains("Confirmée, En préparation ou Expédiée", (string)page.TempData["Error"]!);
        Assert.False(page.TempData.ContainsKey("ErrorMessage"));                  // l'ancienne clé, jamais affichée, n'est plus utilisée
    }

    [Fact]
    public async Task Une_commande_entierement_couverte_redirige_avec_un_message_visible()
    {
        var (commande, ligneA, ligneB, client) = await CommandeAsync(quantiteA: 10, quantiteB: 5);
        await BlAsync(commande, client, ligneA, ligneB, 10, 5);
        var page = PageCreation();

        var resultat = await page.OnGetAsync(commande);

        Assert.Equal("/Commande_Client/Index", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.Contains("entièrement couverte", (string)page.TempData["Error"]!);
        Assert.False(page.TempData.ContainsKey("ErrorMessage"));
    }

    [Fact]
    public async Task Une_commande_inexistante_donne_une_page_non_trouvee()
        => Assert.IsType<NotFoundResult>(await PageCreation().OnGetAsync(987654));

    [Fact]
    public async Task Une_commande_eligible_affiche_le_formulaire_avec_le_client_en_lecture_seule_et_le_reste_par_cumul()
    {
        var (commande, ligneA, ligneB, client) = await CommandeAsync(StatutCommande.EnPreparation, quantiteA: 10, quantiteB: 5, nomClient: "Société Beta");
        await BlAsync(commande, client, ligneA, ligneB, 3, 0);                     // brouillon de 3 sur la ligne A
        var page = PageCreation();

        var resultat = await page.OnGetAsync(commande);

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("Société Beta", page.NomClient);
        Assert.Equal(new[] { 7, 5 }, page.Lignes.OrderBy(l => l.LigneCommandeClientId).Select(l => l.QuantiteRestante).ToArray());   // 10 - 3 ; 5
        Assert.Empty(page.CommandesEligibles);
        Assert.False(page.TempData.ContainsKey("Error"));
    }

    // ------------------------------------------------------------------ fiche commande : le bouton suit la règle du serveur

    private DetailsCommande PageFiche()
    {
        var (contexte, temp) = Contexte();
        return new DetailsCommande(Db, Service()) { PageContext = contexte, TempData = temp };
    }

    [Theory]
    [InlineData(StatutCommande.Confirmee, true)]
    [InlineData(StatutCommande.EnPreparation, true)]       // avant : bouton absent (seule « Confirmée » était acceptée par la vue)
    [InlineData(StatutCommande.Expediee, true)]            // avant : bouton absent
    [InlineData(StatutCommande.EnAttente, false)]
    [InlineData(StatutCommande.Livree, false)]
    [InlineData(StatutCommande.Annulee, false)]
    public async Task La_fiche_propose_le_bouton_de_creation_de_bl_selon_la_regle_du_serveur(StatutCommande statut, bool bouton)
    {
        var (commande, _, _, _) = await CommandeAsync(statut);
        var page = PageFiche();

        await page.OnGetAsync(commande);

        Assert.Equal(bouton, page.PeutCreerBonLivraison);
    }

    [Fact]
    public async Task La_fiche_n_affiche_pas_le_bouton_quand_le_reste_par_cumul_est_nul_meme_si_QuantiteLivree_est_a_zero()
    {
        var (commande, ligneA, ligneB, client) = await CommandeAsync(quantiteA: 10, quantiteB: 5);
        await BlAsync(commande, client, ligneA, ligneB, 10, 5);                    // un brouillon couvre tout ; QuantiteLivree vaut encore 0
        var page = PageFiche();

        await page.OnGetAsync(commande);

        Assert.False(page.PeutCreerBonLivraison);                                  // l'ancienne règle (QuantiteLivree) l'aurait affiché
    }

    // ------------------------------------------------------------------ texte des vues et des clés

    private static string Lire(params string[] segments)
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "GestionCommerciale.sln"))) dir = Path.GetDirectoryName(dir);
        return File.ReadAllText(Path.Combine(new[] { dir!, "GestionCommerciale", "Pages" }.Concat(segments).ToArray()));
    }

    [Fact]
    public void La_vue_de_creation_de_bl_contient_la_liste_de_selection_et_le_message_de_liste_vide()
    {
        var vue = Lire("BonsLivraison", "Create.cshtml");

        Assert.Contains("Model.CommandesEligibles", vue);
        Assert.Contains("asp-route-commandeId=\"@c.Id\"", vue);                   // un lien « Créer un BL » par commande proposée
        Assert.Contains("Aucune commande éligible", vue);
    }

    [Fact]
    public void Le_bouton_de_la_fiche_commande_utilise_PeutCreerBonLivraison_et_plus_l_ancienne_regle()
    {
        var vue = Lire("Commande_Client", "Details.cshtml");

        Assert.Contains("Model.PeutCreerBonLivraison", vue);
        Assert.DoesNotContain("Any(l => l.QuantiteRestante > 0)", vue);             // ancienne condition du bouton, fondée sur QuantiteLivree
        Assert.DoesNotContain("@if (Model.CommandeClient.Statut == StatutCommande.Confirmee", vue);   // (le badge de statut et la colonne « Reste » restent des affichages)
    }

    [Fact]
    public void La_page_de_creation_de_bl_n_ecrit_plus_la_cle_ErrorMessage_pour_ses_redirections_vers_la_liste_des_commandes()
    {
        var page = Lire("BonsLivraison", "Create.cshtml.cs");
        var liste = Lire("Commande_Client", "Index.cshtml");

        Assert.DoesNotContain("TempData[\"ErrorMessage\"]", page);
        Assert.Contains("TempData[\"Error\"]", page);
        Assert.Contains("TempData[\"Error\"]", liste);                              // la liste des commandes lit bien cette clé
    }
}
