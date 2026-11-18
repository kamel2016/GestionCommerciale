using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using GestionCommerciale.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

/// <summary>
/// S4 et S5 sur un VRAI SQL Server (base jetable LocalDB, Migrate()) : vraies courses entre le contrôle du service et
/// l'enregistrement, avec un second contexte qui s'exécute juste avant le SaveChanges du premier (intercepteur).
/// Chaque cas doit renvoyer une erreur sur le BON champ, sans exception.
/// </summary>
[Trait("Category", "SqlServer")]
public class SqlServerCoursesTests : IClassFixture<BaseJetableFixture>
{
    private readonly BaseJetableFixture _base;

    public SqlServerCoursesTests(BaseJetableFixture baseJetable) => _base = baseJetable;

    private static string Unique(string prefixe) => prefixe + "-" + Guid.NewGuid().ToString("N")[..10];

    private static Article Saisie(string reference) => new()
    {
        Reference = reference, Libelle = "Course", Unite = "Pièce", TauxTVA = 19m, EstActif = true
    };

    private async Task<int> InsererAsync(string reference)
    {
        using var contexte = _base.NouveauContexte();
        var article = new Article { Reference = reference, Libelle = "Existant" };
        contexte.Articles.Add(article);
        await contexte.SaveChangesAsync();
        return article.ID;
    }

    private async Task<int> InsererCategorieAsync()
    {
        using var contexte = _base.NouveauContexte();
        var categorie = new Category { Name = Unique("Cat") };
        contexte.Categories.Add(categorie);
        await contexte.SaveChangesAsync();
        return categorie.Id;
    }

    private async Task<int> CompterAsync(string reference)
    {
        using var contexte = _base.NouveauContexte();
        return await contexte.Articles.CountAsync(a => a.Reference == reference);
    }

    private static void UneErreurSurLeChamp(ResultatArticle resultat, string champ, string message)
    {
        Assert.False(resultat.Succes);
        var erreur = Assert.Single(resultat.Erreurs);
        Assert.Equal(champ, erreur.Champ);
        Assert.Equal(message, erreur.Message);
    }

    [SqlServerFact]
    public async Task S4a_creation_doublon_de_reference_simultane_donne_une_erreur_sur_Reference()
    {
        _base.AssurerBase();
        var reference = Unique("S4A");
        var course = new ActionAvantEnregistrement();
        using (var contexte = _base.NouveauContexte(course))
        {
            course.Action = async () =>
            {
                using var autre = _base.NouveauContexte();
                autre.Articles.Add(new Article { Reference = reference, Libelle = "Arrivé en premier" });
                await autre.SaveChangesAsync();
            };

            var resultat = await new ArticleService(contexte).CreerAsync(Saisie(reference));   // ne doit pas lever d'exception

            UneErreurSurLeChamp(resultat, "Reference", "Cette référence existe déjà.");
        }
        Assert.Equal(1, await CompterAsync(reference));    // seul l'article arrivé en premier existe
    }

    [SqlServerFact]
    public async Task S4b_modification_doublon_de_reference_simultane_donne_une_erreur_sur_Reference()
    {
        _base.AssurerBase();
        var maReference = Unique("S4B-MOI");
        var prise = Unique("S4B-PRIS");
        var id = await InsererAsync(maReference);
        var course = new ActionAvantEnregistrement();
        using (var contexte = _base.NouveauContexte(course))
        {
            course.Action = async () =>
            {
                using var autre = _base.NouveauContexte();
                autre.Articles.Add(new Article { Reference = prise, Libelle = "Arrivé en premier" });
                await autre.SaveChangesAsync();
            };

            var resultat = await new ArticleService(contexte).ModifierAsync(id, Saisie(prise));   // ne doit pas lever d'exception

            UneErreurSurLeChamp(resultat, "Reference", "Cette référence existe déjà.");
        }
        Assert.Equal(1, await CompterAsync(maReference));  // ma référence n'a pas changé
        Assert.Equal(1, await CompterAsync(prise));
    }

    [SqlServerFact]
    public async Task S5a_creation_categorie_supprimee_entre_le_controle_et_l_enregistrement_donne_une_erreur_sur_CategoryId()
    {
        _base.AssurerBase();
        var categorieId = await InsererCategorieAsync();
        var reference = Unique("S5A");
        var course = new ActionAvantEnregistrement();
        using (var contexte = _base.NouveauContexte(course))
        {
            course.Action = async () =>
            {
                using var autre = _base.NouveauContexte();
                autre.Categories.Remove(await autre.Categories.SingleAsync(c => c.Id == categorieId));
                await autre.SaveChangesAsync();
            };
            var saisie = Saisie(reference);
            saisie.CategoryId = categorieId;

            var resultat = await new ArticleService(contexte).CreerAsync(saisie);   // ne doit pas lever d'exception

            UneErreurSurLeChamp(resultat, "CategoryId", "Catégorie introuvable.");
        }
        Assert.Equal(0, await CompterAsync(reference));
    }

    [SqlServerFact]
    public async Task S5b_modification_categorie_supprimee_entre_le_controle_et_l_enregistrement_donne_une_erreur_sur_CategoryId()
    {
        _base.AssurerBase();
        var categorieId = await InsererCategorieAsync();
        var reference = Unique("S5B");
        var id = await InsererAsync(reference);
        var course = new ActionAvantEnregistrement();
        using (var contexte = _base.NouveauContexte(course))
        {
            course.Action = async () =>
            {
                using var autre = _base.NouveauContexte();
                autre.Categories.Remove(await autre.Categories.SingleAsync(c => c.Id == categorieId));
                await autre.SaveChangesAsync();
            };
            var saisie = Saisie(reference);
            saisie.CategoryId = categorieId;

            var resultat = await new ArticleService(contexte).ModifierAsync(id, saisie);   // ne doit pas lever d'exception

            UneErreurSurLeChamp(resultat, "CategoryId", "Catégorie introuvable.");
        }
        using var relecture = _base.NouveauContexte();
        Assert.Null((await relecture.Articles.AsNoTracking().SingleAsync(a => a.ID == id)).CategoryId);   // inchangé
    }
}
