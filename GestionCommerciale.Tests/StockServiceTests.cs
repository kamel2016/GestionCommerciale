using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

/// <summary>Teste StockService directement : le signe, les refus et l'historique sont calculés par le service.</summary>
public class StockServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _db;
    private readonly StockService _service;

    public StockServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options;
        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
        _service = new StockService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<int> CreerArticleAsync(int stock)
    {
        var article = new Article { Reference = "REF-" + Guid.NewGuid().ToString("N")[..8], Libelle = "Article test", Quantite = stock };
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        return article.ID;
    }

    private async Task<(int Stock, List<MouvementStock> Mouvements)> LireAsync(int articleId)
    {
        _db.ChangeTracker.Clear();
        var stock = (await _db.Articles.AsNoTracking().SingleAsync(a => a.ID == articleId)).Quantite;
        var mouvements = await _db.MouvementsStock.AsNoTracking().Where(m => m.ArticleId == articleId).ToListAsync();
        return (stock, mouvements);
    }

    [Theory]
    [InlineData(TypeMouvementStock.EntreeReception, 5, 15, 5)]
    [InlineData(TypeMouvementStock.RetourClient, 3, 13, 3)]
    [InlineData(TypeMouvementStock.RetourFournisseur, 4, 6, -4)]
    [InlineData(TypeMouvementStock.AjustementInventaire, 2, 12, 2)]
    [InlineData(TypeMouvementStock.AjustementInventaire, -2, 8, -2)]
    public async Task Le_signe_du_mouvement_est_calcule_par_le_service(
        TypeMouvementStock type, int saisie, int stockAttendu, int quantiteMouvement)
    {
        var id = await CreerArticleAsync(10);

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, type, saisie, "motif");
        await _db.SaveChangesAsync();

        Assert.True(resultat.Succes);
        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(stockAttendu, stock);
        var m = Assert.Single(mouvements);
        Assert.Equal(quantiteMouvement, m.Quantite);
        Assert.Equal(10, m.QuantiteAvant);
        Assert.Equal(stockAttendu, m.QuantiteApres);
        Assert.Equal(type, m.Type);
    }

    [Fact]
    public async Task La_sortie_livraison_est_refusee_pour_une_saisie_manuelle()
    {
        var id = await CreerArticleAsync(10);

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, TypeMouvementStock.SortieLivraison, 2, "x");
        await _db.SaveChangesAsync();

        Assert.False(resultat.Succes);
        Assert.Contains("bons de livraison", resultat.Message);
        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);
        Assert.Empty(mouvements);
    }

    [Theory]
    [InlineData(TypeMouvementStock.TransfertInterne)]
    [InlineData((TypeMouvementStock)99)]
    public async Task Les_autres_types_sont_refuses(TypeMouvementStock type)
    {
        var id = await CreerArticleAsync(10);

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, type, 2, "x");

        Assert.False(resultat.Succes);
        Assert.Empty((await LireAsync(id)).Mouvements);
    }

    [Theory]
    [InlineData(TypeMouvementStock.EntreeReception, 0)]
    [InlineData(TypeMouvementStock.EntreeReception, -5)]   // le signe se calcule dans le service, pas dans la saisie
    [InlineData(TypeMouvementStock.RetourClient, -1)]
    [InlineData(TypeMouvementStock.RetourFournisseur, -1)]
    public async Task Hors_ajustement_la_quantite_doit_etre_strictement_positive(TypeMouvementStock type, int saisie)
    {
        var id = await CreerArticleAsync(10);

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, type, saisie, null);
        await _db.SaveChangesAsync();

        Assert.False(resultat.Succes);
        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);
        Assert.Empty(mouvements);
    }

    [Fact]
    public async Task Un_refus_de_stock_negatif_ne_laisse_aucune_trace_et_donne_un_message()
    {
        var id = await CreerArticleAsync(10);

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, TypeMouvementStock.AjustementInventaire, -11, "motif");
        await _db.SaveChangesAsync();

        Assert.False(resultat.Succes);
        Assert.Contains("Stock insuffisant", resultat.Message);
        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);
        Assert.Empty(mouvements);
    }

    [Fact]
    public async Task Le_motif_est_nettoye_et_enregistre_dans_le_commentaire()
    {
        var id = await CreerArticleAsync(10);

        await _service.EnregistrerMouvementManuelAsync(id, TypeMouvementStock.AjustementInventaire, 1, "  Casse  ");
        await _db.SaveChangesAsync();

        Assert.Equal("Casse", Assert.Single((await LireAsync(id)).Mouvements).Commentaire);
    }

    [Theory]
    [InlineData(TypeMouvementStock.EntreeReception, 10_000_000)]
    [InlineData(TypeMouvementStock.RetourClient, 10_000_000)]
    [InlineData(TypeMouvementStock.RetourFournisseur, 10_000_000)]
    [InlineData(TypeMouvementStock.AjustementInventaire, 10_000_000)]
    [InlineData(TypeMouvementStock.AjustementInventaire, -10_000_000)]
    [InlineData(TypeMouvementStock.AjustementInventaire, int.MinValue)]
    [InlineData(TypeMouvementStock.EntreeReception, int.MaxValue)]
    public async Task Une_quantite_superieure_a_la_borne_haute_est_refusee_pour_tous_les_types(TypeMouvementStock type, int saisie)
    {
        var id = await CreerArticleAsync(10);

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, type, saisie, "motif");
        await _db.SaveChangesAsync();

        Assert.False(resultat.Succes);
        Assert.Contains("ne doit pas dépasser", resultat.Message);
        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);
        Assert.Empty(mouvements);
    }

    [Fact]
    public async Task La_borne_haute_elle_meme_est_acceptee()
    {
        var id = await CreerArticleAsync(0);

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, TypeMouvementStock.EntreeReception, 9_999_999, null);
        await _db.SaveChangesAsync();

        Assert.True(resultat.Succes);
        Assert.Equal(9_999_999, (await LireAsync(id)).Stock);
    }

    [Fact]
    public async Task Un_motif_de_plus_de_250_caracteres_est_refuse()
    {
        var id = await CreerArticleAsync(10);

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, TypeMouvementStock.EntreeReception, 1, new string('a', 251));

        Assert.False(resultat.Succes);
    }

    [Fact]
    public async Task Un_article_inexistant_est_signale_introuvable()
    {
        var resultat = await _service.EnregistrerMouvementManuelAsync(9999, TypeMouvementStock.EntreeReception, 1, null);

        Assert.True(resultat.Introuvable);
        Assert.False(resultat.Succes);
    }

    [Fact]
    public async Task Le_mouvement_technique_RetourFournisseur_diminue_et_renseigne_QuantiteAvant()
    {
        var id = await CreerArticleAsync(10);

        await _service.EnregistrerMouvementAsync(id, TypeMouvementStock.RetourFournisseur, 4);
        await _db.SaveChangesAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(6, stock);
        var m = Assert.Single(mouvements);
        Assert.Equal(-4, m.Quantite);
        Assert.Equal(10, m.QuantiteAvant);
    }

    [Fact]
    public async Task Le_mouvement_technique_refuse_l_ajustement_signe()
    {
        var id = await CreerArticleAsync(10);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.EnregistrerMouvementAsync(id, TypeMouvementStock.AjustementInventaire, 3));
    }
}
