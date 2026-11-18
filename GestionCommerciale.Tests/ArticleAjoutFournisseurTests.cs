using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
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
/// Bug signalé : sur la fiche article, « Ajouter un fournisseur » répond « Données fournisseur invalides. »
/// alors que les champs saisis sont corrects.
///
/// Constaté sur l'application réelle (log temporaire, retiré ensuite) : le ModelState invalide ne contenait
/// QUE « Article.Libelle » et « Article.Reference » (champs [Required]), et rien pour NewSupplier.
/// La fenêtre d'ajout n'envoie que Article.ID + NewSupplier.* : MVC valide pourtant l'objet Article lié
/// automatiquement (il est en [BindProperty]), d'où ces deux erreurs ; et TryValidateModel(NewSupplier) renvoie
/// faux dès que le ModelState GLOBAL contient une erreur.
///
/// Dans ce test, la validation automatique de MVC est reproduite en validant les objets liés
/// (TryValidateModel sur Article), comme le fait le framework avant d'appeler le handler.
/// </summary>
public class ArticleAjoutFournisseurTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;

    public ArticleAjoutFournisseurTests()
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

    private async Task<(int ArticleId, int FournisseurId)> CreerArticleEtFournisseurAsync()
    {
        var article = new Article { Reference = "REF-F1", Libelle = "Article fournisseur" };
        var fournisseur = new Fournisseur { Nom = "Fournisseur test" };
        _db.Articles.Add(article);
        _db.Fournisseurs.Add(fournisseur);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return (article.ID, fournisseur.Id);
    }

    /// <summary>
    /// Page telle que MVC la remplit quand la fenêtre « Ajouter un fournisseur » est validée :
    /// Article.ID seulement (le reste de Article garde ses valeurs par défaut) + NewSupplier.* saisis.
    /// </summary>
    private EditArticleModel CreerPage(int articleId, int fournisseurId, string? referenceEtLibellePostes, out TempDataDictionary tempData)
    {
        // Vrais services MVC (métadonnées + validation par annotations) : TryValidateModel en a besoin.
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
            Article = referenceEtLibellePostes == null
                ? new Article { ID = articleId }
                : new Article { ID = articleId, Reference = referenceEtLibellePostes, Libelle = referenceEtLibellePostes },
            // Valeurs saisies dans la fenêtre : Prix Gros HT, Prix Détail HT, Prix Promo HT, Remise max (%)
            NewSupplier = new EditArticleModel.SupplierInput
            {
                FournisseurId = fournisseurId,
                PrixGrosHT = 100m,
                PrixDetailHT = 120m,
                PrixPromoHT = 90m,
                RemiseMax = 5m
            }
        };

        // Validation automatique des objets liés (= ce que fait MVC avant le handler).
        page.TryValidateModel(page.Article, nameof(EditArticleModel.Article));
        page.TryValidateModel(page.NewSupplier, nameof(EditArticleModel.NewSupplier));
        return page;
    }

    [Fact]
    public async Task Ajouter_un_fournisseur_avec_des_valeurs_correctes_fonctionne_meme_si_seul_Article_ID_est_poste()
    {
        var (articleId, fournisseurId) = await CreerArticleEtFournisseurAsync();
        var page = CreerPage(articleId, fournisseurId, null, out var tempData);

        var resultat = await page.OnPostAddSupplierAsync();

        Assert.Equal("./Edit", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.Null(tempData["ErrorMessage"]);                         // valeur obtenue avant correction : « Données fournisseur invalides. »
        Assert.Equal("Fournisseur ajouté.", tempData["SuccessMessage"]);

        _db.ChangeTracker.Clear();
        var lien = Assert.Single(await _db.ArticleFournisseurs.Where(af => af.ArticleId == articleId).ToListAsync());
        Assert.Equal(fournisseurId, lien.FournisseurId);
        Assert.Equal(100m, lien.PrixGrosHT);
        Assert.Equal(120m, lien.PrixDetailHT);
        Assert.Equal(90m, lien.PrixPromoHT);
        Assert.Equal(5m, lien.RemiseMax);
    }

    [Fact]
    public async Task Controle_si_Reference_et_Libelle_sont_postes_l_ajout_fonctionne_deja()
    {
        // Garde-fou : isole la cause. Avec les champs Article requis présents, le même ajout passe aujourd'hui.
        var (articleId, fournisseurId) = await CreerArticleEtFournisseurAsync();
        var page = CreerPage(articleId, fournisseurId, "REF-F1", out var tempData);

        await page.OnPostAddSupplierAsync();

        Assert.Null(tempData["ErrorMessage"]);
        Assert.Equal("Fournisseur ajouté.", tempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Une_valeur_illisible_dans_NewSupplier_reste_refusee()
    {
        // Le classeur MVC signale une valeur illisible (« abc », ou « 100.000 » sans le champ __Invariant) par une erreur
        // de ModelState sur la clé NewSupplier.PrixGrosHT : on la reproduit ici, le classeur n'étant pas exécuté dans ce test.
        // (Constaté sur l'application réelle : « The value '100.000' is not valid for PrixGrosHT. »)
        var (articleId, fournisseurId) = await CreerArticleEtFournisseurAsync();
        var page = CreerPage(articleId, fournisseurId, null, out var tempData);
        page.ModelState.AddModelError("NewSupplier.PrixGrosHT", "The value 'abc' is not valid for PrixGrosHT.");

        await page.OnPostAddSupplierAsync();

        Assert.Equal("Données fournisseur invalides.", tempData["ErrorMessage"]);
        Assert.Null(tempData["SuccessMessage"]);
        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.ArticleFournisseurs.Where(af => af.ArticleId == articleId).ToListAsync());
    }

    [Fact]
    public async Task Un_FournisseurId_illisible_ou_vide_dans_NewSupplier_reste_refuse()
    {
        // Champ envoyé vide (« NewSupplier.FournisseurId= ») : le classeur MVC met une erreur sur cette clé.
        var (articleId, fournisseurId) = await CreerArticleEtFournisseurAsync();
        var page = CreerPage(articleId, fournisseurId, null, out var tempData);
        page.ModelState.AddModelError("NewSupplier.FournisseurId", "The value '' is invalid.");

        await page.OnPostAddSupplierAsync();

        Assert.Equal("Données fournisseur invalides.", tempData["ErrorMessage"]);
        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.ArticleFournisseurs.Where(af => af.ArticleId == articleId).ToListAsync());
    }

    [Fact]
    public async Task Un_FournisseurId_absent_donc_a_zero_ne_provoque_pas_d_exception_et_ne_cree_rien()
    {
        // Champ NewSupplier.FournisseurId absent de la requête : pas d'erreur de classeur, la valeur reste 0
        // ([Required] ne protège pas un int non nul). On vérifie le comportement actuel, sans le supposer.
        var (articleId, _) = await CreerArticleEtFournisseurAsync();
        var page = CreerPage(articleId, 0, null, out var tempData);

        var resultat = await page.OnPostAddSupplierAsync();

        Assert.Equal("./Edit", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.NotNull(tempData["ErrorMessage"]);
        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.ArticleFournisseurs.Where(af => af.ArticleId == articleId).ToListAsync());
    }

    [Fact]
    public async Task Un_vrai_defaut_de_saisie_fournisseur_reste_refuse()
    {
        // Non-régression de la correction : une vraie erreur de saisie (prix négatif) doit rester refusée.
        var (articleId, _) = await CreerArticleEtFournisseurAsync();
        var page = CreerPage(articleId, 1, null, out var tempData);
        page.NewSupplier.PrixGrosHT = -5m; // invalide : [Range(0, 9999999)]

        await page.OnPostAddSupplierAsync();

        Assert.Equal("Données fournisseur invalides.", tempData["ErrorMessage"]);
        _db.ChangeTracker.Clear();
        Assert.Empty(await _db.ArticleFournisseurs.Where(af => af.ArticleId == articleId).ToListAsync());
    }
}
