using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using GestionCommerciale.Services.Interface;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GestionCommerciale.Tests;

/// <summary>
/// Exécute une action UNE seule fois, juste avant le prochain SaveChanges du contexte testé.
/// Sert à fabriquer une VRAIE course : une autre session insère un doublon (ou supprime une catégorie) entre le
/// contrôle fait par le service et l'enregistrement, sans aucun simulateur dans le code de production.
/// </summary>
public sealed class ActionAvantEnregistrement : SaveChangesInterceptor
{
    public Func<Task>? Action { get; set; }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var action = Action;
        Action = null;
        if (action != null) await action();
        return result;
    }
}

/// <summary>Base commune des tests d'articles : SQLite en mémoire (clés étrangères ACTIVES), service, 2e contexte pour les courses.</summary>
public abstract class ArticleTestBase : IDisposable
{
    protected readonly SqliteConnection Connection;
    protected readonly ApplicationDbContext Db;
    protected readonly ActionAvantEnregistrement Interception = new();
    protected readonly ArticleService Service;

    protected ArticleTestBase()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(Connection)
            .AddInterceptors(Interception)
            .Options;

        Db = new ApplicationDbContext(options);
        Db.Database.EnsureCreated();
        Service = new ArticleService(Db);
    }

    public void Dispose()
    {
        Db.Dispose();
        Connection.Dispose();
    }

    /// <summary>Autre « session » sur la même base, sans intercepteur.</summary>
    protected ApplicationDbContext AutreSession()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(Connection).Options);

    /// <summary>Article tel que le formulaire le poste : valide, sans aucun champ interdit.</summary>
    protected static Article Saisie(string reference = "REF-1", string libelle = "Libellé test") => new()
    {
        Reference = reference,
        Libelle = libelle,
        Unite = "Pièce",
        TauxTVA = 19m,
        EstActif = true
    };

    /// <summary>Insère directement un article en base (hors service) et rend son Id.</summary>
    protected async Task<Article> InsererAsync(string reference, string? codeBarre = null, int stock = 0, int? categorieId = null)
    {
        var article = new Article { Reference = reference, Libelle = "Existant " + reference, CodeBarre = codeBarre, Quantite = stock, CategoryId = categorieId };
        Db.Articles.Add(article);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return article;
    }

    protected async Task<int> InsererCategorieAsync(string nom = "Catégorie 1")
    {
        var categorie = new Category { Name = nom };
        Db.Categories.Add(categorie);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return categorie.Id;
    }

    /// <summary>Relit un article depuis la base (contexte vidé).</summary>
    protected async Task<Article?> LireAsync(int id)
    {
        Db.ChangeTracker.Clear();
        return await Db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.ID == id);
    }

    protected async Task<int> CompterArticlesAsync()
    {
        Db.ChangeTracker.Clear();
        return await Db.Articles.CountAsync();
    }

    protected static List<string> Messages(ResultatArticle resultat, string champ)
        => resultat.Erreurs.Where(e => e.Champ == champ).Select(e => e.Message).ToList();
}
