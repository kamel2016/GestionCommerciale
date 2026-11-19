using System.Text.Json;
using GestionCommerciale.Enums;
using GestionCommerciale.Helpers;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using CreerCommande = GestionCommerciale.Pages.Commande_Client.CreateModel;
using ModifierCommande = GestionCommerciale.Pages.Commande_Client.EditModel;

namespace GestionCommerciale.Tests;

/// <summary>
/// S7bis : Create et Edit de la commande client stockent les montants calculés par CalculTotaux (via RecalculerTotaux), copient
/// PrixVenteHT et TauxTVA sur chaque ligne (figés), n'utilisent jamais PrixAchatHT. Les valeurs attendues sont calculées à la main
/// (voir CalculTotauxTests). SQLite ne prouve pas le comportement de SQL Server.
/// </summary>
public class CommandeMontantsTests : ArticleTestBase
{
    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private static (PageContext Contexte, TempDataDictionary Temp) Contexte()
    {
        var http = new DefaultHttpContext();
        var action = new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return (new PageContext(action), new TempDataDictionary(http, new FauxTempData()));
    }

    private static string Json(params (int ArticleId, int Quantite)[] lignes)
        => JsonSerializer.Serialize(lignes.Select(l => new { articleId = l.ArticleId, quantite = l.Quantite, prixUnitaire = 999m }));   // prix posté ignoré

    private async Task<int> CreerClientAsync()
    {
        var client = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Client montants" };
        Db.Clients.Add(client);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return client.Id;
    }

    /// <summary>Article avec prix de vente, TVA et un prix d'achat volontairement très différent (777,777) pour détecter tout usage.</summary>
    private async Task<int> CreerArticleAsync(string prixVente, decimal tva)
    {
        var article = new Article
        {
            Reference = "MT-" + Guid.NewGuid().ToString("N")[..8], Libelle = "Article montants", Quantite = 1000,
            PrixVenteHT = decimal.Parse(prixVente, System.Globalization.CultureInfo.InvariantCulture), TauxTVA = tva, PrixAchatHT = 777.777m
        };
        Db.Articles.Add(article);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return article.ID;
    }

    private async Task ModifierPrixArticleAsync(int articleId, decimal nouveauPrix, decimal nouvelleTva)
    {
        var article = await Db.Articles.SingleAsync(a => a.ID == articleId);
        article.PrixVenteHT = nouveauPrix;
        article.TauxTVA = nouvelleTva;
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
    }

    private async Task<int> CreerCommandeParLaPageAsync(int clientId, params (int ArticleId, int Quantite)[] lignes)
    {
        var (contexte, temp) = Contexte();
        var page = new CreerCommande(Db, NullLogger<CreerCommande>.Instance)
        {
            PageContext = contexte, TempData = temp, LignesJson = Json(lignes),
            CommandeClient = new CommandeClient { ClientId = clientId, DateCommande = DateTime.Today }
        };
        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());
        Db.ChangeTracker.Clear();
        return (await Db.CommandesClient.OrderByDescending(c => c.Id).FirstAsync()).Id;
    }

    private async Task<IActionResult> ModifierParLaPageAsync(int commandeId, int clientId, params (int ArticleId, int Quantite)[] lignes)
    {
        var (contexte, temp) = Contexte();
        var page = new ModifierCommande(Db, NullLogger<ModifierCommande>.Instance)
        {
            PageContext = contexte, TempData = temp, LignesJson = Json(lignes),
            Input = new ModifierCommande.CommandeInput { Id = commandeId, NumeroCommande = "X", ClientId = clientId, DateCommande = DateTime.Today, Statut = StatutCommande.EnAttente }
        };
        var resultat = await page.OnPostAsync();
        Db.ChangeTracker.Clear();
        return resultat;
    }

    private async Task<CommandeClient> RelireAsync(int commandeId)
    {
        Db.ChangeTracker.Clear();
        return await Db.CommandesClient.AsNoTracking().Include(c => c.LignesCommande).SingleAsync(c => c.Id == commandeId);
    }

    // ------------------------------------------------------------------ création

    [Fact]
    public async Task Create_stocke_prix_taux_et_montant_de_chaque_ligne_et_les_totaux_calcules_a_la_main()
    {
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("0.335", 20m);     // 3 x 0,335 = 1,005 à 20 %
        var b = await CreerArticleAsync("10.500", 19m);    // 2 x 10,500 = 21,000 à 19 %
        var id = await CreerCommandeParLaPageAsync(client, (a, 3), (b, 2));

        var commande = await RelireAsync(id);

        var ligneA = commande.LignesCommande.Single(l => l.ArticleId == a);
        var ligneB = commande.LignesCommande.Single(l => l.ArticleId == b);
        Assert.Equal((0.335m, 20m, 1.005m), (ligneA.PrixUnitaire, ligneA.TauxTVA, ligneA.MontantLigne));    // avant : TauxTVA 0 et MontantLigne 0
        Assert.Equal((10.500m, 19m, 21.000m), (ligneB.PrixUnitaire, ligneB.TauxTVA, ligneB.MontantLigne));
        Assert.Equal(22.005m, commande.MontantHTBrut);
        Assert.Equal(22.005m, commande.MontantHT);
        Assert.Equal(4.191m, commande.MontantTVA);       // 19 % : 3,990 + 20 % : 0,201
        Assert.Equal(26.196m, commande.MontantTTC);
    }

    [Fact]
    public async Task Ce_que_Create_enregistre_est_exactement_ce_que_CalculTotaux_retourne_pour_les_lignes_relues()
    {
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("0.335", 20m);
        var b = await CreerArticleAsync("10.500", 19m);
        var c = await CreerArticleAsync("2.345", 7m);
        var id = await CreerCommandeParLaPageAsync(client, (a, 3), (b, 2), (c, 7));

        var commande = await RelireAsync(id);

        var attendu = CalculTotaux.CalculerTotaux(commande.LignesCommande.Select(l => (l.MontantLigne, l.TauxTVA)), commande.TauxRemiseGlobale);
        Assert.Equal(attendu.TotalHTNet, commande.MontantHT);
        Assert.Equal(attendu.TotalTVA, commande.MontantTVA);
        Assert.Equal(attendu.TotalTTC, commande.MontantTTC);
        Assert.All(commande.LignesCommande, l => { Assert.True(l.TauxTVA > 0); Assert.True(l.MontantLigne > 0); });   // aucune valeur à 0
        Assert.Equal(16.415m, commande.LignesCommande.Single(l => l.ArticleId == c).MontantLigne);                  // 7 x 2,345 à la main
    }

    [Fact]
    public async Task Le_prix_poste_par_le_navigateur_est_ignore_seul_le_prix_de_vente_de_l_article_compte()
    {
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("10.500", 19m);
        var id = await CreerCommandeParLaPageAsync(client, (a, 1));    // le JSON poste un prix de 999

        Assert.Equal(10.500m, (await RelireAsync(id)).LignesCommande.Single().PrixUnitaire);
    }

    [Fact]
    public async Task Une_commande_creee_garde_son_prix_et_sa_tva_quand_l_article_change_ensuite()
    {
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("10.500", 19m);
        var id = await CreerCommandeParLaPageAsync(client, (a, 2));

        await ModifierPrixArticleAsync(a, nouveauPrix: 20.000m, nouvelleTva: 7m);

        var commande = await RelireAsync(id);
        var ligne = commande.LignesCommande.Single();
        Assert.Equal((10.500m, 19m, 21.000m), (ligne.PrixUnitaire, ligne.TauxTVA, ligne.MontantLigne));   // figés
        Assert.Equal((21.000m, 3.990m, 24.990m), (commande.MontantHT, commande.MontantTVA, commande.MontantTTC));
    }

    // ------------------------------------------------------------------ modification

    [Fact]
    public async Task Edit_calcule_avec_le_prix_de_vente_jamais_avec_le_prix_d_achat_et_remplit_taux_et_montant()
    {
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("0.335", 20m);
        var b = await CreerArticleAsync("10.500", 19m);
        var id = await CreerCommandeParLaPageAsync(client, (a, 1));

        Assert.IsType<RedirectToPageResult>(await ModifierParLaPageAsync(id, client, (a, 3), (b, 2)));   // PrixAchatHT = 777,777 partout

        var commande = await RelireAsync(id);
        Assert.Equal(22.005m, commande.MontantHT);       // avec PrixAchatHT on aurait 5 000 et plus
        Assert.Equal(4.191m, commande.MontantTVA);
        Assert.Equal(26.196m, commande.MontantTTC);
        Assert.All(commande.LignesCommande, l => { Assert.True(l.TauxTVA > 0); Assert.True(l.MontantLigne > 0); });
    }

    [Fact]
    public async Task Edit_conserve_le_prix_et_la_tva_figes_des_lignes_existantes_et_utilise_le_prix_actuel_pour_un_nouvel_article()
    {
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("0.335", 20m);
        var b = await CreerArticleAsync("10.500", 19m);
        var c = await CreerArticleAsync("2.000", 7m);
        var id = await CreerCommandeParLaPageAsync(client, (a, 3), (b, 2));
        await ModifierPrixArticleAsync(b, nouveauPrix: 20.000m, nouvelleTva: 7m);          // l'article B change APRÈS la commande

        // B passe de 2 à 4 (prix et TVA de la commande conservés), C est un nouvel article (prix actuel)
        Assert.IsType<RedirectToPageResult>(await ModifierParLaPageAsync(id, client, (a, 3), (b, 4), (c, 5)));

        var commande = await RelireAsync(id);
        var ligneB = commande.LignesCommande.Single(l => l.ArticleId == b);
        var ligneC = commande.LignesCommande.Single(l => l.ArticleId == c);
        Assert.Equal((10.500m, 19m, 42.000m), (ligneB.PrixUnitaire, ligneB.TauxTVA, ligneB.MontantLigne));   // 4 x 10,500 à la main
        Assert.Equal((2.000m, 7m, 10.000m), (ligneC.PrixUnitaire, ligneC.TauxTVA, ligneC.MontantLigne));
        // HT 1,005 + 42,000 + 10,000 = 53,005 ; TVA 20 % : 0,201 ; 19 % : 7,980 ; 7 % : 0,700 ; TVA 8,881 ; TTC 61,886
        Assert.Equal(53.005m, commande.MontantHT);
        Assert.Equal(8.881m, commande.MontantTVA);
        Assert.Equal(61.886m, commande.MontantTTC);
    }

    [Fact]
    public async Task Edit_sans_changement_de_lignes_ne_reprice_pas_la_commande()
    {
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("10.500", 19m);
        var id = await CreerCommandeParLaPageAsync(client, (a, 2));
        await ModifierPrixArticleAsync(a, nouveauPrix: 99.000m, nouvelleTva: 7m);

        Assert.IsType<RedirectToPageResult>(await ModifierParLaPageAsync(id, client, (a, 2)));

        var commande = await RelireAsync(id);
        Assert.Equal(10.500m, commande.LignesCommande.Single().PrixUnitaire);
        Assert.Equal(24.990m, commande.MontantTTC);
    }

    // ------------------------------------------------------------------ affichage (JavaScript) aligné sur la règle de stockage

    private static string Lire(params string[] segments)
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "GestionCommerciale.sln"))) dir = Path.GetDirectoryName(dir);
        return File.ReadAllText(Path.Combine(new[] { dir!, "GestionCommerciale" }.Concat(segments).ToArray()));
    }

    [Fact]
    public void Les_scripts_d_affichage_calculent_en_millimes_entiers_comme_la_regle_de_stockage_et_affichent_3_decimales()
    {
        var create = Lire("wwwroot", "js", "commande-create.js");
        var edit = Lire("Pages", "Commande_Client", "Edit.cshtml");

        foreach (var script in new[] { create, edit })
        {
            Assert.Contains("calculerTotauxMillimes", script);                       // la même fonction dans les deux écrans
            Assert.DoesNotContain("montant * (a.tva / 100)", script);                // ancienne formule en virgule flottante
            Assert.DoesNotContain("* ((l.TauxTVA ?? 0) / 100)", script);
            Assert.Contains("minimumFractionDigits: 3", script);                     // affichage au millime (le stockage est au millime)
        }
    }

    [Fact]
    public void Create_et_Edit_n_utilisent_plus_PrixAchatHT_pour_les_montants_de_commande()
    {
        Assert.DoesNotContain(".PrixAchatHT", Lire("Pages", "Commande_Client", "Create.cshtml.cs"));
        Assert.DoesNotContain(".PrixAchatHT", Lire("Pages", "Commande_Client", "Edit.cshtml.cs"));
        Assert.DoesNotContain("PrixAchatHT", Lire("Pages", "Commande_Client", "Edit.cshtml"));
    }
}
