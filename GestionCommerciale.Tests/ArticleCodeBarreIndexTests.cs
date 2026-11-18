using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using CreateArticleModel = GestionCommerciale.Pages.Articles.CreateModel;
using EditArticleModel = GestionCommerciale.Pages.Articles.EditModel;

namespace GestionCommerciale.Tests;

/// <summary>
/// S4 étape 4 : index unique filtré sur Articles.CodeBarre ([CodeBarre] IS NOT NULL) et détection de IX_Articles_CodeBarre.
/// SQLite (clés étrangères actives) : l'index vient du modèle (EnsureCreated). Le comportement sur SQL Server réel est
/// vérifié par les tests S6 (SqlServerCodeBarreTests), activés par GC_TESTS_SQLSERVER=1.
/// </summary>
public class ArticleCodeBarreIndexTests : ArticleTestBase
{
    private class FauxTempData : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private (ActionContext, TempDataDictionary) Contexte()
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        return (actionContext, new TempDataDictionary(httpContext, new FauxTempData()));
    }

    /// <summary>Insère « en premier » un article portant ce code-barres, depuis une autre session, juste avant le prochain SaveChanges.</summary>
    private void ProgrammerCourseCodeBarre(string codeBarre)
    {
        Interception.Action = async () =>
        {
            using var autre = AutreSession();
            autre.Articles.Add(new Article { Reference = "ARRIVE-AVANT-" + Guid.NewGuid().ToString("N")[..6], Libelle = "Arrivé en premier", CodeBarre = codeBarre });
            await autre.SaveChangesAsync();
        };
    }

    // ------------------------------------------------------------------ l'index lui-même (modèle)

    [Fact]
    public async Task L_index_accepte_plusieurs_articles_sans_code_barres_NULL()
    {
        Db.Articles.Add(new Article { Reference = "N1", Libelle = "a", CodeBarre = null });
        Db.Articles.Add(new Article { Reference = "N2", Libelle = "b", CodeBarre = null });
        Db.Articles.Add(new Article { Reference = "N3", Libelle = "c", CodeBarre = null });

        await Db.SaveChangesAsync();

        Assert.Equal(3, await CompterArticlesAsync());
    }

    [Fact]
    public async Task L_index_refuse_un_doublon_de_code_barres_et_la_detection_nomme_le_champ_CodeBarre()
    {
        Db.Articles.Add(new Article { Reference = "D1", Libelle = "a", CodeBarre = "123456" });
        await Db.SaveChangesAsync();
        using var autre = AutreSession();
        autre.Articles.Add(new Article { Reference = "D2", Libelle = "b", CodeBarre = "123456" });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => autre.SaveChangesAsync());

        Assert.Equal("CodeBarre", ArticleService.ChampEnViolationIndexUnique(exception));
        Assert.True(ArticleService.EstViolationIndexCodeBarre(exception));
        Assert.False(ArticleService.EstViolationIndexReference(exception));
    }

    [Fact]
    public async Task Un_doublon_de_reference_reste_reconnu_comme_Reference_et_pas_comme_CodeBarre()
    {
        Db.Articles.Add(new Article { Reference = "MEME", Libelle = "a" });
        await Db.SaveChangesAsync();
        using var autre = AutreSession();
        autre.Articles.Add(new Article { Reference = "MEME", Libelle = "b" });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => autre.SaveChangesAsync());

        Assert.Equal("Reference", ArticleService.ChampEnViolationIndexUnique(exception));
        Assert.False(ArticleService.EstViolationIndexCodeBarre(exception));
    }

    [Fact]
    public async Task Une_violation_de_cle_etrangere_n_est_prise_pour_aucun_index_unique()
    {
        using var autre = AutreSession();
        autre.Articles.Add(new Article { Reference = "FK", Libelle = "a", CategoryId = 9999 });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => autre.SaveChangesAsync());

        Assert.Null(ArticleService.ChampEnViolationIndexUnique(exception));
    }

    // ------------------------------------------------------------------ le service : courses sur le code-barres

    [Fact]
    public async Task Creation_doublon_de_code_barres_simultane_donne_une_erreur_sur_CodeBarre()
    {
        var saisie = Saisie("CB-CREA"); saisie.CodeBarre = "777";
        ProgrammerCourseCodeBarre("777");

        var resultat = await Service.CreerAsync(saisie);                  // ne doit pas lever d'exception

        Assert.False(resultat.Succes);
        Assert.Equal("Ce code-barres existe déjà.", Assert.Single(Messages(resultat, "CodeBarre")));
        Assert.Empty(Messages(resultat, "Reference"));
        Assert.Equal(1, await CompterArticlesAsync());                    // seul l'article arrivé en premier existe
    }

    [Fact]
    public async Task Modification_doublon_de_code_barres_simultane_donne_une_erreur_sur_CodeBarre()
    {
        var moi = await InsererAsync("CB-MOI", codeBarre: "111");
        var saisie = Saisie("CB-MOI"); saisie.CodeBarre = "888";
        ProgrammerCourseCodeBarre("888");

        var resultat = await Service.ModifierAsync(moi.ID, saisie);       // ne doit pas lever d'exception

        Assert.False(resultat.Succes);
        Assert.Equal("Ce code-barres existe déjà.", Assert.Single(Messages(resultat, "CodeBarre")));
        Assert.Equal("111", (await LireAsync(moi.ID))!.CodeBarre);        // rien n'a été modifié
    }

    // ------------------------------------------------------------------ les pages : plus d'erreur 500

    [Fact]
    public async Task La_page_Create_ne_leve_pas_d_exception_sur_un_doublon_simultane_de_code_barres()
    {
        var (actionContext, tempData) = Contexte();
        var saisie = Saisie("CB-PAGE-C"); saisie.CodeBarre = "555";
        var page = new CreateArticleModel(Db, Service, NullLogger<CreateArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext), TempData = tempData, Article = saisie
        };
        ProgrammerCourseCodeBarre("555");

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("Ce code-barres existe déjà.", Assert.Single(page.ModelState["Article.CodeBarre"]!.Errors).ErrorMessage);
    }

    [Fact]
    public async Task La_page_Edit_ne_leve_pas_d_exception_sur_un_doublon_simultane_de_code_barres()
    {
        var moi = await InsererAsync("CB-PAGE-E");
        var (actionContext, tempData) = Contexte();
        var saisie = Saisie("CB-PAGE-E"); saisie.ID = moi.ID; saisie.CodeBarre = "444";
        var page = new EditArticleModel(Db, new StockService(Db), Service, NullLogger<EditArticleModel>.Instance)
        {
            PageContext = new PageContext(actionContext), TempData = tempData, Article = saisie
        };
        ProgrammerCourseCodeBarre("444");

        var resultat = await page.OnPostAsync();

        Assert.IsType<PageResult>(resultat);
        Assert.Equal("Ce code-barres existe déjà.", Assert.Single(page.ModelState["Article.CodeBarre"]!.Errors).ErrorMessage);
    }
}
