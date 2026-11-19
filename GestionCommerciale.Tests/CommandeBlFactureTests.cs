using System.Text.Json;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using CreerCommande = GestionCommerciale.Pages.Commande_Client.CreateModel;
using Xunit.Abstractions;

namespace GestionCommerciale.Tests;

/// <summary>
/// S7bis étape 3 : parcours commande -> BL -> facture avec des chiffres fixes (calculés à la main). Établit où les montants restent
/// identiques et PROUVE les divergences connues de FactureService (prix actuel de l'article, arrondi ToEven), consignées « bloquant
/// avant pilote » (S10). Les tests « DIVERGENCE CONNUE » décrivent le comportement ACTUEL : ils devront être réécrits quand
/// la facturation sera corrigée. SQLite ne prouve pas le comportement de SQL Server.
/// </summary>
public class CommandeBlFactureTests : ArticleTestBase
{
    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private async Task<int> CreerClientAsync()
    {
        var client = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Client parcours" };
        Db.Clients.Add(client);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return client.Id;
    }

    private async Task<int> CreerArticleAsync(string prixVente, decimal tva)
    {
        var article = new Article
        {
            Reference = "PF-" + Guid.NewGuid().ToString("N")[..8], Libelle = "Article parcours", Quantite = 1000,
            PrixVenteHT = decimal.Parse(prixVente, System.Globalization.CultureInfo.InvariantCulture), TauxTVA = tva
        };
        Db.Articles.Add(article);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return article.ID;
    }

    private async Task ModifierPrixArticleAsync(int articleId, decimal prix, decimal tva)
    {
        var article = await Db.Articles.SingleAsync(a => a.ID == articleId);
        article.PrixVenteHT = prix;
        article.TauxTVA = tva;
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
    }

    /// <summary>Paramètres société neutres : pas de FODEC, timbre 1,000, retenue à la source inactive.</summary>
    private async Task ParametresNeutresAsync()
    {
        var p = await Db.ParametresSociete.FirstOrDefaultAsync();
        if (p == null) { p = new ParametresSociete { RaisonSociale = "Société test" }; Db.ParametresSociete.Add(p); }
        p.TauxFodec = 0m;
        p.TimbreFiscal = 1.000m;
        p.RetenueActive = false;
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
    }

    private async Task<int> CommandeParLaPageAsync(int clientId, params (int ArticleId, int Quantite)[] lignes)
    {
        var http = new DefaultHttpContext();
        var action = new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        var page = new CreerCommande(Db, NullLogger<CreerCommande>.Instance)
        {
            PageContext = new PageContext(action), TempData = new TempDataDictionary(http, new FauxTempData()),
            LignesJson = JsonSerializer.Serialize(lignes.Select(l => new { articleId = l.ArticleId, quantite = l.Quantite, prixUnitaire = 0m })),
            CommandeClient = new CommandeClient { ClientId = clientId, DateCommande = DateTime.Today }
        };
        Assert.IsType<RedirectToPageResult>(await page.OnPostAsync());
        Db.ChangeTracker.Clear();
        return (await Db.CommandesClient.OrderByDescending(c => c.Id).FirstAsync()).Id;
    }

    /// <summary>Crée le BL comme la page BonsLivraison/Create le propose : une ligne par ligne de commande, PrixUnitaire de la ligne de commande.</summary>
    private async Task<int> BlDepuisLaCommandeAsync(int commandeId)
    {
        var commande = await Db.CommandesClient.AsNoTracking().Include(c => c.LignesCommande).SingleAsync(c => c.Id == commandeId);
        var bl = new BonLivraison { Numero = "BL-PF-" + Guid.NewGuid().ToString("N")[..5], ClientId = commande.ClientId, CommandeClientId = commande.Id };
        foreach (var l in commande.LignesCommande)
            bl.Lignes.Add(new LigneBonLivraison { ArticleId = l.ArticleId, LigneCommandeClientId = l.Id, Quantite = l.Quantite, PrixUnitaire = l.PrixUnitaire });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return bl.ID;
    }

    private async Task<Facture> FactureDepuisLeBlAsync(int blId)
    {
        var facture = await new FactureService(Db, null!).CreerDepuisBonLivraisonAsync(blId);   // la séquence ne sert qu'à la validation
        Db.ChangeTracker.Clear();
        return await Db.Factures.AsNoTracking().Include(f => f.Lignes).SingleAsync(f => f.Id == facture.Id);
    }

    // ------------------------------------------------------------------ prix inchangé : mêmes montants de bout en bout

    [Fact]
    public async Task Parcours_commande_bl_facture_prix_inchanges_donne_les_memes_montants()
    {
        await ParametresNeutresAsync();
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("0.335", 20m);
        var b = await CreerArticleAsync("10.500", 19m);

        var commandeId = await CommandeParLaPageAsync(client, (a, 3), (b, 2));
        var commande = await Db.CommandesClient.AsNoTracking().SingleAsync(c => c.Id == commandeId);
        // à la main : lignes 1,005 (20 %) et 21,000 (19 %) ; HT 22,005 ; TVA 0,201 + 3,990 = 4,191 ; TTC 26,196
        Assert.Equal((22.005m, 4.191m, 26.196m), (commande.MontantHT, commande.MontantTVA, commande.MontantTTC));

        var blId = await BlDepuisLaCommandeAsync(commandeId);
        var bl = await Db.BonsLivraison.AsNoTracking().Include(x => x.Lignes).SingleAsync(x => x.ID == blId);
        Assert.Equal(22.005m, bl.TotalHT);                                  // le BL reprend le prix figé de la commande

        var facture = await FactureDepuisLeBlAsync(blId);
        Assert.Equal(new[] { 1.005m, 21.000m }, facture.Lignes.OrderBy(l => l.Id).Select(l => l.TotalHt).ToArray());
        Assert.Equal(22.005m, facture.TotalHt);
        Assert.Equal(4.191m, facture.TotalTva);
        Assert.Equal(26.196m + 1.000m, facture.TotalTtc);                   // commande + timbre fiscal 1,000 (FODEC 0)
    }

    // ------------------------------------------------------------------ DIVERGENCES CONNUES (comportement actuel, bloquant avant pilote)

    [Fact]
    public async Task DIVERGENCE_CONNUE_la_facture_reprend_le_prix_et_la_tva_ACTUELS_de_l_article_pas_ceux_de_la_commande_ni_du_bl()
    {
        await ParametresNeutresAsync();
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("0.335", 20m);
        var b = await CreerArticleAsync("10.500", 19m);
        var commandeId = await CommandeParLaPageAsync(client, (a, 3), (b, 2));
        var blId = await BlDepuisLaCommandeAsync(commandeId);

        await ModifierPrixArticleAsync(a, prix: 0.500m, tva: 7m);           // les articles changent APRÈS la commande et le BL
        await ModifierPrixArticleAsync(b, prix: 12.000m, tva: 19m);
        var facture = await FactureDepuisLeBlAsync(blId);

        var bl = await Db.BonsLivraison.AsNoTracking().Include(x => x.Lignes).SingleAsync(x => x.ID == blId);
        Assert.Equal(22.005m, bl.TotalHT);                                  // commande et BL : prix figés
        // facture, à la main : 3 x 0,500 = 1,500 (7 %) ; 2 x 12,000 = 24,000 (19 %) ; HT 25,500 ; TVA 0,105 + 4,560 = 4,665 ; TTC 25,500 + 4,665 + timbre 1,000 = 31,165
        Assert.Equal(25.500m, facture.TotalHt);                             // ≠ 22,005 : divergence
        Assert.Equal(4.665m, facture.TotalTva);
        Assert.Equal(31.165m, facture.TotalTtc);
        Assert.NotEqual(bl.TotalHT, facture.TotalHt);
    }

    [Fact]
    public async Task DIVERGENCE_CONNUE_la_facture_arrondit_la_tva_en_ToEven_la_commande_en_AwayFromZero()
    {
        await ParametresNeutresAsync();
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("0.150", 7m);                       // 0,150 x 7 % = 0,0105 : demi-millime
        var commandeId = await CommandeParLaPageAsync(client, (a, 1));
        var commande = await Db.CommandesClient.AsNoTracking().SingleAsync(c => c.Id == commandeId);
        Assert.Equal(0.011m, commande.MontantTVA);                          // AwayFromZero
        Assert.Equal(0.161m, commande.MontantTTC);

        var facture = await FactureDepuisLeBlAsync(await BlDepuisLaCommandeAsync(commandeId));

        Assert.Equal(0.010m, facture.TotalTva);                             // ToEven (Math.Round par défaut) : 1 millime d'écart
        Assert.Equal(0.150m + 0.010m + 1.000m, facture.TotalTtc);           // 1,160 au lieu de 0,161 + 1,000 = 1,161
    }
}

/// <summary>
/// S7bis étape 3 : les colonnes de montants de Facture sont en decimal(18,2) alors que le code calcule et que le reste du modèle
/// stocke au millime (decimal(18,3)). Preuve sur un vrai SQL Server (base LocalDB jetable), seulement avec GC_TESTS_SQLSERVER=1.
/// </summary>
[Trait("Category", "SqlServer")]
public class SqlServerFactureDecimalTests : IClassFixture<BaseJetableFixture>
{
    private readonly BaseJetableFixture _base;
    private readonly ITestOutputHelper _sortie;

    public SqlServerFactureDecimalTests(BaseJetableFixture baseJetable, ITestOutputHelper sortie) { _base = baseJetable; _sortie = sortie; }

    [SqlServerFact]
    public async Task DIVERGENCE_CONNUE_la_facture_perd_le_millime_a_l_enregistrement_colonnes_decimal_18_2()
    {
        _base.AssurerBase();
        int blId;
        using (var db = _base.NouveauContexte())
        {
            var p = await db.ParametresSociete.FirstOrDefaultAsync();
            if (p == null) { p = new ParametresSociete { RaisonSociale = "Société test" }; db.ParametresSociete.Add(p); }
            p.TauxFodec = 0m; p.TimbreFiscal = 1.000m; p.RetenueActive = false;
            var article = new Article { Reference = "S7B-" + Guid.NewGuid().ToString("N")[..8], Libelle = "Decimal", Quantite = 10, PrixVenteHT = 0.335m, TauxTVA = 20m };
            db.Articles.Add(article);
            await db.SaveChangesAsync();
            var bl = new BonLivraison { Numero = "BL-S7B-" + Guid.NewGuid().ToString("N")[..5] };
            bl.Lignes.Add(new LigneBonLivraison { ArticleId = article.ID, Quantite = 3, PrixUnitaire = 0.335m });
            db.BonsLivraison.Add(bl);
            await db.SaveChangesAsync();
            blId = bl.ID;
        }

        int factureId;
        using (var db = _base.NouveauContexte())
        {
            var facture = await new FactureService(db, null!).CreerDepuisBonLivraisonAsync(blId);
            Assert.Equal(1.005m, facture.TotalHt);                           // calculé en mémoire au millime (3 x 0,335)
            factureId = facture.Id;
        }

        using (var db = _base.NouveauContexte())
        {
            var relue = await db.Factures.AsNoTracking().SingleAsync(f => f.Id == factureId);
            _sortie.WriteLine($"[S7bis] TotalHt calculé 1,005 -> relu sur SQL Server : {relue.TotalHt} ; TotalTva calculé 0,201 -> relu : {relue.TotalTva}");
            Assert.NotEqual(1.005m, relue.TotalHt);                          // le millime est perdu : la colonne n'a que 2 décimales
        }
    }
}
