using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

public class ClientServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;

    public ClientServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    /// <summary>Service dont les premiers codes proposés sont imposés (pour simuler un doublon).</summary>
    private class ClientServiceCodesImposes : ClientService
    {
        private readonly Queue<string> _codes;
        public int NombreAppels { get; private set; }

        public ClientServiceCodesImposes(ApplicationDbContext db, params string[] codes) : base(db)
            => _codes = new Queue<string>(codes);

        protected override async Task<string> CalculerProchainCodeAsync(int essai, CancellationToken cancellationToken)
        {
            NombreAppels++;
            return _codes.Count > 0 ? _codes.Dequeue() : await base.CalculerProchainCodeAsync(essai, cancellationToken);
        }
    }

    private static Client NouveauClient(string nom = "Client", string? email = null, string? matricule = null)
        => new() { Nom = nom, Email = email, MatriculeFiscale = matricule };

    // ---------- Code client ----------

    [Fact]
    public async Task Les_codes_sont_generes_dans_l_ordre()
    {
        var service = new ClientService(_db);

        var premier = NouveauClient("Premier");
        var second = NouveauClient("Second");
        Assert.True((await service.CreerAsync(premier)).Succes);
        Assert.True((await service.CreerAsync(second)).Succes);

        Assert.Equal("CLI-00001", premier.Code);
        Assert.Equal("CLI-00002", second.Code);
    }

    [Fact]
    public async Task Doublon_de_code_simule_le_deuxieme_essai_reussit()
    {
        _db.Clients.Add(new Client { Code = "CLI-00001", Nom = "Existant" });
        await _db.SaveChangesAsync();

        // 1er essai : "CLI-00001" (déjà pris) ; 2e essai : calcul réel -> CLI-00002
        var service = new ClientServiceCodesImposes(_db, "CLI-00001");
        var client = NouveauClient("Nouveau");

        var resultat = await service.CreerAsync(client);

        Assert.True(resultat.Succes);
        Assert.Equal("CLI-00002", client.Code);
        Assert.Equal(2, service.NombreAppels);
        Assert.Equal(2, await _db.Clients.CountAsync());
    }

    [Fact]
    public async Task Apres_trois_essais_en_doublon_la_creation_echoue_proprement()
    {
        _db.Clients.Add(new Client { Code = "CLI-00001", Nom = "Existant" });
        await _db.SaveChangesAsync();

        var service = new ClientServiceCodesImposes(_db, "CLI-00001", "CLI-00001", "CLI-00001");

        var resultat = await service.CreerAsync(NouveauClient("Nouveau"));

        Assert.False(resultat.Succes);
        Assert.Equal(ClientService.NombreMaxEssais, service.NombreAppels);
        Assert.Equal(1, await _db.Clients.CountAsync()); // rien de plus enregistré
    }

    [Fact]
    public async Task Une_autre_erreur_de_base_n_est_pas_reessayee_et_remonte()
    {
        // Erreur de base différente de l'index du code : clé primaire (Id) déjà utilisée.
        _db.Clients.Add(new Client { Id = 50, Code = "CLI-00050", Nom = "Existant" });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear(); // l'existant n'est plus suivi : seule la base le connaît

        var service = new ClientServiceCodesImposes(_db); // codes réels
        var client = NouveauClient();
        client.Id = 50;

        await Assert.ThrowsAsync<DbUpdateException>(() => service.CreerAsync(client));
        Assert.Equal(1, service.NombreAppels); // un seul essai : pas de nouvelle tentative
    }

    [Fact]
    public void Seule_la_violation_de_l_index_du_code_est_reconnue()
    {
        var autre = new DbUpdateException("x", new InvalidOperationException("UNIQUE constraint failed: Clients.Email"));
        Assert.False(ClientService.EstViolationIndexCode(autre));
    }

    // ---------- Unicité ----------

    [Fact]
    public async Task Email_duplique_est_refuse_sans_tenir_compte_de_la_casse_ni_des_espaces()
    {
        var service = new ClientService(_db);
        Assert.True((await service.CreerAsync(NouveauClient("A", email: "Ali@Test.com"))).Succes);

        var resultat = await service.CreerAsync(NouveauClient("B", email: "  ali@TEST.com "));

        Assert.False(resultat.Succes);
        Assert.Contains(resultat.Erreurs, e => e.Champ == nameof(Client.Email));
        Assert.Equal(1, await _db.Clients.CountAsync());
    }

    [Fact]
    public async Task Matricule_duplique_est_refuse_sans_tenir_compte_de_la_casse_ni_des_espaces()
    {
        var service = new ClientService(_db);
        Assert.True((await service.CreerAsync(NouveauClient("A", matricule: "1234567a/p/m/000"))).Succes);

        var resultat = await service.CreerAsync(NouveauClient("B", matricule: " 1234567A/P/M/000 "));

        Assert.False(resultat.Succes);
        Assert.Contains(resultat.Erreurs, e => e.Champ == nameof(Client.MatriculeFiscale));
        Assert.Equal(1, await _db.Clients.CountAsync());
    }

    [Fact]
    public async Task Deux_clients_sans_email_ni_matricule_sont_acceptes()
    {
        var service = new ClientService(_db);

        Assert.True((await service.CreerAsync(NouveauClient("A"))).Succes);
        Assert.True((await service.CreerAsync(NouveauClient("B", email: "   ", matricule: ""))).Succes);
        Assert.True((await service.CreerAsync(NouveauClient("C", email: "", matricule: "   "))).Succes);

        Assert.Equal(3, await _db.Clients.CountAsync());
    }

    [Fact]
    public async Task Verification_d_unicite_ignore_le_client_modifie()
    {
        var service = new ClientService(_db);
        var client = NouveauClient("A", email: "a@test.com");
        Assert.True((await service.CreerAsync(client)).Succes);

        var memeClient = new Client { Nom = "A", Email = "A@test.com" };

        Assert.False((await service.VerifierUniciteAsync(memeClient)).Succes);                // un autre client avec cet email
        Assert.True((await service.VerifierUniciteAsync(memeClient, client.Id)).Succes);       // c'est lui-même : accepté
    }

    // ---------- Validation et nettoyage ----------

    [Fact]
    public async Task Une_donnee_invalide_est_refusee_avant_toute_ecriture()
    {
        var service = new ClientService(_db);
        var client = NouveauClient("A", matricule: "12-34");
        client.CodePostal = "75001";

        var resultat = await service.CreerAsync(client);

        Assert.False(resultat.Succes);
        Assert.Contains(resultat.Erreurs, e => e.Champ == nameof(Client.MatriculeFiscale));
        Assert.Contains(resultat.Erreurs, e => e.Champ == nameof(Client.CodePostal));
        Assert.Equal(0, await _db.Clients.CountAsync());
    }

    // Choix assumé : le service nettoie avec Trim() COMPLET (espaces, retours à la ligne, tabulations)
    // AVANT la validation. Un matricule collé avec un retour à la ligne final est donc enregistré
    // nettoyé, sans erreur. (ClientValidation seul, lui, refuse "1234567A\n" : voir ClientValidationTests.)
    [Theory]
    [InlineData("ABC\n", "ABC")]
    [InlineData("1234567A\n", "1234567A")]
    [InlineData("  1234567A/P/M/000\r\n", "1234567A/P/M/000")]
    public async Task Un_matricule_avec_retour_a_la_ligne_est_enregistre_nettoye_sans_erreur(string saisi, string attendu)
    {
        var service = new ClientService(_db);
        var client = NouveauClient("A", matricule: saisi);

        var resultat = await service.CreerAsync(client);

        Assert.True(resultat.Succes);
        var enBase = await _db.Clients.AsNoTracking().SingleAsync();
        Assert.Equal(attendu, enBase.MatriculeFiscale);
    }

    [Fact]
    public async Task Les_champs_sont_nettoyes_avant_enregistrement()
    {
        var service = new ClientService(_db);
        var client = NouveauClient("  Dupont  ", email: " a@b.com ");
        client.Ville = "   ";

        Assert.True((await service.CreerAsync(client)).Succes);

        var enBase = await _db.Clients.AsNoTracking().SingleAsync();
        Assert.Equal("Dupont", enBase.Nom);
        Assert.Equal("a@b.com", enBase.Email);
        Assert.Null(enBase.Ville);
        Assert.True(enBase.EstActif);
    }
}
