using GestionCommerciale.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GestionCommerciale.Tests;

/// <summary>
/// Tests qui s'exécutent sur un VRAI SQL Server (LocalDB), dans une base jetable créée puis supprimée par les tests.
/// Ils ne tournent que si la variable d'environnement GC_TESTS_SQLSERVER vaut 1 ; sinon ils sont signalés « ignorés »
/// et la suite par défaut n'est pas affectée. Exemple : GC_TESTS_SQLSERVER=1 dotnet test --filter "Category=SqlServer".
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string VariableActivation = "GC_TESTS_SQLSERVER";

    public SqlServerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(VariableActivation) != "1")
            Skip = $"Test SQL Server ignoré : définir {VariableActivation}=1 (LocalDB requis ; base jetable " +
                   $"{BaseJetableFixture.PrefixeBase}*, jamais la base de développement).";
    }
}

/// <summary>
/// Crée UNE base jetable par classe de tests (Migrate(), donc avec les vraies migrations) et la supprime à la fin.
/// GARDE-FOUS (vérifiés AVANT toute connexion, y compris avant la suppression) :
///   1. le serveur doit être LocalDB ((localdb)\...) : toute autre chaîne est refusée ;
///   2. le nom de la base doit commencer par « GestionCommerciale_Test_ ».
/// La chaîne de connexion est construite ICI : appsettings.json (base de développement) n'est jamais lu.
/// </summary>
public sealed class BaseJetableFixture : IDisposable
{
    public const string PrefixeBase = "GestionCommerciale_Test_";
    private const string ServeurParDefaut = @"(localdb)\MSSQLLocalDB";
    private const string VariableServeur = "GC_TEST_SQLSERVER_SERVER";

    private readonly object _verrou = new();
    private bool _creee;

    public string NomBase { get; } = PrefixeBase + Guid.NewGuid().ToString("N");

    public string ChaineConnexion { get; }

    public BaseJetableFixture()
    {
        // Le constructeur ne se connecte à rien : xUnit l'appelle même quand tous les tests sont ignorés.
        var serveur = Environment.GetEnvironmentVariable(VariableServeur);
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = string.IsNullOrWhiteSpace(serveur) ? ServeurParDefaut : serveur,
            InitialCatalog = NomBase,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            ConnectTimeout = 60
        };
        ChaineConnexion = builder.ConnectionString;
    }

    /// <summary>Refuse toute cible qui n'est pas LocalDB ou dont la base n'a pas le préfixe jetable.</summary>
    public static void ControlerCible(string chaineConnexion)
    {
        var b = new SqlConnectionStringBuilder(chaineConnexion);

        if (!b.DataSource.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Refus : la chaîne de connexion ne pointe pas sur LocalDB (« {b.DataSource} »). Les tests SQL Server n'utilisent que LocalDB.");

        if (!b.InitialCatalog.StartsWith(PrefixeBase, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Refus : la base « {b.InitialCatalog} » n'a pas le préfixe {PrefixeBase} (base jetable obligatoire).");
    }

    /// <summary>Crée la base (Migrate) au premier appel. Si une migration échoue, l'exception remonte telle quelle.</summary>
    public void AssurerBase()
    {
        lock (_verrou)
        {
            if (_creee) return;
            ControlerCible(ChaineConnexion);
            _creee = true; // même en cas d'échec de Migrate : Dispose tentera de supprimer la base jetable
            using var contexte = NouveauContexte();
            contexte.Database.Migrate();
        }
    }

    public ApplicationDbContext NouveauContexte(params IInterceptor[] intercepteurs)
    {
        ControlerCible(ChaineConnexion);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ChaineConnexion)
            .AddInterceptors(intercepteurs)
            .Options;
        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        if (!_creee) return;
        ControlerCible(ChaineConnexion);   // jamais de suppression sans les deux garde-fous
        try
        {
            SqlConnection.ClearAllPools();
            using var contexte = NouveauContexte();
            contexte.Database.EnsureDeleted();
        }
        catch
        {
            // La base jetable pourrait rester : son nom (GestionCommerciale_Test_*) permet de la retrouver et de la supprimer à la main.
        }
    }

    /// <summary>Retrouve la SqlException réelle dans une exception d'enregistrement EF.</summary>
    public static SqlException? ExtraireSqlException(Exception exception)
    {
        for (Exception? e = exception; e != null; e = e.InnerException)
            if (e is SqlException sql) return sql;
        return null;
    }
}
