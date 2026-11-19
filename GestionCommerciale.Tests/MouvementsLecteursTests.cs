using System.Text.RegularExpressions;
using GestionCommerciale.Models;

namespace GestionCommerciale.Tests;

/// <summary>
/// S6 étape 2 : les trois écrans qui affichent des mouvements de stock (fiche article, historique de la fiche de modification,
/// page Stock/Mouvements) déduisent le sens du TYPE et non du signe de la quantité (qui est maintenant toujours positive).
/// Ces tests lisent le texte des vues (pas de rendu Razor) : ils prouvent le contrat, pas l'affichage dans un navigateur.
/// </summary>
public class MouvementsLecteursTests : ArticleTestBase
{
    private static string Vue(params string[] segments)
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "GestionCommerciale.sln"))) dir = Path.GetDirectoryName(dir);
        var texte = File.ReadAllText(Path.Combine(new[] { dir!, "GestionCommerciale", "Pages" }.Concat(segments).ToArray()));
        return Regex.Replace(texte, @"@\*.*?\*@", string.Empty, RegexOptions.Singleline);   // sans les commentaires Razor
    }

    [Fact]
    public void La_fiche_article_Details_deduit_le_sens_du_type()
    {
        var vue = Vue("Articles", "Details.cshtml");

        Assert.Contains("movement.QuantiteSignee > 0", vue);
        Assert.DoesNotContain("movement.Quantite > 0", vue);
    }

    [Fact]
    public void L_historique_de_la_fiche_de_modification_deduit_le_sens_du_type()
    {
        var vue = Vue("Articles", "Edit.cshtml");

        Assert.Contains("m.QuantiteSignee.ToString(\"+0;-0\")", vue);
        Assert.DoesNotContain("m.Quantite.ToString(\"+0;-0\")", vue);
    }

    [Fact]
    public void La_page_Stock_Mouvements_deduit_le_sens_du_type()
    {
        var vue = Vue("Stock", "Mouvements.cshtml");

        Assert.Contains("m.QuantiteSignee.ToString(\"+0;-0;0\")", vue);
        Assert.DoesNotContain("@m.Quantite</td>", vue);
    }

    [Fact]
    public void Le_formulaire_de_mouvement_propose_les_deux_ajustements_et_plus_l_ancien()
    {
        var vue = Vue("Articles", "Edit.cshtml");

        Assert.Contains("value=\"AjustementPositif\"", vue);
        Assert.Contains("value=\"AjustementNegatif\"", vue);
        Assert.DoesNotContain("value=\"AjustementInventaire\"", vue);
        Assert.DoesNotContain("négatif pour retirer", vue);       // la quantité saisie est toujours positive
    }

    [Fact]
    public async Task La_fiche_donne_une_quantite_positive_et_un_sens_negatif_pour_une_sortie()
    {
        var article = await InsererAsync("LECT-1", stock: 7);
        Db.MouvementsStock.Add(new MouvementStock { ArticleId = article.ID, Type = TypeMouvementStock.SortieLivraison, Quantite = 3, QuantiteAvant = 10, QuantiteApres = 7 });
        await Db.SaveChangesAsync();

        var fiche = await Service.ObtenirFicheAsync(article.ID);

        var m = Assert.Single(fiche!.DerniersMouvements);
        Assert.Equal(3, m.Quantite);
        Assert.Equal(-3, m.QuantiteSignee);
    }
}
