using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using BonLivraisonPage = GestionCommerciale.Pages.BonsLivraison.DetailsModel;

namespace GestionCommerciale.Tests;

/// <summary>
/// S6 étapes 3 et 4 : validation et annulation d'un bon de livraison (mouvements, idempotence, refus si facturé) et
/// gestion de la concurrence par la page. Tout tourne sous SQLite en mémoire : SQLite ne reproduit NI le rowversion
/// NI le verrouillage de SQL Server ; la course de la page est donc SIMULÉE par une exception levée à l'enregistrement.
/// </summary>
public class BonLivraisonServiceTests : ArticleTestBase
{
    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private BonLivraisonService CreerService() => new(Db, new StockService(Db));

    private BonLivraisonPage CreerPage()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return new BonLivraisonPage(Db, CreerService())
        {
            PageContext = new PageContext(actionContext),
            TempData = new TempDataDictionary(httpContext, new FauxTempData())
        };
    }

    /// <summary>Crée un article en stock et un BL brouillon d'une ligne.</summary>
    private async Task<(int ArticleId, int BlId)> CreerBlAsync(int stock = 10, int quantite = 4, string numero = "BL-TEST-1")
    {
        var article = await InsererAsync("BL-" + Guid.NewGuid().ToString("N")[..6], stock: stock);
        var bl = new BonLivraison { Numero = numero, Statut = StatutBonLivraison.Brouillon };
        bl.Lignes.Add(new LigneBonLivraison { ArticleId = article.ID, Quantite = quantite, PrixUnitaire = 5m });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return (article.ID, bl.ID);
    }

    private async Task<(int Stock, List<MouvementStock> Mouvements, StatutBonLivraison Statut)> LireAsync(int articleId, int blId)
    {
        Db.ChangeTracker.Clear();
        var stock = (await Db.Articles.AsNoTracking().SingleAsync(a => a.ID == articleId)).Quantite;
        var mouvements = await Db.MouvementsStock.AsNoTracking().Where(m => m.ArticleId == articleId).OrderBy(m => m.ID).ToListAsync();
        var statut = (await Db.BonsLivraison.AsNoTracking().SingleAsync(b => b.ID == blId)).Statut;
        return (stock, mouvements, statut);
    }

    private async Task AjouterFactureAsync(int blId, StatutFacture statut)
    {
        Db.Factures.Add(new Facture { BonLivraisonId = blId, Statut = statut });
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
    }

    // ------------------------------------------------------------------ validation

    [Fact]
    public async Task Valider_fait_sortir_le_stock_avec_un_mouvement_de_quantite_positive_et_tracable()
    {
        var (articleId, blId) = await CreerBlAsync(stock: 10, quantite: 4);

        var (succes, erreur) = await CreerService().ValiderAsync(blId);

        Assert.True(succes, erreur);
        var (stock, mouvements, statut) = await LireAsync(articleId, blId);
        Assert.Equal(6, stock);
        Assert.Equal(StatutBonLivraison.Valide, statut);
        var m = Assert.Single(mouvements);
        Assert.Equal(TypeMouvementStock.SortieLivraison, m.Type);
        Assert.Equal(4, m.Quantite);                       // positive : le sens est porté par le type
        Assert.Equal(10, m.QuantiteAvant);                 // avant : jamais renseigné
        Assert.Equal(6, m.QuantiteApres);
        Assert.Equal("BL-TEST-1", m.Reference);
        Assert.Equal("BonLivraison", m.SourceType);
        Assert.Equal(blId, m.SourceId);
    }

    [Fact]
    public async Task Valider_avec_un_stock_insuffisant_est_refuse_sans_aucune_trace()
    {
        var (articleId, blId) = await CreerBlAsync(stock: 3, quantite: 4);

        var (succes, erreur) = await CreerService().ValiderAsync(blId);

        Assert.False(succes);
        Assert.Contains("Stock insuffisant", erreur);
        var (stock, mouvements, statut) = await LireAsync(articleId, blId);
        Assert.Equal(3, stock);
        Assert.Empty(mouvements);
        Assert.Equal(StatutBonLivraison.Brouillon, statut);
    }

    // ------------------------------------------------------------------ annulation (étape 3)

    [Fact]
    public async Task Annuler_un_bl_valide_cree_le_mouvement_inverse_sans_supprimer_l_original()
    {
        var (articleId, blId) = await CreerBlAsync(stock: 10, quantite: 4);
        await CreerService().ValiderAsync(blId);

        var (succes, erreur) = await CreerService().AnnulerAsync(blId);

        Assert.True(succes, erreur);
        var (stock, mouvements, statut) = await LireAsync(articleId, blId);
        Assert.Equal(10, stock);                           // stock rétabli
        Assert.Equal(StatutBonLivraison.Annule, statut);
        Assert.Equal(2, mouvements.Count);                 // la sortie d'origine est CONSERVÉE
        Assert.Equal(TypeMouvementStock.SortieLivraison, mouvements[0].Type);
        var inverse = mouvements[1];
        Assert.Equal(TypeMouvementStock.EntreeAnnulationLivraison, inverse.Type);   // type dédié, plus EntreeReception
        Assert.Equal(4, inverse.Quantite);                 // positive
        Assert.Equal(6, inverse.QuantiteAvant);
        Assert.Equal(10, inverse.QuantiteApres);
        Assert.Equal("BonLivraison", inverse.SourceType);
        Assert.Equal(blId, inverse.SourceId);
    }

    [Fact]
    public async Task Annuler_deux_fois_ne_double_pas_l_effet()
    {
        var (articleId, blId) = await CreerBlAsync(stock: 10, quantite: 4);
        await CreerService().ValiderAsync(blId);
        await CreerService().AnnulerAsync(blId);

        var (succes, erreur) = await CreerService().AnnulerAsync(blId);

        Assert.False(succes);
        Assert.Contains("déjà annulé", erreur);
        var (stock, mouvements, _) = await LireAsync(articleId, blId);
        Assert.Equal(10, stock);                           // pas 14
        Assert.Equal(2, mouvements.Count);                 // pas 3
    }

    [Fact]
    public async Task Annuler_est_idempotent_meme_si_le_statut_a_ete_remis_a_valide_apres_une_annulation()
    {
        // Garde-fou indépendant du statut : un mouvement d'annulation existe déjà pour ce BL.
        var (articleId, blId) = await CreerBlAsync(stock: 10, quantite: 4);
        await CreerService().ValiderAsync(blId);
        await CreerService().AnnulerAsync(blId);
        Db.ChangeTracker.Clear();
        var bl = await Db.BonsLivraison.SingleAsync(b => b.ID == blId);
        bl.Statut = StatutBonLivraison.Valide;             // état incohérent fabriqué à la main
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var (succes, _) = await CreerService().AnnulerAsync(blId);

        Assert.False(succes);
        var (stock, mouvements, _) = await LireAsync(articleId, blId);
        Assert.Equal(10, stock);
        Assert.Equal(2, mouvements.Count);
    }

    [Theory]
    [InlineData(StatutFacture.Brouillon)]
    [InlineData(StatutFacture.Validee)]
    public async Task Annuler_un_bl_lie_a_une_facture_non_annulee_est_refuse(StatutFacture statutFacture)
    {
        var (articleId, blId) = await CreerBlAsync(stock: 10, quantite: 4);
        await CreerService().ValiderAsync(blId);
        await AjouterFactureAsync(blId, statutFacture);

        var (succes, erreur) = await CreerService().AnnulerAsync(blId);

        Assert.False(succes);
        Assert.Contains("facture", erreur, StringComparison.OrdinalIgnoreCase);
        var (stock, mouvements, statut) = await LireAsync(articleId, blId);
        Assert.Equal(6, stock);                            // rien n'a bougé
        Assert.Single(mouvements);
        Assert.Equal(StatutBonLivraison.Valide, statut);
    }

    [Fact]
    public async Task Annuler_un_bl_dont_la_facture_est_annulee_est_autorise()
    {
        var (articleId, blId) = await CreerBlAsync(stock: 10, quantite: 4);
        await CreerService().ValiderAsync(blId);
        await AjouterFactureAsync(blId, StatutFacture.Annulee);

        var (succes, erreur) = await CreerService().AnnulerAsync(blId);

        Assert.True(succes, erreur);
        Assert.Equal(10, (await LireAsync(articleId, blId)).Stock);
    }

    [Fact]
    public async Task Annuler_un_bl_au_statut_Facture_est_toujours_refuse()
    {
        var (articleId, blId) = await CreerBlAsync(stock: 10, quantite: 4);
        await CreerService().ValiderAsync(blId);
        Db.ChangeTracker.Clear();
        (await Db.BonsLivraison.SingleAsync(b => b.ID == blId)).Statut = StatutBonLivraison.Facture;
        await Db.SaveChangesAsync();

        var (succes, erreur) = await CreerService().AnnulerAsync(blId);

        Assert.False(succes);
        Assert.Contains("facturé", erreur);
        Assert.Equal(6, (await LireAsync(articleId, blId)).Stock);
    }

    [Fact]
    public async Task Annuler_un_brouillon_ne_cree_aucun_mouvement()
    {
        var (articleId, blId) = await CreerBlAsync(stock: 10, quantite: 4);

        var (succes, erreur) = await CreerService().AnnulerAsync(blId);

        Assert.True(succes, erreur);
        var (stock, mouvements, statut) = await LireAsync(articleId, blId);
        Assert.Equal(10, stock);
        Assert.Empty(mouvements);
        Assert.Equal(StatutBonLivraison.Annule, statut);
    }

    // ------------------------------------------------------------------ concurrence (étape 4) : la page attrape l'exception

    [Fact]
    public async Task La_page_valider_affiche_un_message_clair_en_cas_de_conflit_de_concurrence_et_rien_n_est_enregistre()
    {
        var (articleId, blId) = await CreerBlAsync(stock: 10, quantite: 4);
        var page = CreerPage();
        Interception.Action = () => throw new DbUpdateConcurrencyException("conflit simulé");   // course SIMULÉE (pas de rowversion sous SQLite)

        var resultat = await page.OnPostValiderAsync(blId);

        Assert.IsType<RedirectToPageResult>(resultat);     // pas d'exception : pas d'erreur 500
        Assert.Contains("modifié par une autre opération", (string)page.TempData["ErrorMessage"]!);
        Assert.False(page.TempData.ContainsKey("SuccessMessage"));
        var (stock, mouvements, statut) = await LireAsync(articleId, blId);
        Assert.Equal(10, stock);                           // transaction annulée
        Assert.Empty(mouvements);
        Assert.Equal(StatutBonLivraison.Brouillon, statut);
    }

    [Fact]
    public async Task La_page_annuler_affiche_un_message_clair_en_cas_de_conflit_de_concurrence_et_rien_n_est_enregistre()
    {
        var (articleId, blId) = await CreerBlAsync(stock: 10, quantite: 4);
        await CreerService().ValiderAsync(blId);
        var page = CreerPage();
        Interception.Action = () => throw new DbUpdateConcurrencyException("conflit simulé");

        var resultat = await page.OnPostAnnulerAsync(blId);

        Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Contains("modifié par une autre opération", (string)page.TempData["ErrorMessage"]!);
        var (stock, mouvements, statut) = await LireAsync(articleId, blId);
        Assert.Equal(6, stock);                            // l'annulation n'a pas eu lieu
        Assert.Single(mouvements);
        Assert.Equal(StatutBonLivraison.Valide, statut);
    }

    [Fact]
    public async Task La_page_affiche_les_messages_normaux_de_succes_et_de_refus()
    {
        var (_, blId) = await CreerBlAsync(stock: 10, quantite: 4);
        var page = CreerPage();

        await page.OnPostValiderAsync(blId);
        Assert.Contains("valid", (string)page.TempData["SuccessMessage"]!);

        await page.OnPostValiderAsync(blId);                // 2e validation : refus normal du service
        Assert.Contains("brouillon", (string)page.TempData["ErrorMessage"]!);
    }
}
