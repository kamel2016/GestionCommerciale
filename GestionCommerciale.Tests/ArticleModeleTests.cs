using GestionCommerciale.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

/// <summary>S4 étape 8 : la précision de Article.TauxTVA est définie une seule fois (5,2), comme le TauxTVA des lignes.</summary>
public class ArticleModeleTests : ArticleTestBase
{
    [Fact]
    public void Le_taux_de_tva_de_l_article_est_decimal_5_2()
    {
        var propriete = Db.Model.FindEntityType(typeof(Article))!.FindProperty(nameof(Article.TauxTVA))!;

        Assert.Equal(5, propriete.GetPrecision());
        Assert.Equal(2, propriete.GetScale());
    }

    [Fact]
    public void La_precision_du_taux_de_tva_n_est_pas_configuree_deux_fois()
    {
        var racine = AppContext.BaseDirectory;
        while (racine != null && !File.Exists(Path.Combine(racine, "GestionCommerciale.sln"))) racine = Path.GetDirectoryName(racine);
        Assert.NotNull(racine);
        var config = File.ReadAllText(Path.Combine(racine!, "GestionCommerciale", "Infrastructure", "ArticleConfiguration.cs"));

        Assert.DoesNotContain("TauxTVA", config);   // la précision est définie dans ApplicationDbContext uniquement
    }
}
