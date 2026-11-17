using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

public class ClientFicheTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly ClientService _service;

    public ClientFicheTests()
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

    private async Task<Client> CreerClientAsync(string nom)
    {
        var client = new Client { Nom = nom };
        Assert.True((await _service.CreerAsync(client)).Succes);
        return client;
    }

    private async Task AjouterCommandeAsync(int clientId, string numero, DateTime date, decimal ttc,
        StatutCommande statut = StatutCommande.EnAttente)
    {
        _db.CommandesClient.Add(new CommandeClient
        {
            NumeroCommande = numero,
            ClientId = clientId,
            DateCommande = date,
            MontantTTC = ttc,
            Statut = statut
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Fiche_d_un_client_sans_commande()
    {
        var client = await CreerClientAsync("A");

        var fiche = await _service.ObtenirFicheAsync(client.Id);

        Assert.NotNull(fiche);
        Assert.Equal("A", fiche.Client.Nom);
        Assert.Equal(client.Code, fiche.Client.Code);
        Assert.Equal(0, fiche.NombreCommandes);
        Assert.Empty(fiche.DernieresCommandes);
        Assert.False(fiche.ModeFacturationVerrouille);
    }

    [Fact]
    public async Task Fiche_d_un_client_inexistant_renvoie_null()
        => Assert.Null(await _service.ObtenirFicheAsync(12345));

    [Fact]
    public async Task La_fiche_donne_le_total_et_les_cinq_dernieres_commandes_les_plus_recentes_d_abord()
    {
        var client = await CreerClientAsync("A");
        var debut = new DateTime(2026, 1, 1);
        for (int i = 1; i <= 7; i++)
            await AjouterCommandeAsync(client.Id, $"CC-{i:D2}", debut.AddDays(i), i * 100m);

        var fiche = await _service.ObtenirFicheAsync(client.Id);

        Assert.NotNull(fiche);
        Assert.Equal(7, fiche.NombreCommandes);
        Assert.Equal(5, fiche.DernieresCommandes.Count);
        Assert.Equal(new[] { "CC-07", "CC-06", "CC-05", "CC-04", "CC-03" },
                     fiche.DernieresCommandes.Select(c => c.Numero));
        Assert.Equal(700m, fiche.DernieresCommandes[0].MontantTTC);
    }

    [Fact]
    public async Task La_fiche_ne_compte_que_les_commandes_de_ce_client()
    {
        var client = await CreerClientAsync("A");
        var autre = await CreerClientAsync("Autre");
        await AjouterCommandeAsync(client.Id, "CC-A", DateTime.Today, 100m, StatutCommande.Livree);
        await AjouterCommandeAsync(autre.Id, "CC-B", DateTime.Today, 999m);

        var fiche = await _service.ObtenirFicheAsync(client.Id);

        Assert.NotNull(fiche);
        Assert.Equal(1, fiche.NombreCommandes);
        var commande = Assert.Single(fiche.DernieresCommandes);
        Assert.Equal("CC-A", commande.Numero);
        Assert.Equal(StatutCommande.Livree, commande.Statut);
    }

    [Fact]
    public async Task La_fiche_indique_le_verrouillage_du_mode_de_facturation()
    {
        var client = await CreerClientAsync("A");
        await AjouterCommandeAsync(client.Id, "CC-1", DateTime.Today, 10m);

        var fiche = await _service.ObtenirFicheAsync(client.Id);

        Assert.NotNull(fiche);
        Assert.True(fiche.ModeFacturationVerrouille);
    }
}
