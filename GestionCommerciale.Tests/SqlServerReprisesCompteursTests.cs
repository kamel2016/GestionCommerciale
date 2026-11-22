using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace GestionCommerciale.Tests;

/// <summary>
/// Script de reprise des compteurs de numérotation (docs/reprise_compteurs_sequences.sql) exécuté sur un VRAI SQL Server (base LocalDB
/// jetable, supprimée ensuite), seulement avec GC_TESTS_SQLSERVER=1. Chaque cas utilise son propre exercice.
/// </summary>
[Trait("Category", "SqlServer")]
public class SqlServerReprisesCompteursTests : IClassFixture<BaseJetableFixture>
{
    private readonly BaseJetableFixture _base;
    private readonly ITestOutputHelper _sortie;

    public SqlServerReprisesCompteursTests(BaseJetableFixture baseJetable, ITestOutputHelper sortie) { _base = baseJetable; _sortie = sortie; }

    private static string Script()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "GestionCommerciale.sln"))) dir = Path.GetDirectoryName(dir);
        return File.ReadAllText(Path.Combine(dir!, "docs", "reprise_compteurs_sequences.sql"));
    }

    private async Task<Dictionary<(string Type, int Exercice), int>> CompteursAsync()
    {
        using var db = _base.NouveauContexte();
        return await db.SequencesDocument.AsNoTracking().ToDictionaryAsync(s => (s.TypeDocument, s.Exercice), s => s.DernierNumero);
    }

    [SqlServerFact]
    public async Task La_reprise_cree_ou_releve_les_compteurs_ne_les_diminue_jamais_ignore_les_formats_inconnus_et_est_idempotente()
    {
        _base.AssurerBase();
        using (var db = _base.NouveauContexte())
        {
            // BL 2040 : max 5, aucun compteur -> créé à 5 (le cas de la base de dev)
            foreach (var n in new[] { "BL-2040-00005", "BL-2040-00002" }) db.BonsLivraison.Add(new BonLivraison { Numero = n, Statut = StatutBonLivraison.Annule });
            // BL 2041 : max 3 mais compteur déjà à 9 -> reste 9 (ne diminue jamais)
            db.BonsLivraison.Add(new BonLivraison { Numero = "BL-2041-00003", Statut = StatutBonLivraison.Annule });
            // BL 2042 : max 4, compteur à 1 -> relevé à 4
            db.BonsLivraison.Add(new BonLivraison { Numero = "BL-2042-00004", Statut = StatutBonLivraison.Annule });
            // formats inconnus : ignorés
            foreach (var n in new[] { "BL-ANCIEN", "BL-2043-12AB", "BL-043-00001", "X-2043-00009" }) db.BonsLivraison.Add(new BonLivraison { Numero = n, Statut = StatutBonLivraison.Annule });
            // facture validée : FAC 2040 max 7
            db.Factures.Add(new Facture { Numero = "FAC-2040-00007", Statut = StatutFacture.Validee });
            db.Factures.Add(new Facture { Numero = null, Statut = StatutFacture.Brouillon });                 // brouillon sans numéro : ignoré
            db.SequencesDocument.Add(new SequenceDocument { TypeDocument = "BL", Exercice = 2041, DernierNumero = 9 });
            db.SequencesDocument.Add(new SequenceDocument { TypeDocument = "BL", Exercice = 2042, DernierNumero = 1 });
            await db.SaveChangesAsync();
        }

        using (var db = _base.NouveauContexte()) await db.Database.ExecuteSqlRawAsync(Script());
        var apres = await CompteursAsync();
        _sortie.WriteLine("[reprise] " + string.Join(" ; ", apres.OrderBy(x => x.Key).Select(x => $"{x.Key.Type}/{x.Key.Exercice}={x.Value}")));

        Assert.Equal(5, apres[("BL", 2040)]);
        Assert.Equal(9, apres[("BL", 2041)]);                          // jamais diminué
        Assert.Equal(4, apres[("BL", 2042)]);                          // relevé
        Assert.Equal(7, apres[("FAC", 2040)]);
        Assert.False(apres.ContainsKey(("BL", 2043)));                 // « BL-2043-12AB » : rang illisible, ignoré
        Assert.False(apres.ContainsKey(("X", 2043)));

        using (var db = _base.NouveauContexte()) await db.Database.ExecuteSqlRawAsync(Script());   // 2e passage : aucun changement
        Assert.Equal(apres, await CompteursAsync());
    }
}
