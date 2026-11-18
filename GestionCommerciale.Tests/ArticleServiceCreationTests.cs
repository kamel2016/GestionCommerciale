using GestionCommerciale.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

/// <summary>S4 étape 3 : ArticleService.CreerAsync (règles N1 à N8, décisions C et D).</summary>
public class ArticleServiceCreationTests : ArticleTestBase
{
    // ------------------------------------------------------------------ cas nominal

    [Fact]
    public async Task Une_creation_valide_enregistre_l_article_et_renseigne_son_Id()
    {
        var saisie = Saisie("REF-OK", "Câble coaxial");
        saisie.PrixAchatHT = 10m; saisie.PrixVenteHT = 15m; saisie.PrixVenteTTC = 17.85m;
        saisie.StockMin = 2; saisie.StockMax = 50;

        var resultat = await Service.CreerAsync(saisie);

        Assert.True(resultat.Succes);
        Assert.Equal("REF-OK", resultat.Reference);
        Assert.True(saisie.ID > 0);
        var enregistre = await LireAsync(saisie.ID);
        Assert.NotNull(enregistre);
        Assert.Equal("Câble coaxial", enregistre!.Libelle);
        Assert.Equal(10m, enregistre.PrixAchatHT);
        Assert.Equal(2, enregistre.StockMin);
        Assert.Equal(50, enregistre.StockMax);
        Assert.True(enregistre.EstActif);
    }

    // ------------------------------------------------------------------ N1 : nettoyage

    [Fact]
    public async Task N1_les_espaces_en_debut_et_en_fin_sont_retires()
    {
        var saisie = Saisie("  REF-ESP  ", "  Libellé avec espaces  ");
        saisie.Unite = "  Kg ";
        saisie.CodeBarre = "  123456  ";
        saisie.Marque = " Dell ";

        var resultat = await Service.CreerAsync(saisie);

        Assert.True(resultat.Succes);
        var enregistre = (await LireAsync(saisie.ID))!;
        Assert.Equal("REF-ESP", enregistre.Reference);
        Assert.Equal("Libellé avec espaces", enregistre.Libelle);
        Assert.Equal("Kg", enregistre.Unite);
        Assert.Equal("123456", enregistre.CodeBarre);
        Assert.Equal("Dell", enregistre.Marque);
    }

    [Fact]
    public async Task N1_un_champ_facultatif_vide_ou_blanc_devient_NULL()
    {
        var saisie = Saisie();
        saisie.CodeBarre = "   ";
        saisie.Marque = "";
        saisie.Modele = " ";
        saisie.Couleur = "\t";
        saisie.Taille = "";
        saisie.Description = "   ";

        var resultat = await Service.CreerAsync(saisie);

        Assert.True(resultat.Succes);
        var enregistre = (await LireAsync(saisie.ID))!;
        Assert.Null(enregistre.CodeBarre);
        Assert.Null(enregistre.Marque);
        Assert.Null(enregistre.Modele);
        Assert.Null(enregistre.Couleur);
        Assert.Null(enregistre.Taille);
        Assert.Null(enregistre.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task N1_la_reference_vide_est_une_erreur_jamais_un_NULL(string? reference)
    {
        var saisie = Saisie();
        saisie.Reference = reference!;

        var resultat = await Service.CreerAsync(saisie);

        Assert.False(resultat.Succes);
        Assert.Equal("La référence est obligatoire.", Assert.Single(Messages(resultat, "Reference")));
        Assert.Equal(0, await CompterArticlesAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task N1_le_libelle_vide_est_une_erreur_jamais_un_NULL(string? libelle)
    {
        var saisie = Saisie();
        saisie.Libelle = libelle!;

        var resultat = await Service.CreerAsync(saisie);

        Assert.False(resultat.Succes);
        Assert.Equal("Le libellé est obligatoire.", Assert.Single(Messages(resultat, "Libelle")));
        Assert.Equal(0, await CompterArticlesAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task L_unite_vide_est_une_erreur(string? unite)
    {
        // Comportement actuel conservé : MVC refuse déjà une unité vide (champ non facultatif).
        var saisie = Saisie();
        saisie.Unite = unite!;

        var resultat = await Service.CreerAsync(saisie);

        Assert.False(resultat.Succes);
        Assert.Equal("L'unité est obligatoire.", Assert.Single(Messages(resultat, "Unite")));
    }

    // ------------------------------------------------------------------ longueurs (règles existantes E1 à E6)

    [Theory]
    [InlineData("Reference", 51, "La référence ne doit pas dépasser 50 caractères.")]
    [InlineData("Libelle", 201, "Le libellé ne doit pas dépasser 200 caractères.")]
    [InlineData("CodeBarre", 51, "Le code-barres ne doit pas dépasser 50 caractères.")]
    [InlineData("Unite", 21, "L'unité ne doit pas dépasser 20 caractères.")]
    [InlineData("Marque", 101, "La marque ne doit pas dépasser 100 caractères.")]
    [InlineData("Modele", 101, "Le modèle ne doit pas dépasser 100 caractères.")]
    [InlineData("Couleur", 51, "La couleur ne doit pas dépasser 50 caractères.")]
    [InlineData("Taille", 51, "La taille ne doit pas dépasser 50 caractères.")]
    public async Task Une_valeur_trop_longue_est_refusee(string champ, int longueur, string messageAttendu)
    {
        var saisie = Saisie();
        typeof(Article).GetProperty(champ)!.SetValue(saisie, new string('x', longueur));

        var resultat = await Service.CreerAsync(saisie);

        Assert.False(resultat.Succes);
        Assert.Equal(messageAttendu, Assert.Single(Messages(resultat, champ)));
        Assert.Equal(0, await CompterArticlesAsync());
    }

    [Fact]
    public async Task Les_longueurs_maximales_exactes_sont_acceptees()
    {
        var saisie = Saisie(new string('r', 50), new string('l', 200));
        saisie.CodeBarre = new string('c', 50);
        saisie.Unite = new string('u', 20);

        Assert.True((await Service.CreerAsync(saisie)).Succes);
    }

    [Fact]
    public async Task Les_prix_et_le_poids_negatifs_sont_refuses()
    {
        var saisie = Saisie();
        saisie.PrixAchatHT = -1m; saisie.PrixVenteHT = -1m; saisie.PrixVenteTTC = -1m; saisie.Poids = -0.5m;

        var resultat = await Service.CreerAsync(saisie);

        Assert.False(resultat.Succes);
        Assert.Single(Messages(resultat, "PrixAchatHT"));
        Assert.Single(Messages(resultat, "PrixVenteHT"));
        Assert.Single(Messages(resultat, "PrixVenteTTC"));
        Assert.Single(Messages(resultat, "Poids"));
    }

    // ------------------------------------------------------------------ N2 : unicité de la référence

    [Theory]
    [InlineData("ABC")]
    [InlineData("abc")]     // casse différente
    [InlineData("  ABC  ")] // espaces autour (N1 puis N2)
    public async Task N2_une_reference_deja_utilisee_est_refusee_sans_tenir_compte_de_la_casse(string reference)
    {
        await InsererAsync("ABC");

        var resultat = await Service.CreerAsync(Saisie(reference));

        Assert.False(resultat.Succes);
        Assert.Equal("Cette référence existe déjà.", Assert.Single(Messages(resultat, "Reference")));
        Assert.Equal(1, await CompterArticlesAsync());
    }

    // ------------------------------------------------------------------ N3 : unicité du code-barres

    [Fact]
    public async Task N3_un_code_barres_deja_utilise_est_refuse()
    {
        await InsererAsync("A1", codeBarre: "123456");
        var saisie = Saisie("A2");
        saisie.CodeBarre = " 123456 "; // espaces ignorés

        var resultat = await Service.CreerAsync(saisie);

        Assert.False(resultat.Succes);
        Assert.Equal("Ce code-barres existe déjà.", Assert.Single(Messages(resultat, "CodeBarre")));
        Assert.Equal(1, await CompterArticlesAsync());
    }

    [Fact]
    public async Task N3_plusieurs_articles_sans_code_barres_restent_possibles()
    {
        var a = Saisie("S1"); a.CodeBarre = null;
        var b = Saisie("S2"); b.CodeBarre = "";
        var c = Saisie("S3"); c.CodeBarre = "   ";

        Assert.True((await Service.CreerAsync(a)).Succes);
        Assert.True((await Service.CreerAsync(b)).Succes);
        Assert.True((await Service.CreerAsync(c)).Succes);
        Assert.Equal(3, await CompterArticlesAsync());
    }

    // ------------------------------------------------------------------ N4 : TVA

    [Theory]
    [InlineData("0")]
    [InlineData("7")]
    [InlineData("7.5")]
    [InlineData("13.00")]
    [InlineData("19")]
    [InlineData("100")]
    [InlineData("99.99")]
    public async Task N4_une_tva_de_0_a_100_avec_2_decimales_au_plus_est_acceptee(string tva)
    {
        var saisie = Saisie();
        saisie.TauxTVA = decimal.Parse(tva, System.Globalization.CultureInfo.InvariantCulture);

        Assert.True((await Service.CreerAsync(saisie)).Succes);
    }

    [Theory]
    [InlineData("-0.01", "Le taux de TVA doit être compris entre 0 et 100.")]
    [InlineData("-19", "Le taux de TVA doit être compris entre 0 et 100.")]
    [InlineData("100.01", "Le taux de TVA doit être compris entre 0 et 100.")]
    [InlineData("1000", "Le taux de TVA doit être compris entre 0 et 100.")]
    [InlineData("19.999", "Le taux de TVA accepte au plus 2 décimales.")]
    [InlineData("0.001", "Le taux de TVA accepte au plus 2 décimales.")]
    public async Task N4_une_tva_hors_limites_ou_a_plus_de_2_decimales_est_refusee(string tva, string messageAttendu)
    {
        var saisie = Saisie();
        saisie.TauxTVA = decimal.Parse(tva, System.Globalization.CultureInfo.InvariantCulture);

        var resultat = await Service.CreerAsync(saisie);

        Assert.False(resultat.Succes);
        Assert.Equal(messageAttendu, Assert.Single(Messages(resultat, "TauxTVA")));
        Assert.Equal(0, await CompterArticlesAsync());
    }

    // ------------------------------------------------------------------ N5 : stocks

    [Fact]
    public async Task N5_un_stock_minimum_negatif_est_refuse()
    {
        var saisie = Saisie(); saisie.StockMin = -1;

        var resultat = await Service.CreerAsync(saisie);

        Assert.Equal("Le stock minimum doit être positif ou nul.", Assert.Single(Messages(resultat, "StockMin")));
    }

    [Fact]
    public async Task N5_un_stock_maximum_negatif_est_refuse()
    {
        var saisie = Saisie(); saisie.StockMin = 0; saisie.StockMax = -1;

        var resultat = await Service.CreerAsync(saisie);

        Assert.Equal("Le stock maximum doit être positif ou nul.", Assert.Single(Messages(resultat, "StockMax")));
    }

    [Fact]
    public async Task N5_un_stock_maximum_inferieur_au_minimum_est_refuse_aussi_a_la_creation()
    {
        var saisie = Saisie(); saisie.StockMin = 10; saisie.StockMax = 5;

        var resultat = await Service.CreerAsync(saisie);

        Assert.False(resultat.Succes);
        Assert.Equal("Le stock maximum doit être supérieur ou égal au stock minimum.", Assert.Single(Messages(resultat, "StockMax")));
    }

    [Theory]
    [InlineData(0, 0)]     // max = 0 avec min = 0 est valide
    [InlineData(5, 5)]     // égalité acceptée
    [InlineData(5, 100)]
    [InlineData(0, null)]  // pas de maximum
    public async Task N5_les_combinaisons_valides_de_stock_sont_acceptees(int min, int? max)
    {
        var saisie = Saisie(); saisie.StockMin = min; saisie.StockMax = max;

        Assert.True((await Service.CreerAsync(saisie)).Succes);
    }

    // ------------------------------------------------------------------ N6 : catégorie

    [Fact]
    public async Task N6_une_categorie_inconnue_est_refusee_avec_un_message()
    {
        var saisie = Saisie(); saisie.CategoryId = 9999;

        var resultat = await Service.CreerAsync(saisie);

        Assert.False(resultat.Succes);
        Assert.Equal("Catégorie introuvable.", Assert.Single(Messages(resultat, "CategoryId")));
        Assert.Equal(0, await CompterArticlesAsync());
    }

    [Fact]
    public async Task N6_une_categorie_existante_est_acceptee_et_la_categorie_reste_facultative()
    {
        var categorieId = await InsererCategorieAsync();
        var avec = Saisie("AVEC"); avec.CategoryId = categorieId;
        var sans = Saisie("SANS"); sans.CategoryId = null;

        Assert.True((await Service.CreerAsync(avec)).Succes);
        Assert.True((await Service.CreerAsync(sans)).Succes);
        Assert.Equal(categorieId, (await LireAsync(avec.ID))!.CategoryId);
        Assert.Null((await LireAsync(sans.ID))!.CategoryId);
    }

    // ------------------------------------------------------------------ N7 : valeurs imposées

    [Fact]
    public async Task N7_stock_CMUP_Id_version_et_date_de_creation_de_la_saisie_sont_ignores()
    {
        var saisie = Saisie("FORGE");
        saisie.ID = 777;
        saisie.Quantite = 50;
        saisie.CMUP = 9.5m;
        saisie.RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        saisie.DateCreation = new DateTime(2000, 1, 1);
        saisie.DateModification = new DateTime(2001, 1, 1);
        saisie.GereNumeroSerie = true;
        saisie.FournisseurId = 123;
        saisie.PrixAchatDevise = 5m;

        var avant = DateTime.Now.AddSeconds(-5);
        var resultat = await Service.CreerAsync(saisie);

        Assert.True(resultat.Succes);
        var enregistre = (await LireAsync(saisie.ID))!;
        Assert.NotEqual(777, enregistre.ID);
        Assert.Equal(0, enregistre.Quantite);
        Assert.Equal(0m, enregistre.CMUP);
        Assert.True(enregistre.DateCreation >= avant);          // date du jour, pas celle de la saisie
        Assert.Null(enregistre.DateModification);
        Assert.False(enregistre.GereNumeroSerie);
        Assert.Null(enregistre.FournisseurId);
        Assert.Null(enregistre.PrixAchatDevise);
        Assert.Empty(await Db.MouvementsStock.ToListAsync());   // aucun mouvement de stock créé
    }

    // ------------------------------------------------------------------ décisions C et D : prix

    [Fact]
    public async Task Decision_D_la_creation_accepte_les_trois_prix()
    {
        var saisie = Saisie();
        saisie.PrixAchatHT = 80.5m; saisie.PrixVenteHT = 100m; saisie.PrixVenteTTC = 119m;

        Assert.True((await Service.CreerAsync(saisie)).Succes);

        var enregistre = (await LireAsync(saisie.ID))!;
        Assert.Equal(80.5m, enregistre.PrixAchatHT);
        Assert.Equal(100m, enregistre.PrixVenteHT);
        Assert.Equal(119m, enregistre.PrixVenteTTC);
    }

    [Theory]
    [InlineData(100, 19, 50)]    // TTC bien inférieur au HT
    [InlineData(100, 19, 0)]     // TTC à zéro
    [InlineData(100, 19, 400.5)] // TTC très supérieur
    [InlineData(0, 19, 123.456)] // HT nul, TTC renseigné
    public async Task Decision_C_un_TTC_incoherent_avec_le_HT_est_accepte_tel_quel(decimal ht, decimal tva, decimal ttc)
    {
        var saisie = Saisie();
        saisie.PrixVenteHT = ht; saisie.TauxTVA = tva; saisie.PrixVenteTTC = ttc;

        var resultat = await Service.CreerAsync(saisie);

        Assert.True(resultat.Succes);
        var enregistre = (await LireAsync(saisie.ID))!;
        Assert.Equal(ht, enregistre.PrixVenteHT);
        Assert.Equal(ttc, enregistre.PrixVenteTTC);   // jamais recalculé ni corrigé
    }

    // ------------------------------------------------------------------ cumul d'erreurs

    [Fact]
    public async Task Toutes_les_erreurs_sont_remontees_ensemble()
    {
        var saisie = new Article { Reference = "", Libelle = "", Unite = "", TauxTVA = 150m, StockMin = -1, CategoryId = 9999 };

        var resultat = await Service.CreerAsync(saisie);

        Assert.False(resultat.Succes);
        foreach (var champ in new[] { "Reference", "Libelle", "Unite", "TauxTVA", "StockMin", "CategoryId" })
            Assert.Single(Messages(resultat, champ));
        Assert.Equal(0, await CompterArticlesAsync());
    }

    // ------------------------------------------------------------------ N8 : courses (vraies, via un 2e contexte)

    [Fact]
    public async Task N8_un_doublon_de_reference_cree_entre_le_controle_et_l_enregistrement_donne_une_erreur_sur_le_champ()
    {
        // Le contrôle du service passe (la référence est libre), puis une autre session insère la même référence
        // AVANT que le service n'enregistre : c'est l'index unique de la base qui refuse.
        Interception.Action = async () =>
        {
            using var autre = AutreSession();
            autre.Articles.Add(new Article { Reference = "COURSE", Libelle = "Arrivé en premier" });
            await autre.SaveChangesAsync();
        };

        var resultat = await Service.CreerAsync(Saisie("COURSE"));   // ne doit PAS lever d'exception

        Assert.False(resultat.Succes);
        Assert.Equal("Cette référence existe déjà.", Assert.Single(Messages(resultat, "Reference")));
        Assert.Equal(1, await CompterArticlesAsync());               // seul l'article arrivé en premier existe
    }

    [Fact]
    public async Task N6_une_categorie_supprimee_entre_le_controle_et_l_enregistrement_donne_une_erreur_sur_le_champ()
    {
        var categorieId = await InsererCategorieAsync();
        Interception.Action = async () =>
        {
            using var autre = AutreSession();
            var categorie = await autre.Categories.SingleAsync(c => c.Id == categorieId);
            autre.Categories.Remove(categorie);
            await autre.SaveChangesAsync();
        };
        var saisie = Saisie("CAT-COURSE"); saisie.CategoryId = categorieId;

        var resultat = await Service.CreerAsync(saisie);              // ne doit PAS lever d'exception

        Assert.False(resultat.Succes);
        Assert.Equal("Catégorie introuvable.", Assert.Single(Messages(resultat, "CategoryId")));
        Assert.Equal(0, await CompterArticlesAsync());
    }
}
