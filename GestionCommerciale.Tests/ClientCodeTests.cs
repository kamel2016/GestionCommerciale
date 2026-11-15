using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

public class ClientCodeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;

    public ClientCodeTests()
    {
        // Base SQLite en mémoire : disparaît à la fin de chaque test, ne touche jamais SQL Server.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Un_client_peut_etre_enregistre_avec_son_code()
    {
        _db.Clients.Add(new Client { Code = "CLI-00001", Nom = "Test" });
        await _db.SaveChangesAsync();

        var client = await _db.Clients.SingleAsync();
        Assert.Equal("CLI-00001", client.Code);
    }

    [Fact]
    public async Task Deux_clients_ne_peuvent_pas_avoir_le_meme_code()
    {
        _db.Clients.Add(new Client { Code = "CLI-00001", Nom = "Premier" });
        await _db.SaveChangesAsync();

        _db.Clients.Add(new Client { Code = "CLI-00001", Nom = "Second" });

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
