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
using DeleteArticleModel = GestionCommerciale.Pages.Articles.DeleteModel;
using EditArticleModel = GestionCommerciale.Pages.Articles.EditModel;
using IndexArticleModel = GestionCommerciale.Pages.Articles.IndexModel;

namespace GestionCommerciale.Tests;

/// <summary>
/// S4 étape 5 : suppression sûre d'un article.
/// Un article n'est supprimable que s'il n'est utilisé nulle part. Les tables qui référencent Articles
/// (clés étrangères, vérifiées sur la base de dev) sont : MouvementsStock, LignesBonLivraison, LignesFacture,
/// LignesBonReception, LignesCommandeFournisseur, LignesCommandeClient, LigneDevis, ArticleFournisseurs.
///
/// NOTE : la connexion SQLite est ouverte avec « Foreign Keys=False » pour pouvoir créer UNE ligne qui référence
/// l'article sans fabriquer tout le document parent (facture, client, commande…). Ces tests vérifient donc le
/// CONTRÔLE APPLICATIF du service, pas le comportement des clés étrangères de SQL Server.
/// </summary>
public class ArticleSuppressionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly ArticleService _service;

    public ArticleSuppressionTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
        _service = new ArticleService(_db);
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

    private async Task<Article> CreerArticleAsync(string reference = "REF-001", int stock = 0)
    {
        var article = new Article { Reference = reference, Libelle = "Article " + reference, Quantite = stock };
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        return article;
    }

    /// <summary>Ajoute UNE ligne qui référence l'article, dans la table correspondant à <paramref name="type"/>.</summary>
    private async Task AjouterUsageAsync(string type, int articleId)
    {
        switch (type)
        {
            case "mouvement": _db.MouvementsStock.Add(new MouvementStock { ArticleId = articleId, Quantite = 1 }); break;
            case "bl": _db.LignesBonLivraison.Add(new LigneBonLivraison { BonLivraisonId = 999, ArticleId = articleId, Quantite = 1 }); break;
            case "facture": _db.LignesFacture.Add(new LigneFacture { FactureId = 999, ArticleId = articleId }); break;
            case "br": _db.LignesBonReception.Add(new LigneBonReception { BonReceptionId = 999, ArticleId = articleId }); break;
            case "cmdfournisseur": _db.LignesCommandeFournisseur.Add(new LigneCommandeFournisseur { CommandeFournisseurId = 999, ArticleId = articleId, Quantite = 1 }); break;
            case "cmdclient": _db.LignesCommandeClient.Add(new LigneCommandeClient { CommandeClientId = 999, ArticleId = articleId, Quantite = 1 }); break;
            case "devis": _db.LigneDevis.Add(new LigneDevis { DevisId = 999, ArticleId = articleId, Designation = "x", Quantite = 1 }); break;
            case "tarif": _db.ArticleFournisseurs.Add(new ArticleFournisseur { ArticleId = articleId, FournisseurId = 999 }); break;
            default: throw new ArgumentException(type);
        }
        await _db.SaveChangesAsync();
    }

    private async Task<int> CompterUsagesAsync(string type, int articleId)
    {
        _db.ChangeTracker.Clear();
        return type switch
        {
            "mouvement" => await _db.MouvementsStock.CountAsync(x => x.ArticleId == articleId),
            "bl" => await _db.LignesBonLivraison.CountAsync(x => x.ArticleId == articleId),
            "facture" => await _db.LignesFacture.CountAsync(x => x.ArticleId == articleId),
            "br" => await _db.LignesBonReception.CountAsync(x => x.ArticleId == articleId),
            "cmdfournisseur" => await _db.LignesCommandeFournisseur.CountAsync(x => x.ArticleId == articleId),
            "cmdclient" => await _db.LignesCommandeClient.CountAsync(x => x.ArticleId == articleId),
            "devis" => await _db.LigneDevis.CountAsync(x => x.ArticleId == articleId),
            "tarif" => await _db.ArticleFournisseurs.CountAsync(x => x.ArticleId == articleId),
            _ => throw new ArgumentException(type)
        };
    }

    private async Task<bool> ArticleExisteAsync(int id)
    {
        _db.ChangeTracker.Clear();
        return await _db.Articles.AnyAsync(a => a.ID == id);
    }

    // ---------------------------------------------------------------- Service : cas autorisé / introuvable

    [Fact]
    public async Task Un_article_sans_mouvement_ni_document_est_supprime()
    {
        var article = await CreerArticleAsync("LIBRE-1");

        var resultat = await _service.SupprimerAsync(article.ID);

        Assert.True(resultat.Succes);
        Assert.Equal("LIBRE-1", resultat.Reference);
        Assert.False(await ArticleExisteAsync(article.ID));
    }

    [Fact]
    public async Task Un_article_introuvable_est_signale_et_rien_d_autre_n_est_supprime()
    {
        var autre = await CreerArticleAsync("AUTRE");

        var resultat = await _service.SupprimerAsync(9999);

        Assert.True(resultat.Introuvable);
        Assert.False(resultat.Succes);
        Assert.True(await ArticleExisteAsync(autre.ID));
    }

    // ---------------------------------------------------------------- Service : un refus par table qui référence Articles

    [Theory]
    [InlineData("mouvement", "mouvement(s) de stock")]
    [InlineData("bl", "ligne(s) de bon de livraison")]
    [InlineData("facture", "ligne(s) de facture")]
    [InlineData("br", "ligne(s) de bon de réception")]
    [InlineData("cmdfournisseur", "ligne(s) de commande fournisseur")]
    [InlineData("cmdclient", "ligne(s) de commande client")]
    [InlineData("devis", "ligne(s) de devis")]
    [InlineData("tarif", "tarif(s) fournisseur")]
    public async Task Un_article_utilise_ne_peut_pas_etre_supprime_et_rien_n_est_efface(string type, string libelleAttendu)
    {
        var article = await CreerArticleAsync("USED-1");
        await AjouterUsageAsync(type, article.ID);

        var resultat = await _service.SupprimerAsync(article.ID);

        Assert.False(resultat.Succes);
        Assert.False(resultat.Introuvable);
        Assert.Equal("USED-1", resultat.Reference);
        Assert.True(await ArticleExisteAsync(article.ID));            // l'article est conservé
        Assert.Equal(1, await CompterUsagesAsync(type, article.ID));  // la ligne qui le référence est conservée aussi

        var message = Assert.Single(resultat.Erreurs).Message;
        Assert.Contains("USED-1", message);          // dit de quel article il s'agit
        Assert.Contains(libelleAttendu, message);    // dit pourquoi
        Assert.Contains("désactiver", message);      // propose l'alternative
    }

    [Fact]
    public async Task Le_message_donne_le_nombre_d_elements_et_liste_toutes_les_raisons()
    {
        var article = await CreerArticleAsync("MULTI");
        await AjouterUsageAsync("mouvement", article.ID);
        await AjouterUsageAsync("mouvement", article.ID);
        await AjouterUsageAsync("facture", article.ID);

        var resultat = await _service.SupprimerAsync(article.ID);

        var message = Assert.Single(resultat.Erreurs).Message;
        Assert.Contains("2 mouvement(s) de stock", message);
        Assert.Contains("1 ligne(s) de facture", message);
        Assert.True(await ArticleExisteAsync(article.ID));
    }

    [Fact]
    public async Task Un_article_qui_n_a_que_des_tarifs_fournisseur_est_refuse_avec_la_consigne_de_les_retirer()
    {
        var article = await CreerArticleAsync("TARIF");
        await AjouterUsageAsync("tarif", article.ID);

        var resultat = await _service.SupprimerAsync(article.ID);

        Assert.False(resultat.Succes);
        Assert.Contains("fournisseurs associés", Assert.Single(resultat.Erreurs).Message);
    }

    [Fact]
    public async Task La_suppression_ne_touche_que_l_article_demande()
    {
        var libre = await CreerArticleAsync("LIBRE");
        var utilise = await CreerArticleAsync("UTILISE");
        await AjouterUsageAsync("mouvement", utilise.ID);
        await AjouterUsageAsync("facture", utilise.ID);

        var resultat = await _service.SupprimerAsync(libre.ID);

        Assert.True(resultat.Succes);
        Assert.False(await ArticleExisteAsync(libre.ID));
        Assert.True(await ArticleExisteAsync(utilise.ID));
        Assert.Equal(1, await CompterUsagesAsync("mouvement", utilise.ID));
        Assert.Equal(1, await CompterUsagesAsync("facture", utilise.ID));
    }

    // ---------------------------------------------------------------- Service : résumé d'utilisation (page de confirmation)

    [Fact]
    public async Task L_utilisation_d_un_article_compte_chaque_type_de_document()
    {
        var article = await CreerArticleAsync("COMPTE");
        await AjouterUsageAsync("mouvement", article.ID);
        await AjouterUsageAsync("mouvement", article.ID);
        await AjouterUsageAsync("bl", article.ID);

        var utilisation = await _service.ObtenirUtilisationAsync(article.ID);

        Assert.True(utilisation.EstUtilise);
        Assert.Contains(("mouvement(s) de stock", 2), utilisation.Elements);
        Assert.Contains(("ligne(s) de bon de livraison", 1), utilisation.Elements);
        Assert.Equal(2, utilisation.Elements.Count);
    }

    [Fact]
    public async Task Un_article_libre_n_a_aucune_utilisation()
    {
        var article = await CreerArticleAsync("LIBRE-2");

        var utilisation = await _service.ObtenirUtilisationAsync(article.ID);

        Assert.False(utilisation.EstUtilise);
        Assert.Empty(utilisation.Elements);
    }

    // ---------------------------------------------------------------- Règle du stock : suppression refusée si Quantite != 0

    [Fact]
    public async Task Un_article_dont_le_stock_est_positif_ne_peut_pas_etre_supprime()
    {
        var article = await CreerArticleAsync("STOCK-5", stock: 5);

        var resultat = await _service.SupprimerAsync(article.ID);

        Assert.False(resultat.Succes);
        Assert.True(await ArticleExisteAsync(article.ID));
        var message = Assert.Single(resultat.Erreurs).Message;
        Assert.Contains("STOCK-5", message);
        Assert.Contains("Stock non nul (5 unités) : ajustez le stock à 0 ou désactivez l'article", message);
    }

    [Fact]
    public async Task Le_message_du_stock_est_au_singulier_pour_une_seule_unite()
    {
        var article = await CreerArticleAsync("STOCK-1", stock: 1);

        var resultat = await _service.SupprimerAsync(article.ID);

        Assert.Contains("Stock non nul (1 unité) : ajustez le stock à 0 ou désactivez l'article",
            Assert.Single(resultat.Erreurs).Message);
    }

    [Fact]
    public async Task Un_article_dont_le_stock_est_negatif_ne_peut_pas_etre_supprime()
    {
        var article = await CreerArticleAsync("STOCK-NEG", stock: -2);

        var resultat = await _service.SupprimerAsync(article.ID);

        Assert.False(resultat.Succes);
        Assert.True(await ArticleExisteAsync(article.ID));
        Assert.Contains("Stock non nul (-2 unités)", Assert.Single(resultat.Erreurs).Message);
    }

    [Fact]
    public async Task Un_article_dont_le_stock_est_a_zero_est_supprime()
    {
        var article = await CreerArticleAsync("STOCK-0", stock: 0);

        var resultat = await _service.SupprimerAsync(article.ID);

        Assert.True(resultat.Succes);
        Assert.False(await ArticleExisteAsync(article.ID));
    }

    [Fact]
    public async Task Un_article_utilise_ET_en_stock_donne_les_deux_raisons_dans_un_seul_message()
    {
        var article = await CreerArticleAsync("DOUBLE", stock: 7);
        await AjouterUsageAsync("facture", article.ID);

        var resultat = await _service.SupprimerAsync(article.ID);

        var message = Assert.Single(resultat.Erreurs).Message;
        Assert.Contains("1 ligne(s) de facture", message);
        Assert.Contains("Stock non nul (7 unités)", message);
        Assert.True(await ArticleExisteAsync(article.ID));
    }

    [Fact]
    public async Task L_utilisation_expose_le_stock_et_bloque_la_suppression_meme_sans_document()
    {
        var article = await CreerArticleAsync("STOCK-USE", stock: 4);

        var utilisation = await _service.ObtenirUtilisationAsync(article.ID);

        Assert.False(utilisation.EstUtilise);          // aucun document
        Assert.Equal(4, utilisation.Stock);
        Assert.True(utilisation.StockNonNul);
        Assert.True(utilisation.SuppressionImpossible); // mais le stock bloque
    }

    // ---------------------------------------------------------------- Pages existantes (Index et Edit) : même règle

    private (HttpContext, ActionContext, TempDataDictionary) Contexte()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return (httpContext, actionContext, new TempDataDictionary(httpContext, new FauxTempData()));
    }

    private IndexArticleModel CreerIndex(out TempDataDictionary tempData)
    {
        var (_, actionContext, td) = Contexte();
        tempData = td;
        return new IndexArticleModel(_db, _service, NullLogger<IndexArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            TempData = td
        };
    }

    private EditArticleModel CreerEdit(int articleId, out TempDataDictionary tempData)
    {
        var (_, actionContext, td) = Contexte();
        tempData = td;
        return new EditArticleModel(_db, new StockService(_db), new ArticleService(_db), NullLogger<EditArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            TempData = td,
            Article = new Article { ID = articleId }
        };
    }

    [Fact]
    public async Task Index_refuse_de_supprimer_un_article_utilise()
    {
        var article = await CreerArticleAsync("IDX-USED");
        await AjouterUsageAsync("facture", article.ID);
        var page = CreerIndex(out var tempData);

        await page.OnPostDeleteAsync(article.ID, CancellationToken.None);

        Assert.True(await ArticleExisteAsync(article.ID));
        Assert.Equal(1, await CompterUsagesAsync("facture", article.ID));
        Assert.Contains("IDX-USED", tempData["ErrorMessage"] as string);
    }

    [Fact]
    public async Task Index_supprime_un_article_libre_avec_un_message_de_succes()
    {
        var article = await CreerArticleAsync("IDX-LIBRE");
        var page = CreerIndex(out var tempData);

        await page.OnPostDeleteAsync(article.ID, CancellationToken.None);

        Assert.False(await ArticleExisteAsync(article.ID));
        Assert.Contains("IDX-LIBRE", tempData["SuccessMessage"] as string);
    }

    // ---------------------------------------------------------------- Page Delete (confirmation, réelle)

    private DeleteArticleModel CreerDelete(int articleId, out TempDataDictionary tempData)
    {
        var (_, actionContext, td) = Contexte();
        tempData = td;
        return new DeleteArticleModel(_db, _service, NullLogger<DeleteArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            TempData = td,
            Id = articleId
        };
    }

    [Fact]
    public async Task Delete_GET_d_un_article_inconnu_redirige_vers_la_liste_avec_un_message()
    {
        var page = CreerDelete(9999, out var tempData);

        var resultat = await page.OnGetAsync(CancellationToken.None);

        Assert.Equal("./Index", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.Equal("Article introuvable.", tempData["ErrorMessage"]);
    }

    [Fact]
    public async Task Delete_GET_d_un_article_utilise_annonce_que_la_suppression_est_impossible()
    {
        var article = await CreerArticleAsync("DEL-USED");
        await AjouterUsageAsync("bl", article.ID);
        var page = CreerDelete(article.ID, out _);

        var resultat = await page.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultat);
        Assert.True(page.SuppressionImpossible);
        Assert.Contains("DEL-USED", page.MessageRefus);
        Assert.Contains("ligne(s) de bon de livraison", page.MessageRefus);
    }

    [Fact]
    public async Task Delete_GET_d_un_article_libre_demande_confirmation()
    {
        var article = await CreerArticleAsync("DEL-LIBRE-GET");
        var page = CreerDelete(article.ID, out _);

        var resultat = await page.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultat);
        Assert.False(page.SuppressionImpossible);
        Assert.Empty(page.MessageRefus);
    }

    [Fact]
    public async Task Delete_POST_refuse_un_article_utilise_reste_sur_la_page_et_ne_supprime_rien()
    {
        var article = await CreerArticleAsync("DEL-POST-USED");
        await AjouterUsageAsync("mouvement", article.ID);
        var page = CreerDelete(article.ID, out _);

        var resultat = await page.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultat);
        Assert.True(await ArticleExisteAsync(article.ID));
        Assert.Equal(1, await CompterUsagesAsync("mouvement", article.ID));
        var erreur = Assert.Single(page.ModelState[string.Empty]!.Errors);
        Assert.Contains("DEL-POST-USED", erreur.ErrorMessage);
        Assert.Contains("désactiver", erreur.ErrorMessage);
    }

    [Fact]
    public async Task Delete_GET_d_un_article_en_stock_annonce_que_la_suppression_est_impossible()
    {
        var article = await CreerArticleAsync("DEL-STOCK-GET", stock: 3);
        var page = CreerDelete(article.ID, out _);

        var resultat = await page.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultat);
        Assert.True(page.SuppressionImpossible);
        Assert.Contains("Stock non nul (3 unités)", page.MessageRefus);
    }

    [Fact]
    public async Task Delete_POST_refuse_un_article_en_stock_reste_sur_la_page_et_ne_supprime_rien()
    {
        var article = await CreerArticleAsync("DEL-STOCK-POST", stock: 3);
        var page = CreerDelete(article.ID, out _);

        var resultat = await page.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultat);
        Assert.True(await ArticleExisteAsync(article.ID));
        Assert.Contains("Stock non nul (3 unités)", Assert.Single(page.ModelState[string.Empty]!.Errors).ErrorMessage);
    }

    [Fact]
    public async Task Delete_POST_supprime_un_article_dont_le_stock_est_a_zero()
    {
        var article = await CreerArticleAsync("DEL-STOCK-ZERO", stock: 0);
        var page = CreerDelete(article.ID, out var tempData);

        var resultat = await page.OnPostAsync(CancellationToken.None);

        Assert.Equal("./Index", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.False(await ArticleExisteAsync(article.ID));
        Assert.Equal("Article DEL-STOCK-ZERO supprimé.", tempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Index_refuse_de_supprimer_un_article_en_stock()
    {
        var article = await CreerArticleAsync("IDX-STOCK", stock: 3);
        var page = CreerIndex(out var tempData);

        await page.OnPostDeleteAsync(article.ID, CancellationToken.None);

        Assert.True(await ArticleExisteAsync(article.ID));
        Assert.Contains("Stock non nul (3 unités)", tempData["ErrorMessage"] as string);
    }

    [Fact]
    public async Task Edit_refuse_de_supprimer_un_article_en_stock_et_reste_sur_la_fiche()
    {
        var article = await CreerArticleAsync("EDT-STOCK", stock: 3);
        var page = CreerEdit(article.ID, out var tempData);

        var resultat = await page.OnPostDeleteAsync();

        Assert.Equal("./Edit", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.True(await ArticleExisteAsync(article.ID));
        Assert.Contains("Stock non nul (3 unités)", tempData["ErrorMessage"] as string);
    }

    [Fact]
    public async Task Delete_POST_supprime_un_article_libre_et_retourne_a_la_liste()
    {
        var article = await CreerArticleAsync("DEL-POST-LIBRE");
        var page = CreerDelete(article.ID, out var tempData);

        var resultat = await page.OnPostAsync(CancellationToken.None);

        Assert.Equal("./Index", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.False(await ArticleExisteAsync(article.ID));
        Assert.Equal("Article DEL-POST-LIBRE supprimé.", tempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Delete_POST_d_un_article_inconnu_redirige_vers_la_liste_avec_un_message()
    {
        var page = CreerDelete(9999, out var tempData);

        var resultat = await page.OnPostAsync(CancellationToken.None);

        Assert.Equal("./Index", Assert.IsType<RedirectToPageResult>(resultat).PageName);
        Assert.Equal("Article introuvable.", tempData["ErrorMessage"]);
    }

    [Fact]
    public async Task Edit_refuse_de_supprimer_un_article_utilise_et_reste_sur_la_fiche()
    {
        var article = await CreerArticleAsync("EDT-USED");
        await AjouterUsageAsync("mouvement", article.ID);
        var page = CreerEdit(article.ID, out var tempData);

        var resultat = await page.OnPostDeleteAsync();

        var redirection = Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal("./Edit", redirection.PageName);
        Assert.True(await ArticleExisteAsync(article.ID));
        Assert.Equal(1, await CompterUsagesAsync("mouvement", article.ID));
        Assert.Contains("EDT-USED", tempData["ErrorMessage"] as string);
    }

    [Fact]
    public async Task Edit_supprime_un_article_libre_et_retourne_a_la_liste()
    {
        var article = await CreerArticleAsync("EDT-LIBRE");
        var page = CreerEdit(article.ID, out var tempData);

        var resultat = await page.OnPostDeleteAsync();

        var redirection = Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal("./Index", redirection.PageName);
        Assert.False(await ArticleExisteAsync(article.ID));
        Assert.Contains("EDT-LIBRE", tempData["SuccessMessage"] as string);
    }
}
