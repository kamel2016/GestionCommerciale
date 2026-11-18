using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using EditArticleModel = GestionCommerciale.Pages.Articles.EditModel;

namespace GestionCommerciale.Tests;

/// <summary>
/// S4 étape 3 : le handler principal de la page Articles/Edit (« Enregistrer l'article ») passe par ArticleService
/// et ne lève plus d'exception (plus d'erreur 500) pour une catégorie inconnue ou un doublon simultané.
/// </summary>
public class ArticleEditPageEnregistrementTests : ArticleTestBase
{
    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private EditArticleModel CreerPage(Article saisie, out TempDataDictionary tempData)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        tempData = new TempDataDictionary(httpContext, new FauxTempData());

        return new EditArticleModel(Db, new StockService(Db), Service, NullLogger<EditArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            TempData = tempData,
            Article = saisie
        };
    }

    /// <summary>Saisie complète d'un formulaire Edit pour l'article <paramref name="id"/>.</summary>
    private static Article SaisiePour(int id, string reference)
    {
        var saisie = Saisie(reference, "Libellé saisi");
        saisie.ID = id;
        return saisie;
    }

    private static List<string> Erreurs(EditArticleModel page, string cle)
        => page.ModelState.TryGetValue(cle, out var entree) ? entree.Errors.Select(e => e.ErrorMessage).ToList() : new();

    [Fact]
    public async Task Un_enregistrement_valide_retourne_sur_la_fiche_avec_le_message_de_succes()
    {
        var existant = await InsererAsync("EDT");
        var page = CreerPage(SaisiePour(existant.ID, "EDT"), out var tempData);

        var resultat = await page.OnPostAsync();

        var redirection = Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal("./Edit", redirection.PageName);
        Assert.Equal("Article enregistré avec succès.", tempData["SuccessMessage"]);
        Assert.Equal("Libellé saisi", (await LireAsync(existant.ID))!.Libelle);
    }

    [Fact]
    public async Task Une_categorie_inconnue_ne_leve_plus_d_exception_et_affiche_une_erreur_sur_le_champ()
    {
        var existant = await InsererAsync("EDT-CAT");
        var saisie = SaisiePour(existant.ID, "EDT-CAT");
        saisie.CategoryId = 9999;
        var page = CreerPage(saisie, out _);

        var resultat = await page.OnPostAsync();      // avant correction : DbUpdateException (erreur 500)

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("Catégorie introuvable.", Assert.Single(Erreurs(page, "Article.CategoryId")));
        Assert.Null((await LireAsync(existant.ID))!.CategoryId);
    }

    [Fact]
    public async Task Un_doublon_simultane_ne_leve_plus_d_exception_et_affiche_une_erreur_sur_le_champ()
    {
        var moi = await InsererAsync("EDT-MOI");
        var page = CreerPage(SaisiePour(moi.ID, "EDT-PRIS"), out _);
        // Course réelle : une autre session prend la référence « EDT-PRIS » après le contrôle et avant l'enregistrement.
        Interception.Action = async () =>
        {
            using var autre = AutreSession();
            autre.Articles.Add(new Article { Reference = "EDT-PRIS", Libelle = "Arrivé en premier" });
            await autre.SaveChangesAsync();
        };

        var resultat = await page.OnPostAsync();      // avant correction : DbUpdateException (erreur 500)

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("Cette référence existe déjà.", Assert.Single(Erreurs(page, "Article.Reference")));
        Assert.Equal("EDT-MOI", (await LireAsync(moi.ID))!.Reference);
    }

    [Fact]
    public async Task Un_doublon_de_reference_non_simultane_est_refuse_sur_le_champ()
    {
        await InsererAsync("EDT-A");
        var moi = await InsererAsync("EDT-B");
        var page = CreerPage(SaisiePour(moi.ID, "edt-a"), out _);

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("Cette référence existe déjà.", Assert.Single(Erreurs(page, "Article.Reference")));
    }

    [Fact]
    public async Task Un_stock_maximum_inferieur_au_minimum_est_refuse_et_garde_l_erreur_sur_le_bon_champ()
    {
        var moi = await InsererAsync("EDT-STK");
        var saisie = SaisiePour(moi.ID, "EDT-STK"); saisie.StockMin = 10; saisie.StockMax = 5;
        var page = CreerPage(saisie, out _);

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        Assert.Single(Erreurs(page, "Article.StockMax"));
        Assert.Equal(0, (await LireAsync(moi.ID))!.StockMin);
    }

    [Fact]
    public async Task Decision_C_un_TTC_incoherent_est_enregistre_tel_quel_par_la_page()
    {
        var moi = await InsererAsync("EDT-TTC");
        var saisie = SaisiePour(moi.ID, "EDT-TTC"); saisie.PrixVenteHT = 100m; saisie.TauxTVA = 19m; saisie.PrixVenteTTC = 50m;
        var page = CreerPage(saisie, out _);

        var resultat = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal(50m, (await LireAsync(moi.ID))!.PrixVenteTTC);
    }

    [Fact]
    public async Task Un_article_introuvable_donne_une_page_non_trouvee()
    {
        var page = CreerPage(SaisiePour(9999, "FANTOME"), out _);

        Assert.IsType<NotFoundResult>(await page.OnPostAsync());
    }

    [Fact]
    public async Task Une_erreur_de_liaison_deja_presente_dans_le_ModelState_empeche_l_enregistrement()
    {
        // Ex. : TauxTVA envoyé vide, le classeur MVC met une erreur sur « Article.TauxTVA » avant le handler.
        var moi = await InsererAsync("EDT-BIND");
        var page = CreerPage(SaisiePour(moi.ID, "EDT-BIND"), out _);
        page.ModelState.AddModelError("Article.TauxTVA", "The value '' is invalid.");

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("Existant EDT-BIND", (await LireAsync(moi.ID))!.Libelle);
    }
}
