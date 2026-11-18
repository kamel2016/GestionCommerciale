using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace GestionCommerciale.Tests;

/// <summary>
/// SONDES sur un vrai SQL Server (base jetable LocalDB créée par Migrate()). Elles ne testent PAS le service :
/// elles CONSTATENT les numéros d'erreur et les messages réels de SQL Server (2601, 2627, 547) et vérifient que
/// les détecteurs existants (ArticleService.EstViolationIndexReference, ClientService.EstViolationCleEtrangere) y réagissent.
/// Les valeurs observées sont écrites dans la sortie du test.
/// </summary>
[Trait("Category", "SqlServer")]
public class SqlServerSondesTests : IClassFixture<BaseJetableFixture>
{
    private readonly BaseJetableFixture _base;
    private readonly ITestOutputHelper _sortie;

    public SqlServerSondesTests(BaseJetableFixture baseJetable, ITestOutputHelper sortie)
    {
        _base = baseJetable;
        _sortie = sortie;
    }

    private static string Unique(string prefixe) => prefixe + "-" + Guid.NewGuid().ToString("N")[..10];

    private void Journal(string sonde, SqlException sql)
        => _sortie.WriteLine($"[{sonde}] SqlException.Number = {sql.Number} | Message = {sql.Message}");

    [SqlServerFact]
    public void S0_la_base_jetable_est_creee_par_Migrate_et_les_garde_fous_refusent_une_mauvaise_cible()
    {
        _base.AssurerBase();

        // Garde-fous : mauvaise machine, mauvais nom, base de développement.
        Assert.Throws<InvalidOperationException>(() => BaseJetableFixture.ControlerCible(
            "Server=tcp:serveur-distant,1433;Database=GestionCommerciale_Test_abc;Integrated Security=true"));
        Assert.Throws<InvalidOperationException>(() => BaseJetableFixture.ControlerCible(
            @"Server=(localdb)\MSSQLLocalDB;Database=GestionCommerciale;Integrated Security=true"));
        Assert.Throws<InvalidOperationException>(() => BaseJetableFixture.ControlerCible(
            @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true"));
        BaseJetableFixture.ControlerCible(_base.ChaineConnexion); // la vraie cible jetable est acceptée

        using var contexte = _base.NouveauContexte();
        var indexes = contexte.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(i.name, ' unique=', i.is_unique, ' filtre=', ISNULL(i.filter_definition, 'aucun')) AS Value " +
            "FROM sys.indexes i WHERE i.object_id = OBJECT_ID('Articles') AND i.name IS NOT NULL ORDER BY i.name").ToList();
        _sortie.WriteLine("[S0] base : " + _base.NomBase);
        foreach (var ligne in indexes) _sortie.WriteLine("[S0] index Articles : " + ligne);

        Assert.Contains(indexes, l => l.StartsWith("IX_Articles_Reference unique=1"));
        Assert.Contains(indexes, l => l.StartsWith("IX_Articles_CodeBarre"));   // son caractère unique dépend de l'étape 4
    }

    /// <summary>
    /// Sonde d'observation (sans assertion de valeur) : empreinte du schéma reconstruit par les migrations, à comparer
    /// avec la même requête exécutée à la main sur la base de développement (lecture seule).
    /// </summary>
    [SqlServerFact]
    public void S0b_empreinte_du_schema_reconstruit_par_les_migrations()
    {
        _base.AssurerBase();
        using var contexte = _base.NouveauContexte();
        var lignes = contexte.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(x.element, ' | ', x.nombre, ' | ', ISNULL(CAST(x.empreinte AS varchar(20)), '-')) AS Value FROM (" +
            "SELECT 'tables' AS element, COUNT(*) AS nombre, CAST(NULL AS int) AS empreinte FROM sys.tables WHERE name <> '__EFMigrationsHistory' " +
            "UNION ALL SELECT 'colonnes', COUNT(*), CHECKSUM_AGG(CHECKSUM(t.name, c.name, ty.name, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity, c.is_computed)) " +
            "FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id WHERE t.name <> '__EFMigrationsHistory' " +
            "UNION ALL SELECT 'index', COUNT(*), CHECKSUM_AGG(CHECKSUM(t.name, i.name, i.is_unique, i.is_primary_key, ISNULL(i.filter_definition, ''))) " +
            "FROM sys.tables t JOIN sys.indexes i ON i.object_id = t.object_id WHERE t.name <> '__EFMigrationsHistory' AND i.name IS NOT NULL " +
            "UNION ALL SELECT 'colonnes d''index', COUNT(*), CHECKSUM_AGG(CHECKSUM(t.name, i.name, ic.key_ordinal, c.name)) " +
            "FROM sys.tables t JOIN sys.indexes i ON i.object_id = t.object_id JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id " +
            "JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE t.name <> '__EFMigrationsHistory' AND i.name IS NOT NULL " +
            "UNION ALL SELECT 'cles etrangeres', COUNT(*), CHECKSUM_AGG(CHECKSUM(OBJECT_NAME(fk.parent_object_id), fk.name, OBJECT_NAME(fk.referenced_object_id), fk.delete_referential_action_desc)) FROM sys.foreign_keys fk " +
            "UNION ALL SELECT 'valeurs par defaut', COUNT(*), CHECKSUM_AGG(CHECKSUM(OBJECT_NAME(d.parent_object_id), d.name, d.definition)) FROM sys.default_constraints d" +
            ") x").ToList();
        foreach (var ligne in lignes) _sortie.WriteLine("[S0b] " + ligne);

        // Détail pour localiser un écart éventuel : empreinte des colonnes et des valeurs par défaut, table par table.
        var parTable = contexte.Database.SqlQueryRaw<string>(
            "SELECT CONCAT('colonnes | ', t.name, ' | ', COUNT(*), ' | ', CHECKSUM_AGG(CHECKSUM(c.name, ty.name, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity, c.is_computed))) AS Value " +
            "FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id " +
            "WHERE t.name <> '__EFMigrationsHistory' GROUP BY t.name " +
            "UNION ALL SELECT CONCAT('defauts | ', OBJECT_NAME(d.parent_object_id), ' | ', COUNT(*), ' | ', CHECKSUM_AGG(CHECKSUM(c.name, d.definition))) " +
            "FROM sys.default_constraints d JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id GROUP BY OBJECT_NAME(d.parent_object_id)").ToList();
        foreach (var ligne in parTable) _sortie.WriteLine("[S0c] " + ligne);

        // Colonnes détaillées des tables où un écart a été relevé (BonsReception, CommandesClient, CommandesFournisseur).
        var detail = contexte.Database.SqlQueryRaw<string>(
            "SELECT CONCAT(t.name, '.', c.name, ' : ', ty.name, '(', c.max_length, ',', c.precision, ',', c.scale, ') null=', c.is_nullable, ' identity=', c.is_identity) AS Value " +
            "FROM sys.tables t JOIN sys.columns c ON c.object_id = t.object_id JOIN sys.types ty ON ty.user_type_id = c.user_type_id " +
            "WHERE t.name IN ('BonsReception', 'CommandesClient', 'CommandesFournisseur')").ToList();
        foreach (var ligne in detail) _sortie.WriteLine("[S0d] " + ligne);

        Assert.NotEmpty(lignes);
    }

    [SqlServerFact]
    public async Task S1_un_doublon_de_reference_donne_l_erreur_2601_avec_le_nom_de_l_index()
    {
        _base.AssurerBase();
        var reference = Unique("S1");
        using (var premier = _base.NouveauContexte())
        {
            premier.Articles.Add(new Article { Reference = reference, Libelle = "Premier" });
            await premier.SaveChangesAsync();
        }

        using var second = _base.NouveauContexte();
        second.Articles.Add(new Article { Reference = reference, Libelle = "Doublon" });
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());

        var sql = BaseJetableFixture.ExtraireSqlException(exception);
        Assert.NotNull(sql);
        Journal("S1", sql!);
        Assert.Equal(2601, sql.Number);
        Assert.Contains("IX_Articles_Reference", sql.Message);
        Assert.True(ArticleService.EstViolationIndexReference(exception));
        Assert.Equal("Reference", ArticleService.ChampEnViolationIndexUnique(exception));
    }

    [SqlServerFact]
    public async Task S1b_la_collation_rend_l_unicite_insensible_a_la_casse()
    {
        _base.AssurerBase();
        var reference = Unique("S1B");
        using (var premier = _base.NouveauContexte())
        {
            premier.Articles.Add(new Article { Reference = reference.ToUpperInvariant(), Libelle = "Premier" });
            await premier.SaveChangesAsync();
        }

        using var second = _base.NouveauContexte();
        second.Articles.Add(new Article { Reference = reference.ToLowerInvariant(), Libelle = "Même référence, autre casse" });
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());

        var sql = BaseJetableFixture.ExtraireSqlException(exception)!;
        Journal("S1b", sql);
        Assert.Equal(2601, sql.Number);
    }

    [SqlServerFact]
    public async Task S2_une_violation_de_cle_primaire_donne_2627_et_n_est_pas_prise_pour_un_doublon_de_reference()
    {
        _base.AssurerBase();
        int idExistant;
        using (var premier = _base.NouveauContexte())
        {
            var article = new Article { Reference = Unique("S2"), Libelle = "Existant" };
            premier.Articles.Add(article);
            await premier.SaveChangesAsync();
            idExistant = article.ID;
        }

        using var second = _base.NouveauContexte();
        await second.Database.OpenConnectionAsync();                       // même session pour IDENTITY_INSERT et l'insertion
        try
        {
            await second.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Articles] ON");
            second.Articles.Add(new Article { ID = idExistant, Reference = Unique("S2-AUTRE"), Libelle = "Même clé primaire" });
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());

            var sql = BaseJetableFixture.ExtraireSqlException(exception)!;
            Journal("S2", sql);
            Assert.Equal(2627, sql.Number);
            Assert.Contains("PK_Articles", sql.Message);
            Assert.False(ArticleService.EstViolationIndexReference(exception));   // le nom de l'index de référence n'y figure pas
            Assert.Null(ArticleService.ChampEnViolationIndexUnique(exception));    // ni celui du code-barres : aucun index unique en cause
            Assert.False(ClientService.EstViolationIndexCode(exception));
        }
        finally
        {
            await second.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Articles] OFF");
        }
    }

    [SqlServerFact]
    public async Task S3_une_categorie_inexistante_donne_l_erreur_547_de_cle_etrangere()
    {
        _base.AssurerBase();
        using var contexte = _base.NouveauContexte();
        contexte.Articles.Add(new Article { Reference = Unique("S3"), Libelle = "Catégorie fantôme", CategoryId = 987654 });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => contexte.SaveChangesAsync());

        var sql = BaseJetableFixture.ExtraireSqlException(exception)!;
        Journal("S3", sql);
        Assert.Equal(547, sql.Number);
        Assert.Contains("FK_Articles_Categories_CategoryId", sql.Message);
        Assert.True(ClientService.EstViolationCleEtrangere(exception));
        Assert.False(ArticleService.EstViolationIndexReference(exception));
    }
}
