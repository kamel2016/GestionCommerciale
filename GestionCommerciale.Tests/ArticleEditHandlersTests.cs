using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using EditArticleModel = GestionCommerciale.Pages.Articles.EditModel;

namespace GestionCommerciale.Tests;

/// <summary>
/// Les AUTRES handlers de la fiche article (mouvement de stock, retrait de fournisseur, suppression, enregistrement
/// principal) ne doivent pas être bloqués par les erreurs de validation d'objets qu'ils n'utilisent pas.
/// Chaque test envoie une « requête réduite » : Article.ID + les seuls champs du handler. MVC valide pourtant l'objet
/// Article lié automatiquement (Libelle et Reference vides =&gt; erreurs), ce que le test reproduit en validant les objets liés.
/// </summary>
public class ArticleEditHandlersTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;

    public ArticleEditHandlersTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private async Task<Article> CreerArticleAsync(string reference = "REF-H1", int stock = 0)
    {
        var article = new Article { Reference = reference, Libelle = "Article " + reference, Quantite = stock };
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return article;
    }

    /// <summary>Page avec de vrais services MVC ; Article ne contient que son ID (requête réduite).</summary>
    private EditArticleModel CreerPage(Article article, out TempDataDictionary tempData)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvc();
        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        tempData = new TempDataDictionary(httpContext, new FauxTempData());

        var page = new EditArticleModel(_db, new StockService(_db), new ArticleService(_db), NullLogger<EditArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            MetadataProvider = sp.GetRequiredService<IModelMetadataProvider>(),
            TempData = tempData,
            Article = article
        };
        return page;
    }

    /// <summary>Validation automatique des objets liés avant le handler (ce que fait MVC pour les [BindProperty]).</summary>
    private static void ValiderCommeMvc(EditArticleModel page)
    {
        page.TryValidateModel(page.Article, nameof(EditArticleModel.Article));
        page.TryValidateModel(page.NewSupplier, nameof(EditArticleModel.NewSupplier));
        page.TryValidateModel(page.NewStock, nameof(EditArticleModel.NewStock));
    }

    [Fact]
    public async Task Controle_la_requete_reduite_provoque_bien_des_erreurs_Article_dans_le_ModelState()
    {
        // Garde-fou du dispositif : sans lui, les autres tests ne prouveraient rien.
        var article = await CreerArticleAsync();
        var page = CreerPage(new Article { ID = article.ID }, out _);

        ValiderCommeMvc(page);

        Assert.False(page.ModelState.IsValid);
        Assert.Contains("Article.Reference", page.ModelState.Keys);
        Assert.Contains("Article.Libelle", page.ModelState.Keys);
    }

    [Fact]
    public async Task Mouvement_de_stock_n_est_pas_bloque_par_les_erreurs_Article()
    {
        var article = await CreerArticleAsync("STK", stock: 10);
        var page = CreerPage(new Article { ID = article.ID }, out var tempData);
        page.NewStock = new EditArticleModel.StockInput { Type = TypeMouvementStock.EntreeReception, Quantite = 5, Commentaire = "test" };
        ValiderCommeMvc(page);

        await page.OnPostAddStockMovementAsync();

        Assert.Null(tempData["ErrorMessage"]);
        Assert.Equal("Mouvement de stock enregistré.", tempData["SuccessMessage"]);
        _db.ChangeTracker.Clear();
        Assert.Equal(15, (await _db.Articles.SingleAsync(a => a.ID == article.ID)).Quantite);
    }

    [Fact]
    public async Task Retrait_de_fournisseur_n_est_pas_bloque_par_les_erreurs_Article()
    {
        var article = await CreerArticleAsync("RET");
        var fournisseur = new Fournisseur { Nom = "F" };
        _db.Fournisseurs.Add(fournisseur);
        await _db.SaveChangesAsync();
        _db.ArticleFournisseurs.Add(new ArticleFournisseur { ArticleId = article.ID, FournisseurId = fournisseur.Id });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var page = CreerPage(new Article { ID = article.ID }, out var tempData);
        ValiderCommeMvc(page);

        await page.OnPostRemoveSupplierAsync(fournisseur.Id);

        Assert.Equal("Fournisseur retiré.", tempData["SuccessMessage"]);
        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.ArticleFournisseurs.Where(af => af.ArticleId == article.ID).ToListAsync());
    }

    [Theory]
    [InlineData(1)]   // fournisseur qui existe mais n'est pas (ou plus) lié à l'article
    [InlineData(999)] // fournisseur inconnu
    [InlineData(0)]   // supplierId absent de la requête : le paramètre vaut 0
    public async Task Retrait_d_un_fournisseur_non_lie_affiche_un_message_d_erreur_au_lieu_de_ne_rien_dire(int supplierId)
    {
        var article = await CreerArticleAsync("NONLIE");
        _db.Fournisseurs.Add(new Fournisseur { Nom = "F" });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var page = CreerPage(new Article { ID = article.ID }, out var tempData);

        var resultat = await page.OnPostRemoveSupplierAsync(supplierId);

        Assert.Equal("./Edit", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.Equal("Ce fournisseur n'est plus lié à l'article.", tempData["ErrorMessage"]); // avant correction : null (silence)
        Assert.Null(tempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Retrait_d_un_fournisseur_lie_a_un_AUTRE_article_ne_le_retire_pas_et_affiche_une_erreur()
    {
        var article = await CreerArticleAsync("A1");
        var autre = await CreerArticleAsync("A2");
        var fournisseur = new Fournisseur { Nom = "F" };
        _db.Fournisseurs.Add(fournisseur);
        await _db.SaveChangesAsync();
        _db.ArticleFournisseurs.Add(new ArticleFournisseur { ArticleId = autre.ID, FournisseurId = fournisseur.Id });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var page = CreerPage(new Article { ID = article.ID }, out var tempData);

        await page.OnPostRemoveSupplierAsync(fournisseur.Id);

        Assert.Equal("Ce fournisseur n'est plus lié à l'article.", tempData["ErrorMessage"]);
        _db.ChangeTracker.Clear();
        Assert.Single(await _db.ArticleFournisseurs.Where(af => af.ArticleId == autre.ID).ToListAsync()); // le lien de l'autre article est intact
    }

    [Fact]
    public async Task Suppression_n_est_pas_bloquee_par_les_erreurs_Article()
    {
        var article = await CreerArticleAsync("SUP");
        var page = CreerPage(new Article { ID = article.ID }, out var tempData);
        ValiderCommeMvc(page);

        var resultat = await page.OnPostDeleteAsync();

        Assert.Equal("./Index", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.Equal("Article SUP supprimé.", tempData["SuccessMessage"]);
        _db.ChangeTracker.Clear();
        Assert.False(await _db.Articles.AnyAsync(a => a.ID == article.ID));
    }

    [Fact]
    public async Task Enregistrement_principal_n_est_pas_bloque_par_des_erreurs_NewSupplier_ou_NewStock()
    {
        // Le formulaire principal envoie les champs Article.* complets ; NewSupplier et NewStock ne sont pas postés
        // (leurs fenêtres sont hors du formulaire). On y ajoute des erreurs sur ces deux clés : le handler doit les ignorer.
        var article = await CreerArticleAsync("MAIN");
        var saisie = new Article
        {
            ID = article.ID, Reference = "MAIN", Libelle = "Libellé modifié", Unite = "Pièce",
            PrixAchatHT = 10m, PrixVenteHT = 12m, PrixVenteTTC = 14.28m, TauxTVA = 19m, StockMin = 1
        };
        var page = CreerPage(saisie, out var tempData);
        ValiderCommeMvc(page);
        page.ModelState.AddModelError("NewSupplier.PrixGrosHT", "erreur parasite");
        page.ModelState.AddModelError("NewStock.Quantite", "erreur parasite");

        var resultat = await page.OnPostAsync();

        Assert.Equal("./Edit", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.Equal("Article enregistré avec succès.", tempData["SuccessMessage"]);
        _db.ChangeTracker.Clear();
        Assert.Equal("Libellé modifié", (await _db.Articles.SingleAsync(a => a.ID == article.ID)).Libelle);
    }
}
