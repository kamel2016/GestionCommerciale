using GestionCommerciale.Models;
using GestionCommerciale.Services;
using GestionCommerciale.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace GestionCommerciale.Tests;

/// <summary>
/// S6 : index unique filtré sur Articles.CodeBarre sur un VRAI SQL Server (base jetable LocalDB créée par Migrate(), donc
/// avec la migration UniciteCodeBarre). Les numéros et messages réels sont écrits dans la sortie du test.
/// </summary>
[Trait("Category", "SqlServer")]
public class SqlServerCodeBarreTests : IClassFixture<BaseJetableFixture>
{
    private readonly BaseJetableFixture _base;
    private readonly ITestOutputHelper _sortie;

    public SqlServerCodeBarreTests(BaseJetableFixture baseJetable, ITestOutputHelper sortie)
    {
        _base = baseJetable;
        _sortie = sortie;
    }

    private static string Unique(string prefixe) => prefixe + "-" + Guid.NewGuid().ToString("N")[..10];

    private static Article Saisie(string reference, string? codeBarre = null) => new()
    {
        Reference = reference, Libelle = "S6", Unite = "Pièce", TauxTVA = 19m, EstActif = true, CodeBarre = codeBarre
    };

    private async Task InsererAsync(string reference, string? codeBarre)
    {
        using var contexte = _base.NouveauContexte();
        contexte.Articles.Add(new Article { Reference = reference, Libelle = "Existant", CodeBarre = codeBarre });
        await contexte.SaveChangesAsync();
    }

    /// <summary>Insère un article avec ce code-barres et renvoie la SqlException du refus (si refus).</summary>
    private async Task<(DbUpdateException Exception, Microsoft.Data.SqlClient.SqlException Sql)> InsererEnDoublonAsync(string premier, string second)
    {
        await InsererAsync(Unique("S6-A"), premier);
        using var contexte = _base.NouveauContexte();
        contexte.Articles.Add(new Article { Reference = Unique("S6-B"), Libelle = "Doublon", CodeBarre = second });
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => contexte.SaveChangesAsync());
        return (exception, BaseJetableFixture.ExtraireSqlException(exception)!);
    }

    [SqlServerFact]
    public void S6a_l_index_reel_est_unique_et_filtre_sur_CodeBarre_non_NULL()
    {
        _base.AssurerBase();
        using var contexte = _base.NouveauContexte();

        var lignes = contexte.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(i.name, ' unique=', i.is_unique, ' filtre=', ISNULL(i.filter_definition, 'aucun')) AS Value " +
            "FROM sys.indexes i WHERE i.object_id = OBJECT_ID('Articles') AND i.name = 'IX_Articles_CodeBarre'").ToList();

        var ligne = Assert.Single(lignes);
        _sortie.WriteLine("[S6a] " + ligne);
        Assert.StartsWith("IX_Articles_CodeBarre unique=1", ligne);
        Assert.Contains("[CodeBarre] IS NOT NULL", ligne);
    }

    [SqlServerFact]
    public async Task S6b_un_doublon_de_code_barres_donne_2601_avec_le_nom_de_l_index_et_la_detection_nomme_CodeBarre()
    {
        _base.AssurerBase();
        var code = Unique("CB");

        var (exception, sql) = await InsererEnDoublonAsync(code, code);

        _sortie.WriteLine($"[S6b] SqlException.Number = {sql.Number} | Message = {sql.Message}");
        Assert.Equal(2601, sql.Number);
        Assert.Contains("IX_Articles_CodeBarre", sql.Message);
        Assert.Equal("CodeBarre", ArticleService.ChampEnViolationIndexUnique(exception));
        Assert.False(ArticleService.EstViolationIndexReference(exception));
    }

    [SqlServerFact]
    public async Task S6c_plusieurs_articles_sans_code_barres_NULL_sont_acceptes()
    {
        _base.AssurerBase();

        await InsererAsync(Unique("S6-N1"), null);
        await InsererAsync(Unique("S6-N2"), null);
        await InsererAsync(Unique("S6-N3"), null);

        using var contexte = _base.NouveauContexte();
        Assert.True(await contexte.Articles.CountAsync(a => a.CodeBarre == null) >= 3);
    }

    [SqlServerFact]
    public async Task S6d_la_collation_rend_l_unicite_du_code_barres_insensible_a_la_casse()
    {
        _base.AssurerBase();
        var code = Unique("Abc");

        var (_, sql) = await InsererEnDoublonAsync(code.ToUpperInvariant(), code.ToLowerInvariant());

        _sortie.WriteLine($"[S6d] SqlException.Number = {sql.Number} | Message = {sql.Message}");
        Assert.Equal(2601, sql.Number);
    }

    [SqlServerFact]
    public async Task S6e_un_espace_en_fin_de_valeur_compte_comme_un_doublon_pour_SQL_Server()
    {
        // Constat : SQL Server ignore les espaces de fin dans la comparaison de l'index ; cohérent avec le Trim() du service.
        _base.AssurerBase();
        var code = Unique("Esp");

        var (_, sql) = await InsererEnDoublonAsync(code, code + " ");

        _sortie.WriteLine($"[S6e] SqlException.Number = {sql.Number} | Message = {sql.Message}");
        Assert.Equal(2601, sql.Number);
    }

    [SqlServerFact]
    public async Task S6f_deux_chaines_vides_sont_un_doublon_pour_l_index_le_service_ne_les_ecrit_jamais()
    {
        // Constat qui justifie N1 : le filtre n'exclut que NULL, pas les chaînes vides.
        _base.AssurerBase();

        var (_, sql) = await InsererEnDoublonAsync(string.Empty, string.Empty);

        _sortie.WriteLine($"[S6f] SqlException.Number = {sql.Number} | Message = {sql.Message}");
        Assert.Equal(2601, sql.Number);
    }

    [SqlServerFact]
    public async Task S6g_creation_doublon_de_code_barres_simultane_donne_une_erreur_sur_CodeBarre()
    {
        _base.AssurerBase();
        var code = Unique("CB-CREA");
        var reference = Unique("S6G");
        var course = new ActionAvantEnregistrement();
        using var contexte = _base.NouveauContexte(course);
        course.Action = async () =>
        {
            using var autre = _base.NouveauContexte();
            autre.Articles.Add(new Article { Reference = Unique("S6G-1ER"), Libelle = "Arrivé en premier", CodeBarre = code });
            await autre.SaveChangesAsync();
        };

        var resultat = await new ArticleService(contexte).CreerAsync(Saisie(reference, code));   // ne doit pas lever d'exception

        Assert.False(resultat.Succes);
        var erreur = Assert.Single(resultat.Erreurs);
        Assert.Equal("CodeBarre", erreur.Champ);
        Assert.Equal("Ce code-barres existe déjà.", erreur.Message);
        using var releve = _base.NouveauContexte();
        Assert.Equal(0, await releve.Articles.CountAsync(a => a.Reference == reference));
    }

    [SqlServerFact]
    public async Task S6h_modification_doublon_de_code_barres_simultane_donne_une_erreur_sur_CodeBarre()
    {
        _base.AssurerBase();
        var reference = Unique("S6H");
        var code = Unique("CB-MOD");
        await InsererAsync(reference, null);
        using var lecture = _base.NouveauContexte();
        var id = (await lecture.Articles.AsNoTracking().SingleAsync(a => a.Reference == reference)).ID;
        var course = new ActionAvantEnregistrement();
        using var contexte = _base.NouveauContexte(course);
        course.Action = async () =>
        {
            using var autre = _base.NouveauContexte();
            autre.Articles.Add(new Article { Reference = Unique("S6H-1ER"), Libelle = "Arrivé en premier", CodeBarre = code });
            await autre.SaveChangesAsync();
        };

        var resultat = await new ArticleService(contexte).ModifierAsync(id, Saisie(reference, code));   // ne doit pas lever d'exception

        Assert.False(resultat.Succes);
        var erreur = Assert.Single(resultat.Erreurs);
        Assert.Equal("CodeBarre", erreur.Champ);
        using var releve = _base.NouveauContexte();
        Assert.Null((await releve.Articles.AsNoTracking().SingleAsync(a => a.ID == id)).CodeBarre);   // inchangé
    }
}
