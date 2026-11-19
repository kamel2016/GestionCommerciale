using System.Text.RegularExpressions;

namespace GestionCommerciale.Tests;

/// <summary>S4 étape 9 : les montants s'affichent en DT (dinar tunisien) et non en euro. Ne concerne que l'affichage (vues, libellés, scripts).</summary>
public class AffichageDeviseTests
{
    private static string RacineProjet()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "GestionCommerciale.sln"))) dir = Path.GetDirectoryName(dir);
        return Path.Combine(dir ?? throw new InvalidOperationException("Racine introuvable"), "GestionCommerciale");
    }

    private static string Lire(string chemin) => Regex.Replace(File.ReadAllText(chemin), @"@\*.*?\*@", string.Empty, RegexOptions.Singleline);

    [Fact]
    public void Les_prix_de_la_fiche_article_sont_affiches_en_DT()
    {
        var details = Lire(Path.Combine(AppContext.BaseDirectory, "Pages", "Articles", "Details.cshtml"));

        Assert.Contains("@supplier.PrixGrosHT.ToString(\"N2\")</strong> DT", details);
        Assert.Contains("@supplier.PrixDetailHT.ToString(\"N2\") DT", details);
        Assert.DoesNotContain("€", details);
    }

    [Fact]
    public void Aucune_vue_ni_aucun_script_n_affiche_le_symbole_euro()
    {
        var racine = RacineProjet();
        var fichiers = Directory.EnumerateFiles(Path.Combine(racine, "Pages"), "*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(racine, "wwwroot", "js"), "*.js", SearchOption.AllDirectories));

        // Exception connue et volontaire : Fournisseurs/Details.cshtml:35 affiche « Adresse € » (copie fautive de la page Client,
        // sans rapport avec un montant) ; elle relève du sprint S5 (Fournisseurs).
        var fautifs = fichiers.Where(f => !f.EndsWith(Path.Combine("Fournisseurs", "Details.cshtml")))
            .Where(f => File.ReadAllText(f).Contains('€'))
            .Select(f => Path.GetRelativePath(racine, f)).ToList();

        Assert.True(fautifs.Count == 0, "Symbole € encore affiché dans : " + string.Join(", ", fautifs));
    }

    [Fact]
    public void Le_script_de_details_des_receptions_lit_le_meme_suffixe_que_celui_affiche_par_la_vue()
    {
        var racine = RacineProjet();
        var js = File.ReadAllText(Path.Combine(racine, "wwwroot", "js", "goods-receipt-details.js"));
        var vue = File.ReadAllText(Path.Combine(racine, "Pages", "GoodsReceipt", "Details.cshtml"));

        Assert.Contains("replace(' DT', '')", js);                                 // le script retire le suffixe pour relire le montant
        Assert.Contains("LineAmountIncludingTax.ToString(\"N2\") DT", vue);       // la vue affiche ce suffixe
    }
}
