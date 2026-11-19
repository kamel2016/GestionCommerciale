using GestionCommerciale.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using DetailsArticleModel = GestionCommerciale.Pages.Articles.DetailsModel;

namespace GestionCommerciale.Tests;

/// <summary>S4 étape 6 : la page Articles/Details affiche la fiche réelle ou une page non trouvée.</summary>
public class ArticleDetailsPageTests : ArticleTestBase
{
    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private DetailsArticleModel CreerPage(int? id)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return new DetailsArticleModel(Service, NullLogger<DetailsArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            TempData = new TempDataDictionary(httpContext, new FauxTempData()),
            Id = id
        };
    }

    [Fact]
    public async Task Un_article_existant_affiche_sa_vraie_fiche()
    {
        var article = await InsererAsync("DET-1");
        var page = CreerPage(article.ID);

        var resultat = await page.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("DET-1", page.Fiche.Article.Reference);
        Assert.NotEqual("LAPTOP-DELL-XPS13", page.Fiche.Article.Reference);   // plus de données inventées
    }

    [Theory]
    [InlineData(9999)]   // inexistant
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(null)]   // identifiant absent de l'adresse
    public async Task Un_article_inexistant_ou_un_identifiant_invalide_donne_une_page_non_trouvee(int? id)
    {
        var page = CreerPage(id);

        Assert.IsType<NotFoundResult>(await page.OnGetAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(TypeMouvementStock.EntreeReception, "Entrée (réception fournisseur)")]
    [InlineData(TypeMouvementStock.SortieLivraison, "Sortie (livraison client)")]
    [InlineData(TypeMouvementStock.AjustementInventaire, "Ajustement d'inventaire")]
    [InlineData(TypeMouvementStock.RetourFournisseur, "Retour fournisseur")]
    public void Le_type_de_mouvement_est_affiche_en_francais(TypeMouvementStock type, string attendu)
        => Assert.Equal(attendu, DetailsArticleModel.LibelleType(type));

    [Theory]
    [InlineData(8, 12, 50)]     // (12 - 8) / 8 = 50 %
    [InlineData(10, 10, 0)]
    [InlineData(0, 12, 0)]      // prix de gros nul : pas de division par zéro
    public void La_marge_detail_suit_la_formule_de_la_fiche_de_modification(decimal gros, decimal detail, decimal attendu)
        => Assert.Equal(attendu, DetailsArticleModel.MargeDetail(gros, detail));
}
