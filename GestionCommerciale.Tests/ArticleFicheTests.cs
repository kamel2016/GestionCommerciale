using GestionCommerciale.Models;
using GestionCommerciale.Services;

namespace GestionCommerciale.Tests;

/// <summary>S4 étape 6 : ArticleService.ObtenirFicheAsync, la fiche article lit de VRAIES données.</summary>
public class ArticleFicheTests : ArticleTestBase
{
    private async Task<int> AjouterFournisseurAsync(string nom)
    {
        var fournisseur = new Fournisseur { Nom = nom };
        Db.Fournisseurs.Add(fournisseur);
        await Db.SaveChangesAsync();
        return fournisseur.Id;
    }

    [Fact]
    public async Task Un_article_inexistant_donne_null()
    {
        Assert.Null(await Service.ObtenirFicheAsync(9999));
    }

    [Fact]
    public async Task La_fiche_contient_les_vraies_donnees_de_l_article_et_de_sa_categorie()
    {
        var categorieId = await InsererCategorieAsync("Câblage");
        var saisie = Saisie("FICHE-1", "Câble coaxial");
        saisie.CodeBarre = "9876543"; saisie.CategoryId = categorieId; saisie.Marque = "Acme"; saisie.Modele = "X1";
        saisie.Couleur = "Noir"; saisie.Taille = "5 m"; saisie.Poids = 1.25m; saisie.TauxTVA = 13m;
        saisie.StockMin = 2; saisie.StockMax = 40; saisie.Description = "Câble blindé";
        Assert.True((await Service.CreerAsync(saisie)).Succes);

        var fiche = await Service.ObtenirFicheAsync(saisie.ID);

        Assert.NotNull(fiche);
        Assert.Equal("FICHE-1", fiche!.Article.Reference);
        Assert.Equal("Câble coaxial", fiche.Article.Libelle);
        Assert.Equal("9876543", fiche.Article.CodeBarre);
        Assert.Equal("Câblage", fiche.CategorieNom);
        Assert.Equal(("Acme", "X1", "Noir", "5 m", 1.25m), (fiche.Article.Marque, fiche.Article.Modele, fiche.Article.Couleur, fiche.Article.Taille, fiche.Article.Poids));
        Assert.Equal(13m, fiche.Article.TauxTVA);
        Assert.Equal((2, 40), (fiche.Article.StockMin, fiche.Article.StockMax));
    }

    [Fact]
    public async Task Un_article_sans_categorie_ni_mouvement_ni_tarif_donne_une_fiche_vide_mais_valide()
    {
        var article = await InsererAsync("VIDE");

        var fiche = await Service.ObtenirFicheAsync(article.ID);

        Assert.NotNull(fiche);
        Assert.Null(fiche!.CategorieNom);
        Assert.Empty(fiche.Tarifs);
        Assert.Empty(fiche.DerniersMouvements);
        Assert.Equal(0, fiche.NombreMouvements);
        Assert.Null(fiche.DernierMouvement);
    }

    [Fact]
    public async Task Les_tarifs_sont_ceux_de_cet_article_tries_par_nom_de_fournisseur()
    {
        var article = await InsererAsync("TARIFS");
        var autre = await InsererAsync("AUTRE");
        var zeta = await AjouterFournisseurAsync("Zeta");
        var alpha = await AjouterFournisseurAsync("Alpha");
        Db.ArticleFournisseurs.Add(new ArticleFournisseur { ArticleId = article.ID, FournisseurId = zeta, PrixGrosHT = 8m, PrixDetailHT = 12m, PrixPromoHT = null, RemiseMax = 5m });
        Db.ArticleFournisseurs.Add(new ArticleFournisseur { ArticleId = article.ID, FournisseurId = alpha, PrixGrosHT = 9m, PrixDetailHT = 14m, PrixPromoHT = 11m, RemiseMax = 2m, DernierAchat = new DateTime(2026, 10, 1) });
        Db.ArticleFournisseurs.Add(new ArticleFournisseur { ArticleId = autre.ID, FournisseurId = alpha, PrixGrosHT = 1m, PrixDetailHT = 2m });
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var fiche = await Service.ObtenirFicheAsync(article.ID);

        Assert.Equal(new[] { "Alpha", "Zeta" }, fiche!.Tarifs.Select(t => t.Fournisseur.Nom).ToArray());
        var alphaTarif = fiche.Tarifs[0];
        Assert.Equal((9m, 14m, 11m, 2m), (alphaTarif.PrixGrosHT, alphaTarif.PrixDetailHT, alphaTarif.PrixPromoHT, alphaTarif.RemiseMax));
        Assert.Equal(new DateTime(2026, 10, 1), alphaTarif.DernierAchat);
        Assert.Null(fiche.Tarifs[1].PrixPromoHT);
    }

    [Fact]
    public async Task Les_mouvements_sont_les_vrais_du_plus_recent_au_plus_ancien_et_limites_mais_comptes_en_totalite()
    {
        var article = await InsererAsync("MVT");
        var autre = await InsererAsync("MVT-AUTRE");
        var debut = new DateTime(2026, 11, 1, 8, 0, 0);
        for (var i = 0; i < 25; i++)
            Db.MouvementsStock.Add(new MouvementStock { ArticleId = article.ID, Date = debut.AddHours(i), Type = TypeMouvementStock.EntreeReception, Quantite = i + 1, Commentaire = "m" + i });
        Db.MouvementsStock.Add(new MouvementStock { ArticleId = autre.ID, Date = debut.AddDays(30), Quantite = 99 });   // autre article : jamais dans cette fiche
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var fiche = await Service.ObtenirFicheAsync(article.ID);

        Assert.Equal(ArticleService.NombreMouvementsAffiches, fiche!.DerniersMouvements.Count);
        Assert.Equal(25, fiche.NombreMouvements);                                   // total réel, pas seulement les 20 affichés
        Assert.Equal(debut.AddHours(24), fiche.DernierMouvement);
        Assert.Equal("m24", fiche.DerniersMouvements[0].Commentaire);               // le plus récent d'abord
        Assert.Equal("m5", fiche.DerniersMouvements[^1].Commentaire);               // le 20e, pas les 5 plus anciens
        Assert.All(fiche.DerniersMouvements, m => Assert.Equal(article.ID, m.ArticleId));
    }

    [Fact]
    public async Task La_fiche_ne_contient_aucune_des_donnees_inventees_de_l_ancienne_page()
    {
        var article = await InsererAsync("REEL-7");

        var fiche = await Service.ObtenirFicheAsync(article.ID);

        Assert.NotEqual("LAPTOP-DELL-XPS13", fiche!.Article.Reference);
        Assert.NotEqual("Ordinateur Portable Dell XPS 13", fiche.Article.Libelle);
        Assert.Empty(fiche.Tarifs);                 // les fournisseurs « TECH SUPPLIES SARL » et « FOURNITURES BUREAU PLUS » étaient inventés
        Assert.Empty(fiche.DerniersMouvements);     // les 4 mouvements « BL-2025-001 »... étaient inventés
    }
}
