using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using EditArticleModel = GestionCommerciale.Pages.Articles.EditModel;

namespace GestionCommerciale.Tests;

/// <summary>
/// S4 étape 1 : reproduit le bug de signe des mouvements de stock saisis à la main
/// dans la fiche article (Pages/Articles/Edit.cshtml.cs, handler AddStockMovement).
/// Convention attendue (celle de IStockService et de MouvementStock.Quantite) :
/// quantité enregistrée TOUJOURS positive, le sens (entrée ou sortie) est porté par le type (S6 étape 2).
/// Ces tests doivent ÉCHOUER tant que le bug n'est pas corrigé (étape 2).
/// </summary>
public class ArticleStockPageTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;

    public ArticleStockPageTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

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

    private async Task<int> CreerArticleAsync(int stock)
    {
        var article = new Article { Reference = "REF-" + Guid.NewGuid().ToString("N")[..8], Libelle = "Article test", Quantite = stock };
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        return article.ID;
    }

    private EditArticleModel CreerPage(int articleId, TypeMouvementStock type, int quantite, string? motif = "test")
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        return new EditArticleModel(_db, new StockService(_db), new ArticleService(_db), NullLogger<EditArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            TempData = new TempDataDictionary(httpContext, new FauxTempData()),
            Article = new Article { ID = articleId },
            NewStock = new EditArticleModel.StockInput { Type = type, Quantite = quantite, Commentaire = motif }
        };
    }

    /// <summary>Relit l'article et son dernier mouvement depuis la base (contexte vidé).</summary>
    private async Task<(int Stock, List<MouvementStock> Mouvements)> LireAsync(int articleId)
    {
        _db.ChangeTracker.Clear();
        var stock = (await _db.Articles.AsNoTracking().SingleAsync(a => a.ID == articleId)).Quantite;
        var mouvements = await _db.MouvementsStock.AsNoTracking().Where(m => m.ArticleId == articleId).ToListAsync();
        return (stock, mouvements);
    }

    [Fact]
    public async Task Une_entree_de_reception_AUGMENTE_le_stock()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.EntreeReception, 5).OnPostAddStockMovementAsync();

        var (stock, _) = await LireAsync(id);
        Assert.Equal(15, stock); // bug actuel : 5
    }

    [Fact]
    public async Task Une_entree_de_reception_sur_un_stock_vide_est_acceptee()
    {
        var id = await CreerArticleAsync(stock: 0);

        await CreerPage(id, TypeMouvementStock.EntreeReception, 5).OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(5, stock);                 // bug actuel : refusée « Stock insuffisant », stock 0
        Assert.Single(mouvements);
    }

    [Fact]
    public async Task Le_mouvement_d_une_entree_est_enregistre_avec_une_quantite_positive()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.EntreeReception, 5).OnPostAddStockMovementAsync();

        var (_, mouvements) = await LireAsync(id);
        var mouvement = Assert.Single(mouvements);
        Assert.Equal(5, mouvement.Quantite);     // bug actuel : -5
        Assert.Equal(15, mouvement.QuantiteApres); // bug actuel : 5
    }

    [Fact]
    public async Task Le_mouvement_enregistre_la_quantite_AVANT_et_APRES()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.EntreeReception, 5).OnPostAddStockMovementAsync();

        var (_, mouvements) = await LireAsync(id);
        var mouvement = Assert.Single(mouvements);
        Assert.Equal(10, mouvement.QuantiteAvant); // bug actuel : jamais renseignée (0)
        Assert.Equal(15, mouvement.QuantiteApres);
    }

    [Fact]
    public async Task Un_retour_client_AUGMENTE_le_stock()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.RetourClient, 3).OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(13, stock);
        Assert.Equal(3, Assert.Single(mouvements).Quantite);
    }

    [Fact]
    public async Task Un_retour_fournisseur_DIMINUE_le_stock()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.RetourFournisseur, 4).OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(6, stock);                          // bug actuel : 14
        Assert.Equal(4, Assert.Single(mouvements).Quantite);  // positive : le sens est porté par le type
    }

    [Fact]
    public async Task Un_retour_fournisseur_superieur_au_stock_est_refuse()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.RetourFournisseur, 20).OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);       // bug actuel : 30 (accepté, stock augmenté)
        Assert.Empty(mouvements);
    }

    // ---- Ajustement d'inventaire : deux types (positif, négatif), quantité positive, motif obligatoire ----

    [Fact]
    public async Task Un_ajustement_positif_augmente_le_stock()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.AjustementPositif, 4, "Retrouvé au comptage").OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(14, stock);
        var mouvement = Assert.Single(mouvements);
        Assert.Equal(4, mouvement.Quantite);
        Assert.Equal(10, mouvement.QuantiteAvant);
        Assert.Equal(14, mouvement.QuantiteApres);
        Assert.Equal("Retrouvé au comptage", mouvement.Commentaire);
    }

    [Fact]
    public async Task Un_ajustement_negatif_diminue_le_stock()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.AjustementNegatif, 3, "Casse").OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(7, stock);                       // bug actuel : 10 (quantité négative refusée)
        var mouvement = Assert.Single(mouvements);
        Assert.Equal(3, mouvement.Quantite);   // positive : le sens est porté par le type
        Assert.Equal(10, mouvement.QuantiteAvant);
        Assert.Equal(7, mouvement.QuantiteApres);
    }

    [Fact]
    public async Task Un_ajustement_negatif_egal_au_stock_est_accepte_et_laisse_zero()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.AjustementNegatif, 10, "Inventaire à zéro").OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(0, stock);                       // bug actuel : 10
        Assert.Single(mouvements);
    }

    [Fact]
    public async Task Un_ajustement_qui_rendrait_le_stock_negatif_est_refuse()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.AjustementNegatif, 11, "Erreur de saisie").OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);
        Assert.Empty(mouvements);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_ajustement_sans_motif_est_refuse(string? motif)
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.AjustementPositif, 4, motif).OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);                      // bug actuel : 14 (motif ignoré)
        Assert.Empty(mouvements);
    }

    [Fact]
    public async Task Un_ajustement_de_zero_est_refuse()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.AjustementPositif, 0, "Rien").OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);
        Assert.Empty(mouvements);
    }

    [Fact]
    public async Task La_sortie_livraison_est_reservee_aux_bons_de_livraison_et_refusee_depuis_la_fiche_article()
    {
        var id = await CreerArticleAsync(stock: 10);

        await CreerPage(id, TypeMouvementStock.SortieLivraison, 2).OnPostAddStockMovementAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);       // bug actuel : 12 (une « sortie » augmente le stock)
        Assert.Empty(mouvements);
    }
}
