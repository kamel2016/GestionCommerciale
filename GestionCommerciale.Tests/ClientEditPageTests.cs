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

/// <summary>Teste le comportement de la page Edit : redirection, message de succès, erreurs sur le formulaire.</summary>
public class ClientEditPageTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly ClientService _service;

    public ClientEditPageTests()
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

    private class FauxFournisseurTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private EditModel CreerPage(int id, Client saisie)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        return new EditModel(_service, NullLogger<EditModel>.Instance)
        {
            PageContext = new PageContext(actionContext),
            TempData = new TempDataDictionary(httpContext, new FauxFournisseurTempData()),
            Id = id,
            Client = saisie
        };
    }

    private async Task<Client> CreerClientAsync(string nom, string? email = null)
    {
        var client = new Client { Nom = nom, Email = email };
        Assert.True((await _service.CreerAsync(client)).Succes);
        return client;
    }

    [Fact]
    public async Task Apres_une_modification_reussie_on_retourne_a_la_liste_avec_le_message_de_succes()
    {
        var client = await CreerClientAsync("Ancien");
        var page = CreerPage(client.Id, new Client { Nom = "Nouveau", Ville = "Tunis" });

        var resultat = await page.OnPostAsync(CancellationToken.None);

        var redirection = Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal("./Index", redirection.PageName);
        // Même clé TempData que la création : la liste l'affiche en vert.
        Assert.Equal($"Client {client.Code} modifié avec succès.", page.TempData["SuccessMessage"]);
        Assert.Equal("Nouveau", (await _service.ObtenirAsync(client.Id))!.Nom);
    }

    [Fact]
    public async Task Un_email_en_doublon_reste_sur_le_formulaire_sans_message_de_succes()
    {
        await CreerClientAsync("Autre", "autre@test.com");
        var client = await CreerClientAsync("A", "a@test.com");
        var page = CreerPage(client.Id, new Client { Nom = "A", Email = "AUTRE@test.com" });

        var resultat = await page.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultat);
        Assert.False(page.ModelState.IsValid);
        Assert.True(page.ModelState.ContainsKey("Client.Email"));
        Assert.False(page.TempData.ContainsKey("SuccessMessage"));
        Assert.Equal("a@test.com", (await _service.ObtenirAsync(client.Id))!.Email); // inchangé
    }

    [Fact]
    public async Task Une_donnee_invalide_reste_sur_le_formulaire_sans_message_de_succes()
    {
        var client = await CreerClientAsync("A");
        var page = CreerPage(client.Id, new Client { Nom = "A", CodePostal = "75001" });

        var resultat = await page.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(resultat);
        Assert.True(page.ModelState.ContainsKey("Client.CodePostal"));
        Assert.False(page.TempData.ContainsKey("SuccessMessage"));
    }

    [Fact]
    public async Task Un_client_inexistant_renvoie_a_la_liste_avec_un_message_d_erreur()
    {
        var page = CreerPage(12345, new Client { Nom = "A" });

        var resultat = await page.OnPostAsync(CancellationToken.None);

        var redirection = Assert.IsType<RedirectToPageResult>(resultat);
        Assert.Equal("./Index", redirection.PageName);
        Assert.Equal("Client introuvable.", page.TempData["ErrorMessage"]);
        Assert.False(page.TempData.ContainsKey("SuccessMessage"));
    }
}
