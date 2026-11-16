using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Pages.Clients;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GestionCommerciale.Tests;

public class ClientSuppressionTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly ClientService _service;

    public ClientSuppressionTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
        _service = new ClientService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    /// <summary>Service dont le contrôle des documents est faussé : simule un document créé juste après le contrôle.</summary>
    private class ClientServiceControleAveugle : ClientService
    {
        public ClientServiceControleAveugle(ApplicationDbContext db) : base(db) { }

        public override Task<bool> ADesDocumentsAsync(int clientId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
    }

    public static IEnumerable<object[]> TypesDeDocument()
    {
        yield return new object[] { "commande" };
        yield return new object[] { "devis" };
        yield return new object[] { "bon de livraison" };
        yield return new object[] { "facture" };
    }

    private async Task<Client> CreerClientAsync(string nom = "A")
    {
        var client = new Client { Nom = nom };
        Assert.True((await _service.CreerAsync(client)).Succes);
        return client;
    }

    private async Task AjouterDocumentAsync(string type, int clientId)
    {
        switch (type)
        {
            case "commande": _db.CommandesClient.Add(new CommandeClient { NumeroCommande = "CC-TEST-1", ClientId = clientId }); break;
            case "devis": _db.Devis.Add(new Devis { Numero = "DEV-TEST-1", ClientId = clientId }); break;
            case "bon de livraison": _db.BonsLivraison.Add(new BonLivraison { Numero = "BL-TEST-1", ClientId = clientId }); break;
            case "facture": _db.Factures.Add(new Facture { ClientId = clientId }); break;
            default: throw new ArgumentException(type);
        }
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private Task<bool> ExisteAsync(int id) => _db.Clients.AsNoTracking().AnyAsync(c => c.Id == id);

    // ---------- Service ----------

    [Fact]
    public async Task Un_client_sans_aucun_lien_est_supprime()
    {
        var client = await CreerClientAsync();

        var resultat = await _service.SupprimerAsync(client.Id);

        Assert.True(resultat.Succes);
        Assert.Equal(client.Code, resultat.Code);
        Assert.False(await ExisteAsync(client.Id));
    }

    [Theory]
    [MemberData(nameof(TypesDeDocument))]
    public async Task Un_client_avec_un_document_est_refuse_avec_un_message_clair(string type)
    {
        var client = await CreerClientAsync();
        await AjouterDocumentAsync(type, client.Id);

        var resultat = await _service.SupprimerAsync(client.Id);

        Assert.False(resultat.Succes);
        Assert.False(resultat.Introuvable);
        Assert.Contains(resultat.Erreurs, e => e.Message == ClientService.MessageSuppressionRefusee(client.Code));
        Assert.True(await ExisteAsync(client.Id)); // rien n'est supprimé
    }

    [Fact]
    public async Task Un_client_inexistant_renvoie_introuvable()
    {
        var resultat = await _service.SupprimerAsync(12345);

        Assert.False(resultat.Succes);
        Assert.True(resultat.Introuvable);
    }

    [Fact]
    public async Task Le_controle_est_refait_au_moment_de_la_suppression()
    {
        var client = await CreerClientAsync();

        // 1. La page de confirmation s'affiche : le client n'a aucun document.
        Assert.False(await _service.ADesDocumentsAsync(client.Id));

        // 2. Entre l'affichage et le clic sur « Supprimer », un document est créé.
        await AjouterDocumentAsync("commande", client.Id);

        // 3. La suppression est refusée malgré ce que la page avait montré.
        var resultat = await _service.SupprimerAsync(client.Id);

        Assert.False(resultat.Succes);
        Assert.True(await ExisteAsync(client.Id));
    }

    [Theory]
    [MemberData(nameof(TypesDeDocument))]
    public async Task Si_le_controle_est_contourne_la_base_refuse_quand_meme(string type)
    {
        var client = await CreerClientAsync();
        await AjouterDocumentAsync(type, client.Id);
        var service = new ClientServiceControleAveugle(_db); // le contrôle croit qu'il n'y a aucun document

        var resultat = await service.SupprimerAsync(client.Id);

        Assert.False(resultat.Succes);
        Assert.Contains(resultat.Erreurs, e => e.Message == ClientService.MessageSuppressionRefusee(client.Code));
        Assert.True(await ExisteAsync(client.Id));
    }

    [Fact]
    public async Task Les_documents_d_un_autre_client_ne_bloquent_pas_la_suppression()
    {
        var autre = await CreerClientAsync("Autre");
        var client = await CreerClientAsync("A");
        await AjouterDocumentAsync("facture", autre.Id);

        var resultat = await _service.SupprimerAsync(client.Id);

        Assert.True(resultat.Succes);
        Assert.False(await ExisteAsync(client.Id));
        Assert.True(await ExisteAsync(autre.Id));
    }

    // ---------- Page de confirmation ----------

    private class FauxFournisseurTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private DeleteModel CreerPage(int id)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        return new DeleteModel(_service, NullLogger<DeleteModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            TempData = new TempDataDictionary(httpContext, new FauxFournisseurTempData()),
            Id = id
        };
    }

    [Fact]
    public async Task Page_suppression_reussie_retourne_a_la_liste_avec_le_message()
    {
        var client = await CreerClientAsync();
        var page = CreerPage(client.Id);

        var resultat = await page.OnPostAsync(CancellationToken.None);

        var redirection = Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal("./Index", redirection.PageName);
        Assert.Equal($"Client {client.Code} supprimé.", page.TempData["SuccessMessage"]);
        Assert.False(await ExisteAsync(client.Id));
    }

    [Fact]
    public async Task Page_suppression_refusee_reste_sur_la_page_avec_le_message_et_ne_supprime_rien()
    {
        var client = await CreerClientAsync();
        await AjouterDocumentAsync("commande", client.Id);
        var page = CreerPage(client.Id);

        var resultat = await page.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultat);
        Assert.True(page.SuppressionImpossible);
        var erreur = Assert.Single(page.ModelState[string.Empty]!.Errors);
        Assert.Equal(ClientService.MessageSuppressionRefusee(client.Code), erreur.ErrorMessage);
        Assert.False(page.TempData.ContainsKey("SuccessMessage"));
        Assert.True(await ExisteAsync(client.Id));
    }

    [Fact]
    public async Task Page_client_inexistant_retourne_a_la_liste_avec_introuvable()
    {
        var page = CreerPage(12345);

        var post = await page.OnPostAsync(CancellationToken.None);
        var redirection = Assert.IsType<RedirectToPageResult>(post);
        Assert.Equal("./Index", redirection.PageName);
        Assert.Equal("Client introuvable.", page.TempData["ErrorMessage"]);

        var pageGet = CreerPage(12345);
        var get = await pageGet.OnGetAsync(CancellationToken.None);
        Assert.IsType<RedirectToPageResult>(get);
        Assert.Equal("Client introuvable.", pageGet.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task Page_affichage_d_un_client_sans_document_propose_la_suppression()
    {
        var client = await CreerClientAsync();
        var page = CreerPage(client.Id);

        var resultat = await page.OnGetAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultat);
        Assert.False(page.SuppressionImpossible);
        Assert.Equal(client.Code, page.Fiche.Client.Code);
    }
}
