using System.Reflection;
using System.Text.RegularExpressions;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using IndexArticleModel = GestionCommerciale.Pages.Articles.IndexModel;

namespace GestionCommerciale.Tests;

/// <summary>S4 étape 7 : la liste des articles (filtres, tris, pagination, colonnes) lit de vraies données.</summary>
public class ArticleIndexPageTests : ArticleTestBase
{
    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private IndexArticleModel CreerPage()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return new IndexArticleModel(Db, Service, NullLogger<IndexArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            TempData = new TempDataDictionary(httpContext, new FauxTempData())
        };
    }

    private async Task<Article> Ajouter(string reference, string libelle = "Libellé", int stock = 10, int stockMin = 5,
        int? categorieId = null, bool actif = true, string? marque = null, string? modele = null, string? codeBarre = null, DateTime? creation = null)
    {
        var article = new Article
        {
            Reference = reference, Libelle = libelle, Quantite = stock, StockMin = stockMin, CategoryId = categorieId,
            EstActif = actif, Marque = marque, Modele = modele, CodeBarre = codeBarre, DateCreation = creation ?? DateTime.Now
        };
        Db.Articles.Add(article);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return article;
    }

    private static string[] Refs(IndexArticleModel page) => page.Articles.Select(a => a.Reference).ToArray();

    // ------------------------------------------------------------------ catégories : vraie table

    [Fact]
    public async Task La_liste_des_categories_vient_de_la_base_triee_par_nom_et_non_d_une_liste_en_dur()
    {
        await InsererCategorieAsync("Outillage");
        await InsererCategorieAsync("Câblage");
        var page = CreerPage();

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(new[] { "Câblage", "Outillage" }, page.Categories.Select(c => c.Text).ToArray());   // avant : 6 noms en dur
        Assert.DoesNotContain(page.Categories, c => c.Text == "Informatique");
    }

    [Fact]
    public async Task Le_filtre_categorie_ne_garde_que_les_articles_de_cette_categorie()
    {
        var cables = await InsererCategorieAsync("Câblage");
        var outils = await InsererCategorieAsync("Outillage");
        await Ajouter("A-CABLE", categorieId: cables);
        await Ajouter("B-OUTIL", categorieId: outils);
        await Ajouter("C-SANS");
        var page = CreerPage();
        page.SelectedCategory = cables.ToString();

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(new[] { "A-CABLE" }, Refs(page));
        Assert.Equal(1, page.TotalRecords);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("99999")]
    public async Task Un_filtre_categorie_illisible_est_ignore_et_un_inconnu_ne_donne_rien_de_faux(string valeur)
    {
        var cat = await InsererCategorieAsync("Câblage");
        await Ajouter("A", categorieId: cat);
        await Ajouter("B");
        var page = CreerPage();
        page.SelectedCategory = valeur;

        await page.OnGetAsync(CancellationToken.None);

        if (valeur == "99999") Assert.Empty(page.Articles);        // catégorie inexistante : aucun article, pas d'erreur
        else Assert.Equal(2, page.Articles.Count);                  // illisible ou vide : filtre ignoré
    }

    [Fact]
    public async Task La_colonne_categorie_affiche_le_nom_de_la_categorie_et_la_marque_et_le_modele()
    {
        var cat = await InsererCategorieAsync("Câblage");
        await Ajouter("A", categorieId: cat, marque: "Acme", modele: "X1");
        var page = CreerPage();

        await page.OnGetAsync(CancellationToken.None);

        var ligne = Assert.Single(page.Articles);
        Assert.Equal("Câblage", ligne.Category);      // avant : toujours vide
        Assert.Equal("Acme", ligne.Brand);
        Assert.Equal("X1", ligne.Model);
    }

    [Fact]
    public async Task Le_nombre_de_fournisseurs_est_le_nombre_reel_de_tarifs_de_l_article()
    {
        var f1 = new Fournisseur { Nom = "F1" }; var f2 = new Fournisseur { Nom = "F2" };
        Db.Fournisseurs.AddRange(f1, f2);
        await Db.SaveChangesAsync();
        var avecDeux = await Ajouter("DEUX");
        var principalSeul = await Ajouter("PRINCIPAL-SEUL");
        var aucun = await Ajouter("AUCUN");
        Db.ArticleFournisseurs.Add(new ArticleFournisseur { ArticleId = avecDeux.ID, FournisseurId = f1.Id });
        Db.ArticleFournisseurs.Add(new ArticleFournisseur { ArticleId = avecDeux.ID, FournisseurId = f2.Id });
        var p = await Db.Articles.FindAsync(principalSeul.ID);
        p!.FournisseurId = f1.Id;                                   // fournisseur principal SANS tarif
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        var page = CreerPage();
        page.SortBy = "reference";

        await page.OnGetAsync(CancellationToken.None);

        var parRef = page.Articles.ToDictionary(a => a.Reference, a => a.SupplierCount);
        Assert.Equal(2, parRef["DEUX"]);
        Assert.Equal(0, parRef["PRINCIPAL-SEUL"]);                  // avant : 1 (comptait le fournisseur principal)
        Assert.Equal(0, parRef["AUCUN"]);
    }

    // ------------------------------------------------------------------ rupture, bas, élevé : trois ensembles distincts

    [Fact]
    public async Task Les_filtres_de_stock_rupture_bas_et_eleve_donnent_trois_ensembles_distincts()
    {
        await Ajouter("RUPTURE", stock: 0, stockMin: 5);
        await Ajouter("BAS", stock: 3, stockMin: 5);
        await Ajouter("BAS-LIMITE", stock: 5, stockMin: 5);          // égal au minimum : bas
        await Ajouter("ELEVE", stock: 20, stockMin: 5);
        await Ajouter("RUPTURE-SANS-MIN", stock: 0, stockMin: 0);    // 0 avec minimum 0 : rupture
        var out_ = CreerPage(); out_.SelectedStockFilter = "out"; out_.SortBy = "reference";
        var low = CreerPage(); low.SelectedStockFilter = "low"; low.SortBy = "reference";
        var high = CreerPage(); high.SelectedStockFilter = "high"; high.SortBy = "reference";

        await out_.OnGetAsync(CancellationToken.None);
        await low.OnGetAsync(CancellationToken.None);
        await high.OnGetAsync(CancellationToken.None);

        Assert.Equal(new[] { "RUPTURE", "RUPTURE-SANS-MIN" }, Refs(out_));   // avant : « rupture » = « bas » (stock <= minimum)
        Assert.Equal(new[] { "BAS", "BAS-LIMITE" }, Refs(low));
        Assert.Equal(new[] { "ELEVE" }, Refs(high));
    }

    [Fact]
    public async Task Le_statut_de_stock_affiche_est_coherent_avec_les_filtres()
    {
        await Ajouter("R", stock: 0, stockMin: 5);
        await Ajouter("B", stock: 3, stockMin: 5);
        await Ajouter("E", stock: 20, stockMin: 5);
        var page = CreerPage(); page.SortBy = "reference";

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal("out", page.Articles.Single(a => a.Reference == "R").StockStatus);
        Assert.Equal("low", page.Articles.Single(a => a.Reference == "B").StockStatus);
        Assert.Equal("good", page.Articles.Single(a => a.Reference == "E").StockStatus);
    }

    [Fact]
    public async Task Un_filtre_de_stock_inconnu_est_ignore()
    {
        await Ajouter("A"); await Ajouter("B");
        var page = CreerPage(); page.SelectedStockFilter = "n'importe quoi";

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(2, page.Articles.Count);
    }

    // ------------------------------------------------------------------ statut, recherche

    [Theory]
    [InlineData("active", "ACTIF")]
    [InlineData("inactive", "INACTIF")]
    public async Task Le_filtre_de_statut_separe_actifs_et_inactifs(string filtre, string attendu)
    {
        await Ajouter("ACTIF", actif: true);
        await Ajouter("INACTIF", actif: false);
        var page = CreerPage(); page.SelectedStatus = filtre;

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(new[] { attendu }, Refs(page));
    }

    [Fact]
    public async Task La_recherche_porte_sur_la_reference_le_libelle_et_le_code_barres()
    {
        await Ajouter("REF-ALPHA", libelle: "Autre");
        await Ajouter("B", libelle: "Câble alpha");
        await Ajouter("C", libelle: "Autre", codeBarre: "123alpha");
        await Ajouter("D", libelle: "Rien");
        var page = CreerPage(); page.SearchTerm = "alpha"; page.SortBy = "reference";

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(new[] { "B", "C", "REF-ALPHA" }, Refs(page));
    }

    [Fact]
    public async Task Les_filtres_se_combinent()
    {
        var cat = await InsererCategorieAsync("Câblage");
        await Ajouter("OK", categorieId: cat, stock: 0, actif: true);
        await Ajouter("MAUVAISE-CAT", stock: 0, actif: true);
        await Ajouter("INACTIF", categorieId: cat, stock: 0, actif: false);
        await Ajouter("PAS-RUPTURE", categorieId: cat, stock: 9, actif: true);
        var page = CreerPage();
        page.SelectedCategory = cat.ToString(); page.SelectedStatus = "active"; page.SelectedStockFilter = "out";

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(new[] { "OK" }, Refs(page));
    }

    // ------------------------------------------------------------------ tris et pagination

    [Theory]
    [InlineData("name", new[] { "R2", "R3", "R1" })]            // libellés : Alpha, Bravo, Charlie
    [InlineData("name_desc", new[] { "R1", "R3", "R2" })]
    [InlineData("reference", new[] { "R1", "R2", "R3" })]
    [InlineData("newest", new[] { "R3", "R2", "R1" })]
    [InlineData("oldest", new[] { "R1", "R2", "R3" })]
    [InlineData("inconnu", new[] { "R2", "R3", "R1" })]         // tri inconnu : par nom
    public async Task Chaque_tri_propose_par_la_vue_est_pris_en_charge(string tri, string[] attendu)
    {
        await Ajouter("R1", libelle: "Charlie", creation: new DateTime(2026, 1, 1));
        await Ajouter("R2", libelle: "Alpha", creation: new DateTime(2026, 2, 1));
        await Ajouter("R3", libelle: "Bravo", creation: new DateTime(2026, 3, 1));
        var page = CreerPage(); page.SortBy = tri;

        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(attendu, Refs(page));
    }

    [Fact]
    public async Task La_pagination_est_stable_meme_quand_les_libelles_sont_identiques()
    {
        for (var i = 1; i <= 7; i++) await Ajouter("P" + i, libelle: "Identique");
        // PageSize non autorisé -> 25 ; page demandée trop grande -> dernière page
        var page = CreerPage(); page.PageSize = 7; page.Page = 99;
        await page.OnGetAsync(CancellationToken.None);

        Assert.Equal(25, page.PageSize);
        Assert.Equal(1, page.TotalPages);
        Assert.Equal(1, page.CurrentPage);
        Assert.Equal(7, page.TotalRecords);
        Assert.Equal(7, page.Articles.Select(a => a.Reference).Distinct().Count());
    }

    [Fact]
    public async Task Deux_pages_successives_ne_se_chevauchent_pas_avec_des_libelles_identiques()
    {
        for (var i = 1; i <= 12; i++) await Ajouter("Q" + i.ToString("00"), libelle: "Identique");
        var a = CreerPage(); a.PageSize = 10; a.Page = 1;
        var b = CreerPage(); b.PageSize = 10; b.Page = 2;

        await a.OnGetAsync(CancellationToken.None);
        await b.OnGetAsync(CancellationToken.None);

        Assert.Equal(10, a.Articles.Count);
        Assert.Equal(2, b.Articles.Count);
        Assert.Empty(Refs(a).Intersect(Refs(b)));
        Assert.Equal(2, a.TotalPages);
    }

    // ------------------------------------------------------------------ contrat vue / modèle (noms des paramètres)

    private static string LireVue()
    {
        var chemin = Path.Combine(AppContext.BaseDirectory, "Pages", "Articles", "Index.cshtml");
        var texte = File.ReadAllText(chemin);
        return Regex.Replace(texte, @"@\*.*?\*@", string.Empty, RegexOptions.Singleline);   // sans les commentaires Razor
    }

    [Theory]
    [InlineData("SelectedCategory", "category")]
    [InlineData("SelectedStatus", "status")]
    [InlineData("SelectedStockFilter", "stockFilter")]
    [InlineData("SearchTerm", "searchTerm")]
    [InlineData("SortBy", "sortBy")]
    [InlineData("PageSize", "pageSize")]
    public void Le_nom_de_chaque_parametre_envoye_par_la_vue_est_celui_que_la_page_lie(string propriete, string nomFormulaire)
    {
        var vue = LireVue();
        var attribut = typeof(IndexArticleModel).GetProperty(propriete)!.GetCustomAttribute<BindPropertyAttribute>()!;
        var nomLie = attribut.Name ?? propriete;

        Assert.Contains($"name=\"{nomFormulaire}\"", vue);                       // la vue envoie bien ce nom
        Assert.Equal(nomFormulaire, nomLie, ignoreCase: true);                   // la page lie ce nom (la casse est sans importance pour MVC)
    }

    [Fact]
    public void La_pagination_conserve_le_filtre_de_stock_et_le_bouton_reinitialiser_en_tient_compte()
    {
        var vue = LireVue();

        var liensAvecCategorie = Regex.Matches(vue, @"asp-route-category=""@Model\.SelectedCategory""").Count;
        var liensAvecStock = Regex.Matches(vue, @"asp-route-stockFilter=""@Model\.SelectedStockFilter""").Count;
        Assert.True(liensAvecCategorie >= 5, "La vue doit contenir les liens de pagination.");
        Assert.Equal(liensAvecCategorie, liensAvecStock);      // chaque lien qui conserve la catégorie conserve aussi le filtre de stock (avant : 0)
        Assert.Contains("!string.IsNullOrEmpty(Model.SelectedStockFilter)", vue);       // le bouton « Réinitialiser » apparaît aussi pour ce filtre
    }
}
