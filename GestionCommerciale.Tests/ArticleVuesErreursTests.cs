using System.Text.RegularExpressions;
using GestionCommerciale.Helpers;
using GestionCommerciale.Models;

namespace GestionCommerciale.Tests;

/// <summary>
/// Contrat des vues : toute erreur que ArticleService peut renvoyer pour un champ doit avoir un emplacement d'affichage
/// dans la page (un asp-validation-for non commenté, ou le résumé « All » de la page de création).
/// ATTENTION : ce test lit le TEXTE des vues (copiées à côté des tests). Il ne rend pas la page : il ne prouve pas
/// l'affichage dans un navigateur, seulement que l'emplacement existe dans le gabarit.
/// </summary>
public class ArticleVuesErreursTests
{
    private static string Lire(string page)
    {
        var chemin = Path.Combine(AppContext.BaseDirectory, "Pages", "Articles", page + ".cshtml");
        Assert.True(File.Exists(chemin), $"Vue introuvable : {chemin}");
        var texte = File.ReadAllText(chemin);
        // Les commentaires Razor @* ... *@ ne produisent rien : on les retire avant de chercher.
        return Regex.Replace(texte, @"@\*.*?\*@", string.Empty, RegexOptions.Singleline);
    }

    /// <summary>Tous les champs pour lesquels ArticleValidation / ArticleService peuvent renvoyer une erreur.</summary>
    private static readonly string[] ChampsAvecErreur =
    {
        nameof(Article.Reference), nameof(Article.Libelle), nameof(Article.CodeBarre), nameof(Article.CategoryId),
        nameof(Article.Unite), nameof(Article.Marque), nameof(Article.Modele), nameof(Article.Couleur), nameof(Article.Taille),
        nameof(Article.Poids), nameof(Article.PrixAchatHT), nameof(Article.PrixVenteHT), nameof(Article.PrixVenteTTC),
        nameof(Article.TauxTVA), nameof(Article.StockMin), nameof(Article.StockMax)
    };

    [Fact]
    public void La_liste_des_champs_couvre_tous_les_champs_que_la_validation_peut_signaler()
    {
        // Article maximalement invalide : si une règle future ajoute un champ, ce test oblige à le lister (et donc à lui donner un emplacement).
        var invalide = new Article
        {
            Reference = "", Libelle = "", Unite = "",
            CodeBarre = new string('x', 51), Marque = new string('x', 101), Modele = new string('x', 101),
            Couleur = new string('x', 51), Taille = new string('x', 51),
            Poids = -1m, PrixAchatHT = -1m, PrixVenteHT = -1m, PrixVenteTTC = -1m,
            TauxTVA = 150m, StockMin = -1, StockMax = -5
        };

        var champsSignales = ArticleValidation.Valider(invalide).Select(e => e.Champ).Distinct().ToList();

        foreach (var champ in champsSignales)
            Assert.Contains(champ, ChampsAvecErreur);
        // CodeBarre et CategoryId ne viennent pas de Valider (unicité, existence) mais du service : ils sont listés à la main.
        Assert.Contains(nameof(Article.CodeBarre), ChampsAvecErreur);
        Assert.Contains(nameof(Article.CategoryId), ChampsAvecErreur);
    }

    [Theory]
    [MemberData(nameof(Champs))]
    public void La_fiche_Edit_a_un_emplacement_d_erreur_pour_chaque_champ(string champ)
    {
        var vue = Lire("Edit");

        Assert.Contains($"asp-validation-for=\"Article.{champ}\"", vue);
    }

    [Theory]
    [MemberData(nameof(Champs))]
    public void La_page_Create_affiche_les_erreurs_de_chaque_champ_par_le_resume_ou_un_emplacement(string champ)
    {
        var vue = Lire("Create");

        var aResume = vue.Contains("asp-validation-summary=\"All\"");
        var aEmplacement = vue.Contains($"asp-validation-for=\"Article.{champ}\"");
        Assert.True(aResume || aEmplacement, $"Aucun emplacement d'erreur pour {champ} dans Create.cshtml");
    }

    public static IEnumerable<object[]> Champs() => ChampsAvecErreur.Select(c => new object[] { c });
}
