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
using ModifierCommande = GestionCommerciale.Pages.Commande_Client.EditModel;
using ListeCommandes = GestionCommerciale.Pages.CommandeClient.IndexModel;

namespace GestionCommerciale.Tests;

/// <summary>
/// S7 : la commande client ne touche JAMAIS au stock (décision : seul le BL validé fait sortir le stock). Règle A : une ligne dont la
/// quantité dépasse le stock physique actuel est refusée, en lecture seule (aucune réservation). Tout tourne sous SQLite, qui ne
/// prouve pas le comportement de SQL Server.
/// </summary>
public class CommandeClientStockTests : ArticleTestBase
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
        => JsonSerializer.Serialize(lignes.Select(l => new { articleId = l.ArticleId, quantite = l.Quantite, prixUnitaire = 1m }));

    private async Task<int> CreerClientAsync()
    {
        var client = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Client test" };
        Db.Clients.Add(client);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return client.Id;
    }

    private async Task<int> CreerArticleAsync(int stock)
        => (await InsererAsync("CMD-" + Guid.NewGuid().ToString("N")[..8], stock: stock)).ID;

    private async Task<int> LireStockAsync(int articleId)
    {
        Db.ChangeTracker.Clear();
        return (await Db.Articles.AsNoTracking().SingleAsync(a => a.ID == articleId)).Quantite;
    }

    /// <summary>Insère directement une commande (une ligne par article demandé).</summary>
    private async Task<int> InsererCommandeAsync(int clientId, StatutCommande statut = StatutCommande.EnAttente, params (int ArticleId, int Quantite)[] lignes)
    {
        var commande = new CommandeClient
        {
            NumeroCommande = "CC-TEST-" + Guid.NewGuid().ToString("N")[..6], ClientId = clientId, Statut = statut,
            MontantHT = 100m, MontantTVA = 19m, MontantTTC = 119m
        };
        foreach (var (articleId, quantite) in lignes)
            commande.LignesCommande.Add(new LigneCommandeClient { ArticleId = articleId, Quantite = quantite, PrixUnitaire = 10m });
        Db.CommandesClient.Add(commande);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return commande.Id;
    }

    private CreerCommande PageCreation(int clientId, string lignesJson)
    {
        var (contexte, temp) = Contexte();
        return new CreerCommande(Db, NullLogger<CreerCommande>.Instance)
        {
            PageContext = contexte, TempData = temp, LignesJson = lignesJson,
            CommandeClient = new CommandeClient { ClientId = clientId, DateCommande = DateTime.Today }
        };
    }

    private ModifierCommande PageModification(int commandeId, int clientId, string lignesJson, StatutCommande statut = StatutCommande.EnAttente)
    {
        var (contexte, temp) = Contexte();
        return new ModifierCommande(Db, NullLogger<ModifierCommande>.Instance)
        {
            PageContext = contexte, TempData = temp, LignesJson = lignesJson,
            Input = new ModifierCommande.CommandeInput { Id = commandeId, NumeroCommande = "X", ClientId = clientId, DateCommande = DateTime.Today, Statut = statut }
        };
    }

    private static string Erreurs(PageModel page)
        => string.Join(" | ", page.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

    // ------------------------------------------------------------------ création

    [Fact]
    public async Task Creer_une_commande_ne_modifie_pas_le_stock()
    {
        var client = await CreerClientAsync();
        var article = await CreerArticleAsync(stock: 10);

        var resultat = await PageCreation(client, Json((article, 4))).OnPostAsync();

        Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal(10, await LireStockAsync(article));        // avant S7 : 6
        Assert.Equal(1, await Db.CommandesClient.CountAsync());
        Assert.Empty(await Db.MouvementsStock.ToListAsync());
    }

    [Fact]
    public async Task Creer_une_commande_dont_la_quantite_depasse_le_stock_physique_est_refuse_avec_un_message_explicite()
    {
        var client = await CreerClientAsync();
        var article = await CreerArticleAsync(stock: 10);
        var page = PageCreation(client, Json((article, 11)));

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        var message = Erreurs(page);
        Assert.Contains("stock physique actuel", message);
        Assert.Contains("ne réserve pas", message);               // aucune promesse de réservation
        Assert.Equal(10, await LireStockAsync(article));
        Assert.Equal(0, await Db.CommandesClient.CountAsync());
    }

    [Fact]
    public async Task Creer_une_commande_egale_au_stock_physique_est_accepte_et_laisse_le_stock_intact()
    {
        var client = await CreerClientAsync();
        var article = await CreerArticleAsync(stock: 10);

        var resultat = await PageCreation(client, Json((article, 10))).OnPostAsync();

        Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal(10, await LireStockAsync(article));
    }

    // ------------------------------------------------------------------ modification

    [Fact]
    public async Task Modifier_les_lignes_d_une_commande_ne_modifie_pas_le_stock()
    {
        var client = await CreerClientAsync();
        var article = await CreerArticleAsync(stock: 10);
        var commande = await InsererCommandeAsync(client, StatutCommande.EnAttente, (article, 3));

        var resultat = await PageModification(commande, client, Json((article, 7))).OnPostAsync();

        Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal(10, await LireStockAsync(article));          // avant S7 : 3 puis 7 (restitue 3, sort 7)
        Db.ChangeTracker.Clear();
        Assert.Equal(7, (await Db.LignesCommandeClient.SingleAsync(l => l.CommandeClientId == commande)).Quantite);
    }

    [Fact]
    public async Task Modifier_une_commande_avec_une_quantite_superieure_au_stock_physique_est_refuse()
    {
        var client = await CreerClientAsync();
        var article = await CreerArticleAsync(stock: 10);
        var commande = await InsererCommandeAsync(client, StatutCommande.EnAttente, (article, 3));
        var page = PageModification(commande, client, Json((article, 11)));

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        Assert.Contains("stock physique actuel", Erreurs(page));
        Assert.Equal(10, await LireStockAsync(article));
        Db.ChangeTracker.Clear();
        Assert.Equal(3, (await Db.LignesCommandeClient.SingleAsync(l => l.CommandeClientId == commande)).Quantite);   // rien n'a changé
    }

    [Fact]
    public async Task Passer_une_commande_au_statut_Annulee_ne_modifie_pas_le_stock()
    {
        var client = await CreerClientAsync();
        var article = await CreerArticleAsync(stock: 10);
        var commande = await InsererCommandeAsync(client, StatutCommande.EnAttente, (article, 3));

        var resultat = await PageModification(commande, client, Json((article, 3)), StatutCommande.Annulee).OnPostAsync();

        Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal(10, await LireStockAsync(article));
        Db.ChangeTracker.Clear();
        Assert.Equal(StatutCommande.Annulee, (await Db.CommandesClient.SingleAsync(c => c.Id == commande)).Statut);
        Assert.Empty(await Db.MouvementsStock.ToListAsync());
    }

    // ------------------------------------------------------------------ suppression

    [Fact]
    public async Task Supprimer_une_commande_ne_modifie_pas_le_stock()
    {
        var client = await CreerClientAsync();
        var article = await CreerArticleAsync(stock: 10);
        var commande = await InsererCommandeAsync(client, StatutCommande.EnAttente, (article, 3));
        var (contexte, temp) = Contexte();
        var page = new ListeCommandes(Db, NullLogger<ListeCommandes>.Instance) { PageContext = contexte, TempData = temp };

        await page.OnPostDeleteAsync(commande);

        Assert.Equal(10, await LireStockAsync(article));
        Assert.Equal(0, await Db.CommandesClient.CountAsync());
    }

    // ------------------------------------------------------------------ commande déjà liée à un BL

    private async Task<(int ArticleId, int CommandeId, int BlId)> CommandeAvecBlAsync(int stock = 10, int quantite = 4)
    {
        var client = await CreerClientAsync();
        var article = await CreerArticleAsync(stock);
        var commande = await InsererCommandeAsync(client, StatutCommande.Confirmee, (article, quantite));
        var ligneCommande = await Db.LignesCommandeClient.AsNoTracking().SingleAsync(l => l.CommandeClientId == commande);
        var bl = new BonLivraison { Numero = "BL-S7-" + Guid.NewGuid().ToString("N")[..5], ClientId = client, CommandeClientId = commande };
        bl.Lignes.Add(new LigneBonLivraison { ArticleId = article, Quantite = quantite, PrixUnitaire = 10m, LigneCommandeClientId = ligneCommande.Id });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return (article, commande, bl.ID);
    }

    [Fact]
    public async Task Modifier_les_lignes_d_une_commande_deja_liee_a_un_bl_est_refuse_et_conserve_le_lien()
    {
        var (article, commande, blId) = await CommandeAvecBlAsync(stock: 10, quantite: 4);
        var clientId = (await Db.CommandesClient.AsNoTracking().SingleAsync(c => c.Id == commande)).ClientId;
        var page = PageModification(commande, clientId, Json((article, 5)));

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        Assert.Contains("bon de livraison", Erreurs(page));
        Db.ChangeTracker.Clear();
        var ligne = await Db.LignesCommandeClient.SingleAsync(l => l.CommandeClientId == commande);
        Assert.Equal(4, ligne.Quantite);                                                    // ligne d'origine conservée
        var ligneBl = await Db.LignesBonLivraison.SingleAsync(l => l.BonLivraisonId == blId);
        Assert.Equal(ligne.Id, ligneBl.LigneCommandeClientId);                              // lien BL -> ligne de commande conservé
        Assert.Equal(10, await LireStockAsync(article));
    }

    [Fact]
    public async Task Modifier_seulement_l_en_tete_d_une_commande_liee_a_un_bl_est_autorise_et_ne_touche_ni_aux_lignes_ni_aux_montants()
    {
        var (article, commande, _) = await CommandeAvecBlAsync(stock: 10, quantite: 4);
        var clientId = (await Db.CommandesClient.AsNoTracking().SingleAsync(c => c.Id == commande)).ClientId;
        Db.ChangeTracker.Clear();
        var idLigneAvant = (await Db.LignesCommandeClient.AsNoTracking().SingleAsync(l => l.CommandeClientId == commande)).Id;
        var page = PageModification(commande, clientId, Json((article, 4)), StatutCommande.EnPreparation);
        page.Input.Commentaire = "Nouveau commentaire";

        var resultat = await page.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(resultat);
        Db.ChangeTracker.Clear();
        var c = await Db.CommandesClient.Include(x => x.LignesCommande).SingleAsync(x => x.Id == commande);
        Assert.Equal(StatutCommande.EnPreparation, c.Statut);
        Assert.Equal("Nouveau commentaire", c.Commentaire);
        Assert.Equal(idLigneAvant, Assert.Single(c.LignesCommande).Id);                      // même ligne, pas recréée
        Assert.Equal(119m, c.MontantTTC);                                                    // montants non recalculés
        Assert.Equal(10, await LireStockAsync(article));
    }

    // ------------------------------------------------------------------ parcours complet

    [Fact]
    public async Task Parcours_commande_puis_bl_valide_puis_annulation_donne_le_bon_stock_et_les_bons_mouvements()
    {
        var client = await CreerClientAsync();
        var article = await CreerArticleAsync(stock: 10);

        // 1. La commande est créée par la page : le stock ne bouge pas.
        await PageCreation(client, Json((article, 4))).OnPostAsync();
        Assert.Equal(10, await LireStockAsync(article));
        var ligneCommande = await Db.LignesCommandeClient.AsNoTracking().SingleAsync();
        var commandeId = ligneCommande.CommandeClientId;

        // 2. Un BL lié à cette commande est créé puis validé : une seule sortie, avec son mouvement.
        var bl = new BonLivraison { Numero = "BL-PARCOURS", ClientId = client, CommandeClientId = commandeId };
        bl.Lignes.Add(new LigneBonLivraison { ArticleId = article, Quantite = 4, PrixUnitaire = 1m, LigneCommandeClientId = ligneCommande.Id });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        var service = new BonLivraisonService(Db, new StockService(Db));
        var (valide, erreurValidation) = await service.ValiderAsync(bl.ID);
        Assert.True(valide, erreurValidation);
        Assert.Equal(6, await LireStockAsync(article));          // 10 - 4, et non 10 - 4 - 4
        Assert.Equal(4, (await Db.LignesCommandeClient.AsNoTracking().SingleAsync()).QuantiteLivree);

        // 3. L'annulation du BL rend le stock et conserve les deux mouvements.
        var (annule, erreurAnnulation) = await service.AnnulerAsync(bl.ID);
        Assert.True(annule, erreurAnnulation);
        Assert.Equal(10, await LireStockAsync(article));
        Db.ChangeTracker.Clear();
        var mouvements = await Db.MouvementsStock.AsNoTracking().Where(m => m.ArticleId == article).OrderBy(m => m.ID).ToListAsync();
        Assert.Equal(2, mouvements.Count);
        Assert.Equal(TypeMouvementStock.SortieLivraison, mouvements[0].Type);
        Assert.Equal(TypeMouvementStock.EntreeAnnulationLivraison, mouvements[1].Type);
        Assert.All(mouvements, m => Assert.Equal(4, m.Quantite));
        Assert.Equal(0, (await Db.LignesCommandeClient.AsNoTracking().SingleAsync()).QuantiteLivree);
    }

    // ------------------------------------------------------------------ textes de l'interface

    [Fact]
    public void L_interface_parle_de_stock_physique_actuel_et_plus_de_stock_disponible_dans_les_pages_de_commande()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "GestionCommerciale.sln"))) dir = Path.GetDirectoryName(dir);
        var racine = Path.Combine(dir!, "GestionCommerciale");
        var textes = new[]
        {
            Path.Combine(racine, "Pages", "Commande_Client", "Create.cshtml"),
            Path.Combine(racine, "Pages", "Commande_Client", "Edit.cshtml"),
            Path.Combine(racine, "wwwroot", "js", "commande-create.js")
        }.Select(File.ReadAllText).ToList();

        Assert.All(textes, t => Assert.DoesNotContain("Stock disponible", t));      // « disponible » laisserait croire à une réservation
        Assert.All(textes, t => Assert.DoesNotContain("(disponible :", t));
        Assert.Contains(textes, t => t.Contains("Stock physique actuel"));
    }
}
