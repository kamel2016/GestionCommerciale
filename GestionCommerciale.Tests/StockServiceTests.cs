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
    [InlineData(TypeMouvementStock.RetourFournisseur, 4, 6, 4)]
    [InlineData(TypeMouvementStock.AjustementPositif, 2, 12, 2)]
    [InlineData(TypeMouvementStock.AjustementNegatif, 2, 8, 2)]
    public async Task Le_sens_est_porte_par_le_type_et_la_quantite_enregistree_est_toujours_positive(
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
    [InlineData(TypeMouvementStock.AjustementPositif, 0)]
    [InlineData(TypeMouvementStock.AjustementPositif, -3)]
    [InlineData(TypeMouvementStock.AjustementNegatif, 0)]
    [InlineData(TypeMouvementStock.AjustementNegatif, -3)]
    public async Task La_quantite_doit_etre_strictement_positive_pour_tous_les_types(TypeMouvementStock type, int saisie)
    {
        var id = await CreerArticleAsync(10);

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, type, saisie, "motif");
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

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, TypeMouvementStock.AjustementNegatif, 11, "motif");
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

        await _service.EnregistrerMouvementManuelAsync(id, TypeMouvementStock.AjustementPositif, 1, "  Casse  ");
        await _db.SaveChangesAsync();

        Assert.Equal("Casse", Assert.Single((await LireAsync(id)).Mouvements).Commentaire);
    }

    [Theory]
    [InlineData(TypeMouvementStock.EntreeReception, 10_000_000)]
    [InlineData(TypeMouvementStock.RetourClient, 10_000_000)]
    [InlineData(TypeMouvementStock.RetourFournisseur, 10_000_000)]
    [InlineData(TypeMouvementStock.AjustementPositif, 10_000_000)]
    [InlineData(TypeMouvementStock.AjustementNegatif, 10_000_000)]
    [InlineData(TypeMouvementStock.AjustementNegatif, int.MinValue)]
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
        Assert.Equal(4, m.Quantite);                       // positive : le sens est porté par le type
        Assert.Equal(10, m.QuantiteAvant);
    }

    [Fact]
    public async Task Le_mouvement_technique_refuse_l_ajustement_signe()
    {
        var id = await CreerArticleAsync(10);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _service.EnregistrerMouvementAsync(id, TypeMouvementStock.AjustementInventaire, 3));
    }

    // ---------------------------------------------------------------- S6 étape 2 : convention unique

    [Theory]
    [InlineData(TypeMouvementStock.EntreeReception, 1)]
    [InlineData(TypeMouvementStock.RetourClient, 1)]
    [InlineData(TypeMouvementStock.AjustementPositif, 1)]
    [InlineData(TypeMouvementStock.EntreeAnnulationLivraison, 1)]
    [InlineData(TypeMouvementStock.SortieLivraison, -1)]
    [InlineData(TypeMouvementStock.RetourFournisseur, -1)]
    [InlineData(TypeMouvementStock.AjustementNegatif, -1)]
    [InlineData(TypeMouvementStock.AjustementInventaire, 0)]   // ancien type signé : plus de sens défini
    [InlineData(TypeMouvementStock.TransfertInterne, 0)]
    public void Le_sens_de_chaque_type_est_defini_en_un_seul_endroit(TypeMouvementStock type, int attendu)
        => Assert.Equal(attendu, type.Sens());

    [Theory]
    [InlineData(TypeMouvementStock.EntreeReception, 4, 4)]
    [InlineData(TypeMouvementStock.SortieLivraison, 4, -4)]
    [InlineData(TypeMouvementStock.EntreeAnnulationLivraison, 4, 4)]
    [InlineData(TypeMouvementStock.AjustementNegatif, 4, -4)]
    [InlineData(TypeMouvementStock.AjustementInventaire, 4, 0)]
    public void La_quantite_signee_d_affichage_se_deduit_du_type(TypeMouvementStock type, int quantite, int attendue)
        => Assert.Equal(attendue, new MouvementStock { Type = type, Quantite = quantite }.QuantiteSignee);

    [Theory]
    [InlineData(TypeMouvementStock.EntreeReception, 15)]
    [InlineData(TypeMouvementStock.RetourClient, 15)]
    [InlineData(TypeMouvementStock.EntreeAnnulationLivraison, 15)]
    [InlineData(TypeMouvementStock.AjustementPositif, 15)]
    [InlineData(TypeMouvementStock.SortieLivraison, 5)]
    [InlineData(TypeMouvementStock.RetourFournisseur, 5)]
    [InlineData(TypeMouvementStock.AjustementNegatif, 5)]
    public async Task Le_point_d_entree_technique_applique_le_bon_effet_pour_chaque_type(TypeMouvementStock type, int stockAttendu)
    {
        var id = await CreerArticleAsync(10);

        await _service.EnregistrerMouvementAsync(id, type, 5, "commentaire", "REF-DOC", "Source", 42);
        await _db.SaveChangesAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(stockAttendu, stock);
        var m = Assert.Single(mouvements);
        Assert.Equal(5, m.Quantite);                  // toujours positive
        Assert.Equal(10, m.QuantiteAvant);
        Assert.Equal(stockAttendu, m.QuantiteApres);
        Assert.Equal("REF-DOC", m.Reference);
        Assert.Equal("Source", m.SourceType);
        Assert.Equal(42, m.SourceId);
        Assert.Equal(0m, m.CoutUnitaire);             // aucune source fiable de coût (CMUP jamais calculé) : non renseigné
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task Le_point_d_entree_technique_refuse_une_quantite_nulle_ou_negative_sans_rien_enregistrer(int quantite)
    {
        var id = await CreerArticleAsync(10);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.EnregistrerMouvementAsync(id, TypeMouvementStock.EntreeReception, quantite));
        await _db.SaveChangesAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);
        Assert.Empty(mouvements);
    }

    [Fact]
    public async Task Le_point_d_entree_technique_refuse_une_sortie_superieure_au_stock_sans_rien_modifier()
    {
        var id = await CreerArticleAsync(10);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.EnregistrerMouvementAsync(id, TypeMouvementStock.SortieLivraison, 11));
        await _db.SaveChangesAsync();

        var (stock, mouvements) = await LireAsync(id);
        Assert.Equal(10, stock);
        Assert.Empty(mouvements);
    }

    [Theory]
    [InlineData(TypeMouvementStock.TransfertInterne)]
    [InlineData((TypeMouvementStock)99)]
    public async Task Le_point_d_entree_technique_refuse_un_type_sans_sens(TypeMouvementStock type)
    {
        var id = await CreerArticleAsync(10);

        await Assert.ThrowsAsync<NotSupportedException>(() => _service.EnregistrerMouvementAsync(id, type, 1));
        Assert.Empty((await LireAsync(id)).Mouvements);
    }

    [Fact]
    public async Task La_saisie_manuelle_refuse_l_ancien_ajustement_signe_et_l_annulation_de_livraison()
    {
        var id = await CreerArticleAsync(10);

        var ancien = await _service.EnregistrerMouvementManuelAsync(id, TypeMouvementStock.AjustementInventaire, 3, "motif");
        var annulation = await _service.EnregistrerMouvementManuelAsync(id, TypeMouvementStock.EntreeAnnulationLivraison, 3, "motif");
        await _db.SaveChangesAsync();

        Assert.False(ancien.Succes);
        Assert.Contains("positif ou négatif", ancien.Message);
        Assert.False(annulation.Succes);
        Assert.Contains("bons de livraison", annulation.Message);
        Assert.Empty((await LireAsync(id)).Mouvements);
    }

    [Theory]
    [InlineData(TypeMouvementStock.AjustementPositif)]
    [InlineData(TypeMouvementStock.AjustementNegatif)]
    public async Task Un_ajustement_sans_motif_est_refuse_pour_les_deux_sens(TypeMouvementStock type)
    {
        var id = await CreerArticleAsync(10);

        var resultat = await _service.EnregistrerMouvementManuelAsync(id, type, 2, "   ");
        await _db.SaveChangesAsync();

        Assert.False(resultat.Succes);
        Assert.Contains("motif", resultat.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty((await LireAsync(id)).Mouvements);
    }
}
