using System.Text.RegularExpressions;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DetailsBl = GestionCommerciale.Pages.BonsLivraison.DetailsModel;
using IndexBl = GestionCommerciale.Pages.BonsLivraison.IndexModel;

namespace GestionCommerciale.Tests;

/// <summary>
/// Bandeaux de messages des pages BL (BonsLivraison/Index et Details). Convention : les pages BL posent et lisent leurs clés propres,
/// TempData["SuccessMessage"] (bandeau vert, alert-success) et TempData["ErrorMessage"] (bandeau rouge, alert-danger) ; une redirection vers
/// un AUTRE module utilise la clé de ce module (ex. « Error » pour la liste des commandes). Le message de conflit de concurrence reste
/// la constante DetailsModel.MessageConflitStock (non réécrit). Les gestionnaires posaient déjà ces messages ; ce qui manquait : les vues
/// ne les affichaient pas. Les tests de vue lisent le texte des fichiers (pas de rendu Razor ici ; le rendu réel est vérifié par une requête).
/// </summary>
public class BonLivraisonBandeauxTests : ArticleTestBase
{
    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private BonLivraisonService Service() => new(Db, new StockService(Db));

    private DetailsBl PageDetails()
    {
        var http = new DefaultHttpContext();
        var action = new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return new DetailsBl(Db, Service()) { PageContext = new PageContext(action), TempData = new TempDataDictionary(http, new FauxTempData()) };
    }

    /// <summary>BL brouillon d'une ligne de 4 sur un article en stock de 10.</summary>
    private async Task<(int BlId, int ArticleId)> BlAsync(StatutBonLivraison statut = StatutBonLivraison.Brouillon)
    {
        var article = await InsererAsync("BAN-" + Guid.NewGuid().ToString("N")[..6], stock: 10);
        var bl = new BonLivraison { Numero = "BL-BAN-" + Guid.NewGuid().ToString("N")[..5], Statut = statut };
        bl.Lignes.Add(new LigneBonLivraison { ArticleId = article.ID, Quantite = 4, PrixUnitaire = 2.000m });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return (bl.ID, article.ID);
    }

    private static void AssertRedirigeVersLaMemePage(IActionResult resultat, int id)
    {
        var redirection = Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Null(redirection.PageName);                                         // même page (Details), donc même module
        Assert.Equal(id, redirection.RouteValues!["id"]);
    }

    private static void AssertSeulementCesCles(DetailsBl page, string cle)
    {
        Assert.True(page.TempData.ContainsKey(cle));
        Assert.False(page.TempData.ContainsKey(cle == "SuccessMessage" ? "ErrorMessage" : "SuccessMessage"));   // jamais les deux bandeaux à la fois
        Assert.False(page.TempData.ContainsKey("Success"));                        // clés d'un AUTRE module : jamais posées par une redirection interne
        Assert.False(page.TempData.ContainsKey("Error"));
    }

    // ------------------------------------------------------------------ Details : gestionnaire Valider

    [Fact]
    public async Task Valider_reussi_pose_le_message_de_succes_dans_la_cle_SuccessMessage_et_redirige_vers_la_meme_page()
    {
        var (bl, _) = await BlAsync();
        var page = PageDetails();

        var resultat = await page.OnPostValiderAsync(bl);

        AssertRedirigeVersLaMemePage(resultat, bl);
        Assert.Equal("Bon de livraison validé, stock mis à jour.", page.TempData["SuccessMessage"]);
        AssertSeulementCesCles(page, "SuccessMessage");
        Assert.Equal(StatutBonLivraison.Valide, (await Db.BonsLivraison.AsNoTracking().SingleAsync(b => b.ID == bl)).Statut);
    }

    [Fact]
    public async Task Valider_refuse_pose_exactement_le_message_du_service_dans_ErrorMessage()
    {
        var (bl, _) = await BlAsync(StatutBonLivraison.Valide);                     // déjà validé : le service refuse
        var attendu = (await Service().ValiderAsync(bl)).Erreur;
        var page = PageDetails();

        var resultat = await page.OnPostValiderAsync(bl);

        AssertRedirigeVersLaMemePage(resultat, bl);
        Assert.Equal("Seul un brouillon peut être validé.", attendu);
        Assert.Equal(attendu, page.TempData["ErrorMessage"]);                       // le message vient du service, mot pour mot
        AssertSeulementCesCles(page, "ErrorMessage");
    }

    [Fact]
    public async Task Valider_en_cas_de_conflit_de_concurrence_utilise_la_constante_existante_sans_exception()
    {
        var (bl, _) = await BlAsync();
        var page = PageDetails();
        Interception.Action = () => throw new DbUpdateConcurrencyException("conflit simulé");

        var resultat = await page.OnPostValiderAsync(bl);

        AssertRedirigeVersLaMemePage(resultat, bl);
        Assert.Equal(DetailsBl.MessageConflitStock, page.TempData["ErrorMessage"]);
        AssertSeulementCesCles(page, "ErrorMessage");
        Assert.Equal(StatutBonLivraison.Brouillon, (await Db.BonsLivraison.AsNoTracking().SingleAsync(b => b.ID == bl)).Statut);
    }

    // ------------------------------------------------------------------ Details : gestionnaire Annuler

    [Fact]
    public async Task Annuler_reussi_pose_le_message_de_succes_dans_SuccessMessage()
    {
        var (bl, _) = await BlAsync();
        var page = PageDetails();

        var resultat = await page.OnPostAnnulerAsync(bl);

        AssertRedirigeVersLaMemePage(resultat, bl);
        Assert.Equal("Bon de livraison annulé.", page.TempData["SuccessMessage"]);
        AssertSeulementCesCles(page, "SuccessMessage");
    }

    [Fact]
    public async Task Annuler_refuse_car_une_facture_est_liee_pose_le_message_du_service_dans_ErrorMessage()
    {
        var (bl, _) = await BlAsync(StatutBonLivraison.Valide);
        Db.Factures.Add(new Facture { BonLivraisonId = bl, Statut = StatutFacture.Validee });
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        var page = PageDetails();

        var resultat = await page.OnPostAnnulerAsync(bl);

        AssertRedirigeVersLaMemePage(resultat, bl);
        Assert.Contains("lié à une facture non annulée", (string)page.TempData["ErrorMessage"]!);
        AssertSeulementCesCles(page, "ErrorMessage");
        Assert.Equal(StatutBonLivraison.Valide, (await Db.BonsLivraison.AsNoTracking().SingleAsync(b => b.ID == bl)).Statut);
    }

    [Fact]
    public async Task Annuler_un_bl_deja_annule_pose_le_message_du_service_dans_ErrorMessage()
    {
        var (bl, _) = await BlAsync(StatutBonLivraison.Annule);
        var page = PageDetails();

        await page.OnPostAnnulerAsync(bl);

        Assert.Equal("Ce BL est déjà annulé.", page.TempData["ErrorMessage"]);
        AssertSeulementCesCles(page, "ErrorMessage");
    }

    [Fact]
    public async Task Annuler_en_cas_de_conflit_de_concurrence_utilise_la_constante_existante()
    {
        var (bl, _) = await BlAsync();
        await Service().ValiderAsync(bl);
        Db.ChangeTracker.Clear();
        var page = PageDetails();
        Interception.Action = () => throw new DbUpdateConcurrencyException("conflit simulé");

        var resultat = await page.OnPostAnnulerAsync(bl);

        AssertRedirigeVersLaMemePage(resultat, bl);
        Assert.Equal(DetailsBl.MessageConflitStock, page.TempData["ErrorMessage"]);
        AssertSeulementCesCles(page, "ErrorMessage");
    }

    // ------------------------------------------------------------------ Index : ne touche pas aux messages

    [Fact]
    public async Task L_index_ne_pose_ni_ne_consomme_aucun_message_il_les_laisse_a_la_vue()
    {
        var http = new DefaultHttpContext();
        var action = new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var page = new IndexBl(Db) { PageContext = new PageContext(action), TempData = new TempDataDictionary(http, new FauxTempData()) };
        page.TempData["ErrorMessage"] = "message venu d'ailleurs";

        await page.OnGetAsync();

        Assert.Equal("message venu d'ailleurs", page.TempData.Peek("ErrorMessage"));   // laissé intact pour être affiché par la vue
        Assert.False(page.TempData.ContainsKey("SuccessMessage"));
    }

    // ------------------------------------------------------------------ vues : les bandeaux sont affichés (contrat de texte)

    private static string Lire(params string[] segments)
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "GestionCommerciale.sln"))) dir = Path.GetDirectoryName(dir);
        return File.ReadAllText(Path.Combine(new[] { dir!, "GestionCommerciale", "Pages", "BonsLivraison" }.Concat(segments).ToArray()));
    }

    private static string SansCommentairesRazor(string t) => Regex.Replace(t, @"@\*.*?\*@", string.Empty, RegexOptions.Singleline);

    [Fact]
    public void Le_partiel_de_messages_affiche_le_succes_en_vert_et_l_erreur_en_rouge_avec_les_cles_des_pages_BL()
    {
        var partiel = SansCommentairesRazor(Lire("_MessagesBonLivraison.cshtml"));

        Assert.Contains("TempData[\"SuccessMessage\"]", partiel);
        Assert.Contains("alert alert-success", partiel);
        Assert.Contains("TempData[\"ErrorMessage\"]", partiel);
        Assert.Contains("alert alert-danger", partiel);
        Assert.DoesNotContain("TempData[\"Error\"]", partiel);                       // les clés des autres modules ne sont pas lues ici
        Assert.DoesNotContain("TempData[\"Success\"]", partiel);
    }

    [Theory]
    [InlineData("Details.cshtml")]
    [InlineData("Index.cshtml")]
    public void Les_pages_BL_Details_et_Index_incluent_le_partiel_de_messages(string vue)
    {
        var texte = SansCommentairesRazor(Lire(vue));

        Assert.Contains("<partial name=\"_MessagesBonLivraison\" />", texte);
    }

    [Fact]
    public void Les_gestionnaires_de_Details_ne_posent_que_les_cles_du_module_BL()
    {
        var page = Lire("Details.cshtml.cs");

        Assert.Contains("TempData[\"SuccessMessage\"]", page);
        Assert.Contains("TempData[\"ErrorMessage\"]", page);
        Assert.DoesNotContain("TempData[\"Error\"]", page);                          // aucune redirection vers un autre module ici
        Assert.DoesNotContain("TempData[\"Success\"]", page);
        Assert.Equal(1, Regex.Matches(page, "Le stock d'un article a ").Count);              // le message de conflit n'existe qu'une fois (la constante)
    }

    // ------------------------------------------------------------------ rendu RÉEL du partiel (moteur Razor, vue compilée de l'application)

    private static async Task<string> RendrePartielAsync(string? succes, string? erreur)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var diagnostic = new System.Diagnostics.DiagnosticListener("Microsoft.AspNetCore");
        services.AddSingleton<System.Diagnostics.DiagnosticListener>(diagnostic);
        services.AddSingleton<System.Diagnostics.DiagnosticSource>(diagnostic);
        services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(new FauxEnvironnement());
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(sp => sp.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>());
        services.AddMvc().AddApplicationPart(typeof(DetailsBl).Assembly);
        var fournisseur = services.BuildServiceProvider();

        var moteur = (Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine)fournisseur.GetService(typeof(Microsoft.AspNetCore.Mvc.Razor.IRazorViewEngine))!;
        var resultatVue = moteur.GetView(null, "/Pages/BonsLivraison/_MessagesBonLivraison.cshtml", isMainPage: false);
        Assert.True(resultatVue.Success, "Vue introuvable : " + string.Join(", ", resultatVue.SearchedLocations ?? Array.Empty<string>()));

        var http = new DefaultHttpContext { RequestServices = fournisseur };
        var action = new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var temp = new TempDataDictionary(http, new FauxTempData());
        if (succes != null) temp["SuccessMessage"] = succes;
        if (erreur != null) temp["ErrorMessage"] = erreur;
        using var ecriture = new StringWriter();
        var contexte = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext(action, resultatVue.View,
            new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary()),
            temp, ecriture, new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions());
        await resultatVue.View.RenderAsync(contexte);
        return ecriture.ToString();
    }

    private sealed class FauxEnvironnement : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "GestionCommerciale";
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Fact]
    public async Task Le_rendu_reel_du_partiel_donne_un_bandeau_vert_pour_le_succes_et_rouge_pour_l_erreur_avec_le_texte_exact()
    {
        var html = await RendrePartielAsync("Bon de livraison validé, stock mis à jour.", DetailsBl.MessageConflitStock);

        Assert.Contains("<div class=\"alert alert-success\" role=\"alert\">Bon de livraison valid", html);
        Assert.Contains("stock mis &#xE0; jour.</div>", html);                                // « à » encodé en HTML : texte exact
        Assert.Contains("<div class=\"alert alert-danger\" role=\"alert\">Le stock d&#x27;un article a &#xE9;t&#xE9; modifi", html);
        Assert.True(html.IndexOf("alert-success") < html.IndexOf("alert-danger"));            // succès avant erreur
    }

    [Fact]
    public async Task Le_rendu_reel_du_partiel_n_affiche_rien_sans_message_et_un_seul_bandeau_avec_un_seul_message()
    {
        Assert.DoesNotContain("alert", (await RendrePartielAsync(null, null)));

        var seulSucces = await RendrePartielAsync("Bon de livraison annulé.", null);
        Assert.Contains("alert alert-success", seulSucces);
        Assert.DoesNotContain("alert-danger", seulSucces);

        var seulErreur = await RendrePartielAsync(null, "Ce BL est déjà annulé.");
        Assert.Contains("alert alert-danger", seulErreur);
        Assert.DoesNotContain("alert-success", seulErreur);
    }

    [Fact]
    public async Task Le_rendu_reel_echappe_le_html_d_un_message_pour_eviter_toute_injection()
    {
        var html = await RendrePartielAsync(null, "<script>alert(1)</script>");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }
}
