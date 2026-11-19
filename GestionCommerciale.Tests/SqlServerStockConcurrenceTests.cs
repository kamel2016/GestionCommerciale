using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace GestionCommerciale.Tests;

/// <summary>
/// S6 étape 4 : concurrence sur le stock sur un VRAI SQL Server (base jetable LocalDB créée par Migrate()). SQLite ne
/// reproduit pas le rowversion : seul ce test prouve qu'une mise à jour simultanée est refusée au lieu d'être perdue.
/// Exécuté seulement avec GC_TESTS_SQLSERVER=1.
/// </summary>
[Trait("Category", "SqlServer")]
public class SqlServerStockConcurrenceTests : IClassFixture<BaseJetableFixture>
{
    private readonly BaseJetableFixture _base;
    private readonly ITestOutputHelper _sortie;

    public SqlServerStockConcurrenceTests(BaseJetableFixture baseJetable, ITestOutputHelper sortie)
    {
        _base = baseJetable;
        _sortie = sortie;
    }

    private static string Unique(string prefixe) => prefixe + "-" + Guid.NewGuid().ToString("N")[..10];

    private async Task<(int ArticleId, int Bl1, int Bl2)> PreparerAsync(int stock, int quantiteParBl)
    {
        using var db = _base.NouveauContexte();
        var article = new Article { Reference = Unique("S6C"), Libelle = "Concurrence", Quantite = stock };
        db.Articles.Add(article);
        var bl1 = new BonLivraison { Numero = Unique("BL"), Statut = StatutBonLivraison.Brouillon };
        var bl2 = new BonLivraison { Numero = Unique("BL"), Statut = StatutBonLivraison.Brouillon };
        await db.SaveChangesAsync();
        foreach (var bl in new[] { bl1, bl2 })
        {
            bl.Lignes.Add(new LigneBonLivraison { ArticleId = article.ID, Quantite = quantiteParBl, PrixUnitaire = 1m });
            db.BonsLivraison.Add(bl);
        }
        await db.SaveChangesAsync();
        return (article.ID, bl1.ID, bl2.ID);
    }

    [SqlServerFact]
    public async Task S6c_deux_validations_simultanees_ne_rendent_pas_le_stock_negatif_et_ne_perdent_aucune_mise_a_jour()
    {
        _base.AssurerBase();
        var (articleId, bl1, bl2) = await PreparerAsync(stock: 10, quantiteParBl: 8);   // 8 + 8 > 10 : une seule validation doit réussir

        // La session B lit son BL et son stock (10), puis, JUSTE AVANT son enregistrement, la session A valide l'autre BL.
        var interception = new ActionAvantEnregistrement();
        using var sessionB = _base.NouveauContexte(interception);
        var serviceB = new BonLivraisonService(sessionB, new StockService(sessionB));
        interception.Action = async () =>
        {
            using var sessionA = _base.NouveauContexte();
            var (okA, erreurA) = await new BonLivraisonService(sessionA, new StockService(sessionA)).ValiderAsync(bl1);
            Assert.True(okA, erreurA);
        };

        var exception = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => serviceB.ValiderAsync(bl2));
        _sortie.WriteLine("[S6c] exception réelle : " + exception.GetType().Name + " : " + exception.Message);

        using var lecture = _base.NouveauContexte();
        Assert.Equal(2, (await lecture.Articles.AsNoTracking().SingleAsync(a => a.ID == articleId)).Quantite);   // 10 - 8, jamais -6
        Assert.Equal(1, await lecture.MouvementsStock.CountAsync(m => m.ArticleId == articleId));
        Assert.Equal(StatutBonLivraison.Brouillon, (await lecture.BonsLivraison.AsNoTracking().SingleAsync(b => b.ID == bl2)).Statut);
        Assert.Equal(StatutBonLivraison.Valide, (await lecture.BonsLivraison.AsNoTracking().SingleAsync(b => b.ID == bl1)).Statut);
    }

    [SqlServerFact]
    public async Task S6d_la_nouvelle_tentative_apres_le_conflit_est_refusee_par_le_controle_du_stock()
    {
        _base.AssurerBase();
        var (articleId, bl1, bl2) = await PreparerAsync(stock: 10, quantiteParBl: 8);
        using (var sessionA = _base.NouveauContexte())
            Assert.True((await new BonLivraisonService(sessionA, new StockService(sessionA)).ValiderAsync(bl1)).Succes);

        using var sessionB = _base.NouveauContexte();
        var (succes, erreur) = await new BonLivraisonService(sessionB, new StockService(sessionB)).ValiderAsync(bl2);

        Assert.False(succes);
        Assert.Contains("Stock insuffisant", erreur);
        using var lecture = _base.NouveauContexte();
        Assert.Equal(2, (await lecture.Articles.AsNoTracking().SingleAsync(a => a.ID == articleId)).Quantite);
    }
}
