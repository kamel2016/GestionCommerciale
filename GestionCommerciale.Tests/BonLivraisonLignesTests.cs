using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

/// <summary>
/// S10 : le prix d'une ligne de BL est DÉRIVÉ côté serveur de la ligne de commande liée ; toute valeur postée par le navigateur est
/// ignorée ; chaque ligne doit être liée à une ligne de la commande du BL (message clair sinon).
/// </summary>
public class BonLivraisonLignesTests : ArticleTestBase
{
    private async Task<(int CommandeId, int LigneA, int LigneB, int ArticleA)> CommandeAsync()
    {
        var client = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Client BL" };
        Db.Clients.Add(client);
        var a = new Article { Reference = "BLL-" + Guid.NewGuid().ToString("N")[..6], Libelle = "A", Quantite = 100, PrixVenteHT = 99.000m, TauxTVA = 19m };
        var b = new Article { Reference = "BLL-" + Guid.NewGuid().ToString("N")[..6], Libelle = "B", Quantite = 100, PrixVenteHT = 99.000m, TauxTVA = 7m };
        Db.Articles.AddRange(a, b);
        await Db.SaveChangesAsync();
        var commande = new CommandeClient { NumeroCommande = "CC-BLL-" + Guid.NewGuid().ToString("N")[..5], ClientId = client.Id, Statut = StatutCommande.Confirmee };
        commande.LignesCommande.Add(new LigneCommandeClient { ArticleId = a.ID, Quantite = 10, PrixUnitaire = 6.500m, TauxTVA = 19m });
        commande.LignesCommande.Add(new LigneCommandeClient { ArticleId = b.ID, Quantite = 5, PrixUnitaire = 3.250m, TauxTVA = 7m });
        commande.RecalculerTotaux();
        Db.CommandesClient.Add(commande);
        await Db.SaveChangesAsync();
        var ids = commande.LignesCommande.OrderBy(l => l.Id).Select(l => l.Id).ToList();
        Db.ChangeTracker.Clear();
        return (commande.Id, ids[0], ids[1], a.ID);
    }

    private BonLivraisonService Service() => new(Db, new StockService(Db));

    [Fact]
    public async Task Le_prix_et_l_article_de_chaque_ligne_viennent_de_la_ligne_de_commande_pas_de_l_article_ni_du_navigateur()
    {
        var (commande, ligneA, ligneB, articleA) = await CommandeAsync();

        var (lignes, erreur) = await Service().ConstruireLignesAsync(commande, new (int?, int)[] { (ligneA, 4), (ligneB, 2) });

        Assert.Null(erreur);
        Assert.NotNull(lignes);
        Assert.Equal(new[] { 6.500m, 3.250m }, lignes!.Select(l => l.PrixUnitaire).ToArray());   // ni 99,000 (article) ni une valeur postée
        Assert.Equal(new[] { 4, 2 }, lignes.Select(l => l.Quantite).ToArray());
        Assert.Equal(new int?[] { ligneA, ligneB }, lignes.Select(l => l.LigneCommandeClientId).ToArray());
        Assert.Equal(articleA, lignes[0].ArticleId);
    }

    [Fact]
    public async Task Une_ligne_sans_ligne_de_commande_liee_est_refusee_avec_un_message_clair()
    {
        var (commande, ligneA, _, _) = await CommandeAsync();

        var (lignes, erreur) = await Service().ConstruireLignesAsync(commande, new (int?, int)[] { (ligneA, 1), (null, 3) });

        Assert.Null(lignes);
        Assert.Contains("liée à une ligne de la commande", erreur);
    }

    [Fact]
    public async Task Une_ligne_d_une_autre_commande_est_refusee()
    {
        var (commande, _, _, _) = await CommandeAsync();
        var (_, ligneAutre, _, _) = await CommandeAsync();                 // lignes d'une AUTRE commande

        var (lignes, erreur) = await Service().ConstruireLignesAsync(commande, new (int?, int)[] { (ligneAutre, 1) });

        Assert.Null(lignes);
        Assert.Contains("liée à une ligne de la commande", erreur);
    }

    [Fact]
    public async Task Une_ligne_inexistante_est_refusee()
    {
        var (commande, _, _, _) = await CommandeAsync();

        var (lignes, erreur) = await Service().ConstruireLignesAsync(commande, new (int?, int)[] { (987654, 1) });

        Assert.Null(lignes);
        Assert.Contains("liée à une ligne de la commande", erreur);
    }

    [Fact]
    public async Task Un_bl_sans_commande_est_refuse()
    {
        var (_, ligneA, _, _) = await CommandeAsync();

        var (lignes, erreur) = await Service().ConstruireLignesAsync(null, new (int?, int)[] { (ligneA, 1) });

        Assert.Null(lignes);
        Assert.Contains("à partir d'une commande", erreur);
    }

    [Fact]
    public async Task Les_lignes_de_quantite_nulle_sont_ignorees_et_un_bl_sans_ligne_est_refuse()
    {
        var (commande, ligneA, ligneB, _) = await CommandeAsync();

        var (lignes, _) = await Service().ConstruireLignesAsync(commande, new (int?, int)[] { (ligneA, 0), (ligneB, 2) });
        var (aucune, erreur) = await Service().ConstruireLignesAsync(commande, new (int?, int)[] { (ligneA, 0) });

        Assert.Equal(new int?[] { ligneB }, lignes!.Select(l => l.LigneCommandeClientId).ToArray());
        Assert.Null(aucune);
        Assert.Contains("au moins une quantité", erreur);
    }
}
