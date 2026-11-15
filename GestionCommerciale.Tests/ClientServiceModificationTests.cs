using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

public class ClientServiceModificationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly ClientService _service;

    public ClientServiceModificationTests()
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

    private async Task<Client> CreerAsync(string nom, string? email = null, string? matricule = null)
    {
        var client = new Client { Nom = nom, Email = email, MatriculeFiscale = matricule };
        var resultat = await _service.CreerAsync(client);
        Assert.True(resultat.Succes);
        return client;
    }

    /// <summary>Relit le client depuis la base, hors suivi (ce que verrait l'utilisateur).</summary>
    private Task<Client> RelireAsync(int id) => _db.Clients.AsNoTracking().SingleAsync(c => c.Id == id);

    [Fact]
    public async Task Modifier_enregistre_les_champs_et_la_date_de_modification()
    {
        var client = await CreerAsync("Ancien nom", "ancien@test.com");

        var saisie = new Client
        {
            Nom = "  Nouveau nom ",
            Email = "nouveau@test.com",
            Ville = "Sfax",
            CodePostal = "3000",
            PlafondCredit = 5000,
            DelaiPaiement = 30,
            RemiseDefaut = 7.5m,
            EstActif = false,
            ModeFacturation = Enums.ModeFacturation.Passager
        };

        var resultat = await _service.ModifierAsync(client.Id, saisie);

        Assert.True(resultat.Succes);
        var enBase = await RelireAsync(client.Id);
        Assert.Equal("Nouveau nom", enBase.Nom);
        Assert.Equal("nouveau@test.com", enBase.Email);
        Assert.Equal("Sfax", enBase.Ville);
        Assert.Equal("3000", enBase.CodePostal);
        Assert.Equal(5000, enBase.PlafondCredit);
        Assert.Equal(30, enBase.DelaiPaiement);
        Assert.Equal(7.5m, enBase.RemiseDefaut);
        Assert.False(enBase.EstActif);
        Assert.Equal(Enums.ModeFacturation.Passager, enBase.ModeFacturation);
        Assert.NotNull(enBase.DateModification);
    }

    [Fact]
    public async Task Modifier_ne_change_jamais_le_code_ni_la_date_de_creation_ni_l_id()
    {
        var client = await CreerAsync("A");
        var codeInitial = client.Code;
        var creationInitiale = (await RelireAsync(client.Id)).DateCreation;

        var saisie = new Client
        {
            Id = 999,                              // tentative de changer l'Id
            Code = "CLI-99999",                    // tentative de changer le code
            DateCreation = new DateTime(2000, 1, 1),
            Nom = "A modifié"
        };

        Assert.True((await _service.ModifierAsync(client.Id, saisie)).Succes);

        var enBase = await RelireAsync(client.Id);
        Assert.Equal(codeInitial, enBase.Code);
        Assert.Equal(creationInitiale, enBase.DateCreation);
        Assert.Equal(1, await _db.Clients.CountAsync());
    }

    [Fact]
    public async Task Modifier_accepte_de_garder_son_propre_email_et_matricule()
    {
        var client = await CreerAsync("A", "a@test.com", "1234567A");

        var saisie = new Client { Nom = "A bis", Email = "A@TEST.com", MatriculeFiscale = " 1234567a " };

        Assert.True((await _service.ModifierAsync(client.Id, saisie)).Succes);
    }

    [Fact]
    public async Task Modifier_refuse_l_email_d_un_autre_client_et_ne_change_rien()
    {
        await CreerAsync("Autre", "autre@test.com");
        var client = await CreerAsync("A", "a@test.com");

        var resultat = await _service.ModifierAsync(client.Id, new Client { Nom = "A modifié", Email = " AUTRE@test.com " });

        Assert.False(resultat.Succes);
        Assert.Contains(resultat.Erreurs, e => e.Champ == nameof(Client.Email));
        var enBase = await RelireAsync(client.Id);
        Assert.Equal("A", enBase.Nom);
        Assert.Equal("a@test.com", enBase.Email);
        Assert.Null(enBase.DateModification);
    }

    [Fact]
    public async Task Modifier_refuse_le_matricule_d_un_autre_client()
    {
        await CreerAsync("Autre", matricule: "1234567A/P/M/000");
        var client = await CreerAsync("A");

        var resultat = await _service.ModifierAsync(client.Id, new Client { Nom = "A", MatriculeFiscale = "1234567a/p/m/000" });

        Assert.False(resultat.Succes);
        Assert.Contains(resultat.Erreurs, e => e.Champ == nameof(Client.MatriculeFiscale));
    }

    [Fact]
    public async Task Modifier_refuse_les_donnees_invalides_sans_rien_ecrire()
    {
        var client = await CreerAsync("A");

        var saisie = new Client
        {
            Nom = "A modifié",
            CodePostal = "75001",
            MatriculeFiscale = "12-34",
            RemiseDefaut = 150,
            PlafondCredit = -1,
            DelaiPaiement = -1
        };

        var resultat = await _service.ModifierAsync(client.Id, saisie);

        Assert.False(resultat.Succes);
        Assert.Equal(5, resultat.Erreurs.Count);
        var enBase = await RelireAsync(client.Id);
        Assert.Equal("A", enBase.Nom);
        Assert.Null(enBase.DateModification);
    }

    [Fact]
    public async Task Modifier_refuse_un_nom_vide()
    {
        var client = await CreerAsync("A");

        var resultat = await _service.ModifierAsync(client.Id, new Client { Nom = "   " });

        Assert.False(resultat.Succes);
        Assert.Contains(resultat.Erreurs, e => e.Champ == nameof(Client.Nom));
    }

    [Fact]
    public async Task Modifier_un_client_inexistant_renvoie_introuvable()
    {
        var resultat = await _service.ModifierAsync(12345, new Client { Nom = "A" });

        Assert.False(resultat.Succes);
        Assert.True(resultat.Introuvable);
    }

    [Fact]
    public async Task Une_societe_n_a_pas_de_prenom()
    {
        var client = new Client { Nom = "Particulier", Prenom = "Jean", TypeClient = TypeClient.Particulier };
        Assert.True((await _service.CreerAsync(client)).Succes);

        var saisie = new Client { Nom = "Ma Société", Prenom = "Jean", TypeClient = TypeClient.Societe };
        Assert.True((await _service.ModifierAsync(client.Id, saisie)).Succes);

        var enBase = await RelireAsync(client.Id);
        Assert.Equal(TypeClient.Societe, enBase.TypeClient);
        Assert.Null(enBase.Prenom);
    }


    // ---------- Verrouillage du mode de facturation ----------

    public static IEnumerable<object[]> TypesDeDocument()
    {
        yield return new object[] { "commande" };
        yield return new object[] { "devis" };
        yield return new object[] { "bon de livraison" };
        yield return new object[] { "facture" };
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

    [Fact]
    public async Task Sans_document_le_mode_de_facturation_peut_changer()
    {
        var client = await CreerAsync("A");
        Assert.False(await _service.ADesDocumentsAsync(client.Id));

        var resultat = await _service.ModifierAsync(client.Id,
            new Client { Nom = "A", ModeFacturation = Enums.ModeFacturation.Passager });

        Assert.True(resultat.Succes);
        Assert.Equal(Enums.ModeFacturation.Passager, (await RelireAsync(client.Id)).ModeFacturation);
    }

    [Theory]
    [MemberData(nameof(TypesDeDocument))]
    public async Task Avec_un_document_le_changement_de_mode_de_facturation_est_refuse(string type)
    {
        var client = await CreerAsync("A"); // mode par défaut : EnCompte
        await AjouterDocumentAsync(type, client.Id);
        Assert.True(await _service.ADesDocumentsAsync(client.Id));

        var resultat = await _service.ModifierAsync(client.Id,
            new Client { Nom = "A modifié", ModeFacturation = Enums.ModeFacturation.Passager });

        Assert.False(resultat.Succes);
        Assert.Contains(resultat.Erreurs, e =>
            e.Champ == nameof(Client.ModeFacturation) && e.Message == ClientService.MessageModeFacturationVerrouille);

        var enBase = await RelireAsync(client.Id);
        Assert.Equal(Enums.ModeFacturation.EnCompte, enBase.ModeFacturation); // inchangé
        Assert.Equal("A", enBase.Nom);                                         // rien d'enregistré
    }

    [Theory]
    [MemberData(nameof(TypesDeDocument))]
    public async Task Avec_un_document_les_autres_champs_restent_modifiables_si_le_mode_ne_change_pas(string type)
    {
        var client = await CreerAsync("A");
        await AjouterDocumentAsync(type, client.Id);

        var resultat = await _service.ModifierAsync(client.Id,
            new Client { Nom = "A modifié", Ville = "Tunis", ModeFacturation = Enums.ModeFacturation.EnCompte });

        Assert.True(resultat.Succes);
        var enBase = await RelireAsync(client.Id);
        Assert.Equal("A modifié", enBase.Nom);
        Assert.Equal("Tunis", enBase.Ville);
    }

    [Fact]
    public async Task Les_documents_d_un_autre_client_ne_verrouillent_pas_celui_ci()
    {
        var autre = await CreerAsync("Autre");
        var client = await CreerAsync("A");
        await AjouterDocumentAsync("facture", autre.Id);

        Assert.False(await _service.ADesDocumentsAsync(client.Id));
        Assert.True((await _service.ModifierAsync(client.Id,
            new Client { Nom = "A", ModeFacturation = Enums.ModeFacturation.Passager })).Succes);
    }
    [Fact]
    public async Task Obtenir_renvoie_le_client_ou_null()
    {
        var client = await CreerAsync("A");

        Assert.Equal("A", (await _service.ObtenirAsync(client.Id))!.Nom);
        Assert.Null(await _service.ObtenirAsync(12345));
    }
}
