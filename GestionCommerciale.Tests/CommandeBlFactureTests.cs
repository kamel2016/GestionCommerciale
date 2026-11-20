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
        var bl = new BonLivraison { Numero = "BL-PF-" + Guid.NewGuid().ToString("N")[..5], ClientId = commande.ClientId, CommandeClientId = commande.Id, Statut = StatutBonLivraison.Valide };
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

    // ------------------------------------------------------------------ S7bis : divergences prouvées, CORRIGÉES en S10

    [Fact]
    public async Task S10_corrige_la_facture_garde_le_prix_et_la_tva_figes_de_la_commande_meme_si_l_article_change()
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
        // facture : prix et TVA figés de la commande (avant S10, prix de l'article du jour : HT 25,500 ; TVA 4,665 ; TTC 31,165)
        Assert.Equal(22.005m, facture.TotalHt);
        Assert.Equal(4.191m, facture.TotalTva);
        Assert.Equal(27.196m, facture.TotalTtc);                            // commande 26,196 + timbre 1,000
        Assert.Equal(bl.TotalHT, facture.TotalHt);
    }

    [Fact]
    public async Task S10_corrige_la_tva_est_arrondie_de_la_meme_facon_en_commande_et_en_facture()
    {
        await ParametresNeutresAsync();
        var client = await CreerClientAsync();
        var a = await CreerArticleAsync("0.150", 7m);                       // 0,150 x 7 % = 0,0105 : demi-millime
        var commandeId = await CommandeParLaPageAsync(client, (a, 1));
        var commande = await Db.CommandesClient.AsNoTracking().SingleAsync(c => c.Id == commandeId);
        Assert.Equal(0.011m, commande.MontantTVA);                          // AwayFromZero
        Assert.Equal(0.161m, commande.MontantTTC);

        var facture = await FactureDepuisLeBlAsync(await BlDepuisLaCommandeAsync(commandeId));

        Assert.Equal(0.011m, facture.TotalTva);                             // AwayFromZero comme la commande (avant S10 : ToEven, 0,010)
        Assert.Equal(0.161m + 1.000m, facture.TotalTtc);                    // 1,161
    }
}

