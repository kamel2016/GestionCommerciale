using GestionCommerciale.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using CreateArticleModel = GestionCommerciale.Pages.Articles.CreateModel;

namespace GestionCommerciale.Tests;

/// <summary>S4 étape 3 : la page Articles/Create passe par ArticleService (règles, prix à la création, plus d'exception).</summary>
public class ArticleCreatePageTests : ArticleTestBase
{
    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private CreateArticleModel CreerPage(Article saisie, out TempDataDictionary tempData)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        tempData = new TempDataDictionary(httpContext, new FauxTempData());

        return new CreateArticleModel(Db, Service, NullLogger<CreateArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            TempData = tempData,
            Article = saisie
        };
    }

    private static List<string> Erreurs(CreateArticleModel page, string cle)
        => page.ModelState.TryGetValue(cle, out var entree) ? entree.Errors.Select(e => e.ErrorMessage).ToList() : new();

    [Fact]
    public async Task Une_creation_valide_avec_les_trois_prix_redirige_vers_la_liste()
    {
        var saisie = Saisie("CRE-1", "Câble");
        saisie.PrixAchatHT = 8m; saisie.PrixVenteHT = 10m; saisie.PrixVenteTTC = 11.9m;
        var page = CreerPage(saisie, out var tempData);

        var resultat = await page.OnPostAsync();

        Assert.Equal("./Index", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.Equal("L'article Câble a été créé avec succès.", tempData["SuccessMessage"]);
        var enregistre = await Db.Articles.AsNoTracking().SingleAsync(a => a.Reference == "CRE-1");
        Assert.Equal((8m, 10m, 11.9m), (enregistre.PrixAchatHT, enregistre.PrixVenteHT, enregistre.PrixVenteTTC));
    }

    [Fact]
    public async Task Decision_C_un_TTC_incoherent_avec_le_HT_est_enregistre_tel_quel()
    {
        var saisie = Saisie("CRE-TTC");
        saisie.PrixVenteHT = 100m; saisie.TauxTVA = 19m; saisie.PrixVenteTTC = 42m;
        var page = CreerPage(saisie, out _);

        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());

        Assert.Equal(42m, (await Db.Articles.AsNoTracking().SingleAsync(a => a.Reference == "CRE-TTC")).PrixVenteTTC);
    }

    [Fact]
    public async Task Les_textes_sont_nettoyes_a_la_creation()
    {
        var saisie = Saisie("  CRE-ESP  ", "  Libellé  ");
        saisie.CodeBarre = "  ";
        var page = CreerPage(saisie, out _);

        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());

        var enregistre = await Db.Articles.AsNoTracking().SingleAsync();
        Assert.Equal("CRE-ESP", enregistre.Reference);
        Assert.Equal("Libellé", enregistre.Libelle);
        Assert.Null(enregistre.CodeBarre);
    }

    [Fact]
    public async Task Un_doublon_de_reference_est_refuse_sur_le_champ_sans_rien_creer()
    {
        await InsererAsync("CRE-DUP");
        var page = CreerPage(Saisie("cre-dup"), out _);   // casse différente

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("Cette référence existe déjà.", Assert.Single(Erreurs(page, "Article.Reference")));
        Assert.Equal(1, await CompterArticlesAsync());
    }

    [Fact]
    public async Task Une_categorie_inconnue_ne_leve_pas_d_exception_et_affiche_une_erreur_sur_le_champ()
    {
        var saisie = Saisie("CRE-CAT"); saisie.CategoryId = 9999;
        var page = CreerPage(saisie, out _);

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("Catégorie introuvable.", Assert.Single(Erreurs(page, "Article.CategoryId")));
        Assert.Equal(0, await CompterArticlesAsync());
    }

    [Fact]
    public async Task Un_doublon_simultane_ne_leve_pas_d_exception_et_affiche_une_erreur_sur_le_champ()
    {
        var page = CreerPage(Saisie("CRE-COURSE"), out _);
        Interception.Action = async () =>
        {
            using var autre = AutreSession();
            autre.Articles.Add(new Article { Reference = "CRE-COURSE", Libelle = "Arrivé en premier" });
            await autre.SaveChangesAsync();
        };

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("Cette référence existe déjà.", Assert.Single(Erreurs(page, "Article.Reference")));
        Assert.Equal(1, await CompterArticlesAsync());
    }

    [Fact]
    public async Task Une_tva_hors_limites_est_refusee_sur_le_champ()
    {
        var saisie = Saisie("CRE-TVA"); saisie.TauxTVA = 150m;
        var page = CreerPage(saisie, out _);

        Assert.IsType<PageResult>(await page.OnPostAsync());
        Assert.Single(Erreurs(page, "Article.TauxTVA"));
        Assert.Equal(0, await CompterArticlesAsync());
    }

    [Fact]
    public async Task Une_erreur_de_liaison_deja_presente_empeche_la_creation()
    {
        // Ex. : prix ou TVA envoyés vides ou illisibles : le classeur MVC a déjà mis une erreur sur « Article.TauxTVA ».
        var page = CreerPage(Saisie("CRE-BIND"), out _);
        page.ModelState.AddModelError("Article.TauxTVA", "The value '' is invalid.");

        Assert.IsType<PageResult>(await page.OnPostAsync());
        Assert.Equal(0, await CompterArticlesAsync());
    }

    [Fact]
    public async Task Le_stock_et_le_CMUP_postes_sont_ignores()
    {
        var saisie = Saisie("CRE-FORGE"); saisie.Quantite = 50; saisie.CMUP = 7m;
        var page = CreerPage(saisie, out _);

        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());

        var enregistre = await Db.Articles.AsNoTracking().SingleAsync(a => a.Reference == "CRE-FORGE");
        Assert.Equal(0, enregistre.Quantite);
        Assert.Equal(0m, enregistre.CMUP);
    }

    [Fact]
    public async Task L_affichage_du_formulaire_propose_les_valeurs_par_defaut()
    {
        var page = CreerPage(new Article(), out _);

        await page.OnGetAsync();

        Assert.Equal(19m, page.Article.TauxTVA);
        Assert.Equal("Pièce", page.Article.Unite);
        Assert.True(page.Article.EstActif);
    }
}
