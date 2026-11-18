using GestionCommerciale.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

/// <summary>S4 étape 3 : ArticleService.ModifierAsync (mêmes règles que la création, l'article lui-même est exclu des unicités).</summary>
public class ArticleServiceModificationTests : ArticleTestBase
{
    // ------------------------------------------------------------------ cas nominal et champs protégés

    [Fact]
    public async Task Une_modification_valide_copie_les_champs_du_formulaire_et_date_la_modification()
    {
        var categorieId = await InsererCategorieAsync();
        var existant = await InsererAsync("ANCIENNE");
        var saisie = Saisie("NOUVELLE", "Nouveau libellé");
        saisie.CodeBarre = "999"; saisie.CategoryId = categorieId; saisie.Unite = "Kg";
        saisie.PrixAchatHT = 10m; saisie.PrixVenteHT = 12m; saisie.PrixVenteTTC = 14.28m; saisie.TauxTVA = 7m; saisie.Fodec = true;
        saisie.StockMin = 3; saisie.StockMax = 30; saisie.EstActif = false;
        saisie.Description = "Desc"; saisie.Marque = "M"; saisie.Modele = "Mo"; saisie.Couleur = "Rouge"; saisie.Taille = "L"; saisie.Poids = 1.5m;

        var avant = DateTime.Now.AddSeconds(-5);
        var resultat = await Service.ModifierAsync(existant.ID, saisie);

        Assert.True(resultat.Succes);
        Assert.Equal("NOUVELLE", resultat.Reference);
        var e = (await LireAsync(existant.ID))!;
        Assert.Equal("NOUVELLE", e.Reference);
        Assert.Equal("Nouveau libellé", e.Libelle);
        Assert.Equal("999", e.CodeBarre);
        Assert.Equal(categorieId, e.CategoryId);
        Assert.Equal("Kg", e.Unite);
        Assert.Equal((10m, 12m, 14.28m, 7m), (e.PrixAchatHT, e.PrixVenteHT, e.PrixVenteTTC, e.TauxTVA));
        Assert.True(e.Fodec);
        Assert.Equal((3, 30, false), (e.StockMin, e.StockMax, e.EstActif));
        Assert.Equal(("Desc", "M", "Mo", "Rouge", "L", 1.5m), (e.Description, e.Marque, e.Modele, e.Couleur, e.Taille, e.Poids));
        Assert.True(e.DateModification >= avant);
    }

    [Fact]
    public async Task Le_stock_le_CMUP_et_les_champs_hors_formulaire_ne_changent_jamais()
    {
        var existant = await InsererAsync("PROT", stock: 25);
        Db.ChangeTracker.Clear();
        var original = await Db.Articles.SingleAsync(a => a.ID == existant.ID);
        original.CMUP = 4.2m; original.GereNumeroSerie = true; original.PrixAchatDevise = 3m;
        await Db.SaveChangesAsync();
        var dateCreation = original.DateCreation;
        Db.ChangeTracker.Clear();

        var saisie = Saisie("PROT", "Libellé modifié");
        saisie.Quantite = 999; saisie.CMUP = 99m; saisie.GereNumeroSerie = false; saisie.PrixAchatDevise = 77m;
        saisie.DateCreation = new DateTime(2000, 1, 1); saisie.FournisseurId = 555; saisie.RowVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };

        var resultat = await Service.ModifierAsync(existant.ID, saisie);

        Assert.True(resultat.Succes);
        var e = (await LireAsync(existant.ID))!;
        Assert.Equal("Libellé modifié", e.Libelle);
        Assert.Equal(25, e.Quantite);
        Assert.Equal(4.2m, e.CMUP);
        Assert.True(e.GereNumeroSerie);
        Assert.Equal(3m, e.PrixAchatDevise);
        Assert.Null(e.FournisseurId);
        Assert.Equal(dateCreation, e.DateCreation);
    }

    [Fact]
    public async Task Un_article_introuvable_est_signale()
    {
        var resultat = await Service.ModifierAsync(9999, Saisie());

        Assert.True(resultat.Introuvable);
        Assert.False(resultat.Succes);
    }

    // ------------------------------------------------------------------ nettoyage, champs obligatoires, longueurs

    [Fact]
    public async Task N1_nettoyage_a_la_modification()
    {
        var existant = await InsererAsync("N1");
        var saisie = Saisie("  N1-BIS  ", "  Libellé  ");
        saisie.CodeBarre = "   "; saisie.Marque = "  Dell "; saisie.Description = "";

        var resultat = await Service.ModifierAsync(existant.ID, saisie);

        Assert.True(resultat.Succes);
        var e = (await LireAsync(existant.ID))!;
        Assert.Equal("N1-BIS", e.Reference);
        Assert.Equal("Libellé", e.Libelle);
        Assert.Null(e.CodeBarre);
        Assert.Equal("Dell", e.Marque);
        Assert.Null(e.Description);
    }

    [Theory]
    [InlineData("Reference", "La référence est obligatoire.")]
    [InlineData("Libelle", "Le libellé est obligatoire.")]
    public async Task Reference_et_libelle_vides_sont_refuses_et_rien_n_est_modifie(string champ, string message)
    {
        var existant = await InsererAsync("VIDE");
        var saisie = Saisie();
        typeof(Article).GetProperty(champ)!.SetValue(saisie, "   ");

        var resultat = await Service.ModifierAsync(existant.ID, saisie);

        Assert.False(resultat.Succes);
        Assert.Equal(message, Assert.Single(Messages(resultat, champ)));
        var e = (await LireAsync(existant.ID))!;
        Assert.Equal("VIDE", e.Reference);
        Assert.Equal("Existant VIDE", e.Libelle);
        Assert.Null(e.DateModification);
    }

    [Fact]
    public async Task Une_reference_trop_longue_est_refusee()
    {
        var existant = await InsererAsync("LONG");

        var resultat = await Service.ModifierAsync(existant.ID, Saisie(new string('x', 51)));

        Assert.Equal("La référence ne doit pas dépasser 50 caractères.", Assert.Single(Messages(resultat, "Reference")));
    }

    // ------------------------------------------------------------------ N2 / N3 : l'article lui-même est exclu

    [Theory]
    [InlineData("AUTRE")]
    [InlineData("autre")]
    [InlineData("  Autre ")]
    public async Task N2_la_reference_d_un_AUTRE_article_est_refusee_sans_tenir_compte_de_la_casse(string reference)
    {
        await InsererAsync("AUTRE");
        var moi = await InsererAsync("MOI");

        var resultat = await Service.ModifierAsync(moi.ID, Saisie(reference));

        Assert.False(resultat.Succes);
        Assert.Equal("Cette référence existe déjà.", Assert.Single(Messages(resultat, "Reference")));
        Assert.Equal("MOI", (await LireAsync(moi.ID))!.Reference);
    }

    [Theory]
    [InlineData("MOI")]
    [InlineData("moi")]   // changer seulement la casse de SA propre référence reste possible
    public async Task N2_garder_sa_propre_reference_n_est_pas_un_doublon(string reference)
    {
        var moi = await InsererAsync("MOI");

        var resultat = await Service.ModifierAsync(moi.ID, Saisie(reference));

        Assert.True(resultat.Succes);
        Assert.Equal(reference, (await LireAsync(moi.ID))!.Reference);
    }

    [Fact]
    public async Task N3_le_code_barres_d_un_autre_article_est_refuse_mais_pas_le_sien()
    {
        await InsererAsync("A1", codeBarre: "111");
        var moi = await InsererAsync("A2", codeBarre: "222");

        var refuse = Saisie("A2"); refuse.CodeBarre = " 111 ";
        var accepte = Saisie("A2"); accepte.CodeBarre = "222";

        var r1 = await Service.ModifierAsync(moi.ID, refuse);
        var r2 = await Service.ModifierAsync(moi.ID, accepte);

        Assert.False(r1.Succes);
        Assert.Equal("Ce code-barres existe déjà.", Assert.Single(Messages(r1, "CodeBarre")));
        Assert.True(r2.Succes);
    }

    [Fact]
    public async Task N3_retirer_son_code_barres_est_possible_et_plusieurs_articles_peuvent_en_etre_sans()
    {
        await InsererAsync("SANS1");
        var moi = await InsererAsync("AVEC", codeBarre: "333");
        var saisie = Saisie("AVEC"); saisie.CodeBarre = "  ";

        Assert.True((await Service.ModifierAsync(moi.ID, saisie)).Succes);
        Assert.Null((await LireAsync(moi.ID))!.CodeBarre);
    }

    // ------------------------------------------------------------------ N4 / N5 / N6

    [Theory]
    [InlineData("-1", "Le taux de TVA doit être compris entre 0 et 100.")]
    [InlineData("100.5", "Le taux de TVA doit être compris entre 0 et 100.")]
    [InlineData("19.125", "Le taux de TVA accepte au plus 2 décimales.")]
    public async Task N4_tva_invalide_a_la_modification(string tva, string message)
    {
        var existant = await InsererAsync("TVA");
        var saisie = Saisie("TVA");
        saisie.TauxTVA = decimal.Parse(tva, System.Globalization.CultureInfo.InvariantCulture);

        var resultat = await Service.ModifierAsync(existant.ID, saisie);

        Assert.Equal(message, Assert.Single(Messages(resultat, "TauxTVA")));
        Assert.Equal(19m, (await LireAsync(existant.ID))!.TauxTVA);   // rien n'a été modifié (19 = valeur par défaut de l'article inséré)
    }

    [Fact]
    public async Task N4_tva_valide_a_la_modification()
    {
        var existant = await InsererAsync("TVA2");
        var saisie = Saisie("TVA2"); saisie.TauxTVA = 13m;

        Assert.True((await Service.ModifierAsync(existant.ID, saisie)).Succes);
        Assert.Equal(13m, (await LireAsync(existant.ID))!.TauxTVA);
    }

    [Fact]
    public async Task N5_le_controle_stock_max_superieur_ou_egal_au_min_est_dans_le_service()
    {
        // Ce contrôle existait dans la page Edit (Edit.cshtml.cs:92, « supérieur ») : il est déplacé ici, avec
        // l'égalité acceptée (max = min, et max = 0 avec min = 0).
        var existant = await InsererAsync("STK");

        var refuse = Saisie("STK"); refuse.StockMin = 10; refuse.StockMax = 9;
        var egalite = Saisie("STK"); egalite.StockMin = 10; egalite.StockMax = 10;
        var zero = Saisie("STK"); zero.StockMin = 0; zero.StockMax = 0;
        var negatif = Saisie("STK"); negatif.StockMin = -1;

        var r1 = await Service.ModifierAsync(existant.ID, refuse);
        Assert.Equal("Le stock maximum doit être supérieur ou égal au stock minimum.", Assert.Single(Messages(r1, "StockMax")));
        Assert.True((await Service.ModifierAsync(existant.ID, egalite)).Succes);
        Assert.True((await Service.ModifierAsync(existant.ID, zero)).Succes);
        Assert.Equal("Le stock minimum doit être positif ou nul.", Assert.Single(Messages(await Service.ModifierAsync(existant.ID, negatif), "StockMin")));
    }

    [Fact]
    public async Task N6_categorie_inconnue_refusee_et_categorie_facultative()
    {
        var categorieId = await InsererCategorieAsync();
        var existant = await InsererAsync("CAT", categorieId: categorieId);

        var inconnue = Saisie("CAT"); inconnue.CategoryId = 9999;
        var r1 = await Service.ModifierAsync(existant.ID, inconnue);
        Assert.Equal("Catégorie introuvable.", Assert.Single(Messages(r1, "CategoryId")));
        Assert.Equal(categorieId, (await LireAsync(existant.ID))!.CategoryId);   // inchangée

        var sans = Saisie("CAT"); sans.CategoryId = null;                        // facultative : retirer la catégorie est permis
        Assert.True((await Service.ModifierAsync(existant.ID, sans)).Succes);
        Assert.Null((await LireAsync(existant.ID))!.CategoryId);
    }

    // ------------------------------------------------------------------ décision C

    [Theory]
    [InlineData(100, 19, 50)]
    [InlineData(100, 19, 0)]
    [InlineData(100, 19, 400.5)]
    public async Task Decision_C_un_TTC_incoherent_avec_le_HT_est_accepte_tel_quel_a_la_modification(decimal ht, decimal tva, decimal ttc)
    {
        var existant = await InsererAsync("TTC");
        var saisie = Saisie("TTC"); saisie.PrixVenteHT = ht; saisie.TauxTVA = tva; saisie.PrixVenteTTC = ttc;

        Assert.True((await Service.ModifierAsync(existant.ID, saisie)).Succes);

        var e = (await LireAsync(existant.ID))!;
        Assert.Equal(ht, e.PrixVenteHT);
        Assert.Equal(ttc, e.PrixVenteTTC);
    }

    // ------------------------------------------------------------------ N8 : courses

    [Fact]
    public async Task N8_un_doublon_de_reference_cree_entre_le_controle_et_l_enregistrement_donne_une_erreur_sur_le_champ()
    {
        var moi = await InsererAsync("MOI");
        Interception.Action = async () =>
        {
            using var autre = AutreSession();
            autre.Articles.Add(new Article { Reference = "PRIS", Libelle = "Arrivé en premier" });
            await autre.SaveChangesAsync();
        };

        var resultat = await Service.ModifierAsync(moi.ID, Saisie("PRIS"));   // ne doit PAS lever d'exception

        Assert.False(resultat.Succes);
        Assert.Equal("Cette référence existe déjà.", Assert.Single(Messages(resultat, "Reference")));
        Assert.Equal("MOI", (await LireAsync(moi.ID))!.Reference);              // rien n'a été modifié
    }

    [Fact]
    public async Task N6_une_categorie_supprimee_entre_le_controle_et_l_enregistrement_donne_une_erreur_sur_le_champ()
    {
        var categorieId = await InsererCategorieAsync();
        var moi = await InsererAsync("MOI");
        Interception.Action = async () =>
        {
            using var autre = AutreSession();
            autre.Categories.Remove(await autre.Categories.SingleAsync(c => c.Id == categorieId));
            await autre.SaveChangesAsync();
        };
        var saisie = Saisie("MOI"); saisie.CategoryId = categorieId;

        var resultat = await Service.ModifierAsync(moi.ID, saisie);             // ne doit PAS lever d'exception

        Assert.False(resultat.Succes);
        Assert.Equal("Catégorie introuvable.", Assert.Single(Messages(resultat, "CategoryId")));
    }
}
