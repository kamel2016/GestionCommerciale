using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Tests;

/// <summary>
/// S10b : création d'un BL côté serveur (CreerBrouillonAsync) : client dérivé de la commande (jamais posté), statuts de commande
/// autorisés (Confirmée, En préparation, Expédiée), contrôle de quantité (cumul des lignes de BL NON ANNULÉS, brouillons inclus,
/// une seule source : jamais QuantiteLivree), une ligne de commande une seule fois par BL, même contrôle à la validation, et
/// facturation refusée si le client du BL n'est pas celui de la commande. Sous SQLite : ne prouve pas la concurrence (voir SqlServerBonLivraisonTests).
/// </summary>
public class BonLivraisonCreationTests : ArticleTestBase
{
    private int _compteur;

    private BonLivraisonService Service(ApplicationDbContext? db = null) => new(db ?? Db, new StockService(db ?? Db));

    /// <summary>Générateur de numéro de test : compte ses appels (il ne doit PAS être appelé quand la création est refusée).</summary>
    private Func<Task<string>> Numero => () => Task.FromResult($"BL-T-{++_compteur:D3}");

    private async Task<(int CommandeId, int LigneA, int LigneB, int ClientId)> CommandeAsync(
        StatutCommande statut = StatutCommande.Confirmee, int quantiteA = 10, int quantiteB = 5)
    {
        var client = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Client BL" };
        Db.Clients.Add(client);
        var a = new Article { Reference = "BC-" + Guid.NewGuid().ToString("N")[..6], Libelle = "Câble A", Quantite = 1000, PrixVenteHT = 6.500m, TauxTVA = 19m };
        var b = new Article { Reference = "BC-" + Guid.NewGuid().ToString("N")[..6], Libelle = "Prise B", Quantite = 1000, PrixVenteHT = 3.250m, TauxTVA = 7m };
        Db.Articles.AddRange(a, b);
        await Db.SaveChangesAsync();
        var commande = new CommandeClient { NumeroCommande = "CC-BC-" + Guid.NewGuid().ToString("N")[..5], ClientId = client.Id, Statut = statut };
        commande.LignesCommande.Add(new LigneCommandeClient { ArticleId = a.ID, Quantite = quantiteA, PrixUnitaire = 6.500m, TauxTVA = 19m });
        commande.LignesCommande.Add(new LigneCommandeClient { ArticleId = b.ID, Quantite = quantiteB, PrixUnitaire = 3.250m, TauxTVA = 7m });
        commande.RecalculerTotaux();
        Db.CommandesClient.Add(commande);
        await Db.SaveChangesAsync();
        var ids = commande.LignesCommande.OrderBy(l => l.Id).Select(l => l.Id).ToList();
        Db.ChangeTracker.Clear();
        return (commande.Id, ids[0], ids[1], client.Id);
    }

    private Task<(BonLivraison? Bl, string? Erreur)> CreerAsync(int commandeId, params (int? Ligne, int Quantite)[] saisies)
        => Service().CreerBrouillonAsync(commandeId, DateTime.Today, "test", saisies, Numero);

    // ------------------------------------------------------------------ client dérivé de la commande

    [Fact]
    public async Task Le_client_du_bl_est_celui_de_la_commande_le_numero_et_les_lignes_sont_enregistres()
    {
        var (commande, ligneA, ligneB, client) = await CommandeAsync();

        var (bl, erreur) = await CreerAsync(commande, (ligneA, 4), (ligneB, 2));

        Assert.Null(erreur);
        Db.ChangeTracker.Clear();
        var relu = await Db.BonsLivraison.AsNoTracking().Include(b => b.Lignes).SingleAsync(b => b.ID == bl!.ID);
        Assert.Equal(client, relu.ClientId);                                   // dérivé de la commande
        Assert.Equal(commande, relu.CommandeClientId);
        Assert.Equal("BL-T-001", relu.Numero);
        Assert.Equal(StatutBonLivraison.Brouillon, relu.Statut);
        Assert.Equal(new[] { 6.500m, 3.250m }, relu.Lignes.OrderBy(l => l.ID).Select(l => l.PrixUnitaire).ToArray());
    }

    [Fact]
    public void Le_formulaire_de_bl_ne_poste_plus_le_client()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "GestionCommerciale.sln"))) dir = Path.GetDirectoryName(dir);
        var racine = Path.Combine(dir!, "GestionCommerciale", "Pages", "BonsLivraison");
        var vue = File.ReadAllText(Path.Combine(racine, "Create.cshtml"));
        var page = File.ReadAllText(Path.Combine(racine, "Create.cshtml.cs"));

        Assert.DoesNotContain("asp-for=\"ClientId\"", vue);                    // ni champ caché ni liste déroulante du client
        Assert.DoesNotContain("[BindProperty] public int? ClientId", page);    // la page ne lie plus de client posté
    }

    // ------------------------------------------------------------------ statuts de commande autorisés

    [Theory]
    [InlineData(StatutCommande.Confirmee)]
    [InlineData(StatutCommande.EnPreparation)]
    [InlineData(StatutCommande.Expediee)]
    public async Task Un_bl_est_autorise_pour_une_commande_confirmee_en_preparation_ou_expediee(StatutCommande statut)
    {
        var (commande, ligneA, _, _) = await CommandeAsync(statut);

        var (bl, erreur) = await CreerAsync(commande, (ligneA, 1));

        Assert.Null(erreur);
        Assert.NotNull(bl);
    }

    [Theory]
    [InlineData(StatutCommande.EnAttente)]
    [InlineData(StatutCommande.Livree)]
    [InlineData(StatutCommande.Annulee)]
    public async Task Un_bl_est_refuse_pour_une_commande_en_attente_livree_ou_annulee_sans_consommer_de_numero(StatutCommande statut)
    {
        var (commande, ligneA, _, _) = await CommandeAsync(statut);

        var (bl, erreur) = await CreerAsync(commande, (ligneA, 1));

        Assert.Null(bl);
        Assert.Contains("Confirmée, En préparation ou Expédiée", erreur);
        Assert.Equal(0, _compteur);                                            // le numéro n'est demandé qu'après tous les contrôles
        Assert.Equal(0, await Db.BonsLivraison.CountAsync());
    }

    [Fact]
    public async Task Une_commande_inexistante_est_refusee()
    {
        var (bl, erreur) = await CreerAsync(987654, (1, 1));

        Assert.Null(bl);
        Assert.Contains("introuvable", erreur);
    }

    // ------------------------------------------------------------------ contrôle de quantité

    [Fact]
    public async Task Une_quantite_superieure_au_reste_est_refusee_avec_la_ligne_la_quantite_demandee_et_le_reste_disponible()
    {
        var (commande, ligneA, _, _) = await CommandeAsync(quantiteA: 10);

        var (bl, erreur) = await CreerAsync(commande, (ligneA, 11));

        Assert.Null(bl);
        Assert.Contains("Câble A", erreur);
        Assert.Contains("quantité demandée 11", erreur);
        Assert.Contains("reste disponible 10", erreur);
        Assert.Equal(0, _compteur);
    }

    [Fact]
    public async Task Le_cumul_des_bl_non_annules_brouillons_inclus_limite_la_quantite()
    {
        var (commande, ligneA, _, _) = await CommandeAsync(quantiteA: 10);
        Assert.Null((await CreerAsync(commande, (ligneA, 6))).Erreur);          // BL brouillon de 6 : reste 4

        var (refuse, erreur) = await CreerAsync(commande, (ligneA, 5));
        var (accepte, erreurOk) = await CreerAsync(commande, (ligneA, 4));

        Assert.Null(refuse);
        Assert.Contains("quantité demandée 5", erreur);
        Assert.Contains("reste disponible 4", erreur);
        Assert.Null(erreurOk);
        Assert.NotNull(accepte);
        var (encore, erreurZero) = await CreerAsync(commande, (ligneA, 1));      // reste 0
        Assert.Null(encore);
        Assert.Contains("reste disponible 0", erreurZero);
    }

    [Fact]
    public async Task Un_bl_annule_ne_compte_pas_dans_le_cumul()
    {
        var (commande, ligneA, _, _) = await CommandeAsync(quantiteA: 10);
        var (premier, _) = await CreerAsync(commande, (ligneA, 10));
        Db.ChangeTracker.Clear();
        (await Db.BonsLivraison.SingleAsync(b => b.ID == premier!.ID)).Statut = StatutBonLivraison.Annule;
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var (second, erreur) = await CreerAsync(commande, (ligneA, 10));

        Assert.Null(erreur);
        Assert.NotNull(second);
    }

    [Fact]
    public async Task La_seule_source_est_le_cumul_des_lignes_de_bl_jamais_QuantiteLivree()
    {
        var (commande, ligneA, _, _) = await CommandeAsync(quantiteA: 10);
        var ligne = await Db.LignesCommandeClient.SingleAsync(l => l.Id == ligneA);
        ligne.QuantiteLivree = 10;                                               // valeur incohérente : aucune ligne de BL ne la justifie
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var (bl, erreur) = await CreerAsync(commande, (ligneA, 10));

        Assert.Null(erreur);                                                     // QuantiteLivree est ignorée
        Assert.NotNull(bl);
    }

    [Fact]
    public async Task Une_meme_ligne_de_commande_ne_peut_apparaitre_qu_une_fois_dans_un_bl()
    {
        var (commande, ligneA, _, _) = await CommandeAsync(quantiteA: 10);

        var (bl, erreur) = await CreerAsync(commande, (ligneA, 2), (ligneA, 3));

        Assert.Null(bl);
        Assert.Contains("une seule fois", erreur);
        Assert.Contains("Câble A", erreur);
        Assert.Equal(0, _compteur);
    }

    [Fact]
    public async Task Le_controle_de_quantite_porte_sur_chaque_ligne_et_les_lignes_a_zero_sont_ignorees()
    {
        var (commande, ligneA, ligneB, _) = await CommandeAsync(quantiteA: 10, quantiteB: 5);

        var (refuse, erreur) = await CreerAsync(commande, (ligneA, 10), (ligneB, 6));
        var (accepte, _) = await CreerAsync(commande, (ligneA, 0), (ligneB, 5));

        Assert.Null(refuse);
        Assert.Contains("Prise B", erreur);
        Assert.Contains("reste disponible 5", erreur);
        Assert.NotNull(accepte);
        Db.ChangeTracker.Clear();
        Assert.Single(await Db.LignesBonLivraison.Where(l => l.BonLivraisonId == accepte!.ID).ToListAsync());
    }

    // ------------------------------------------------------------------ même contrôle à la validation

    private async Task<int> BrouillonDirectAsync(int commandeId, int clientId, int ligneId, int articleId, int quantite)
    {
        var bl = new BonLivraison { Numero = "BL-D-" + Guid.NewGuid().ToString("N")[..5], ClientId = clientId, CommandeClientId = commandeId, Statut = StatutBonLivraison.Brouillon };
        bl.Lignes.Add(new LigneBonLivraison { ArticleId = articleId, LigneCommandeClientId = ligneId, Quantite = quantite, PrixUnitaire = 6.500m });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return bl.ID;
    }

    [Fact]
    public async Task La_validation_refuse_un_cumul_superieur_a_la_quantite_commandee_avec_un_message_clair()
    {
        // deux brouillons de 8 sur une ligne de 10, fabriqués directement (la création les aurait refusés)
        var (commande, ligneA, _, client) = await CommandeAsync(quantiteA: 10);
        var articleA = (await Db.LignesCommandeClient.AsNoTracking().SingleAsync(l => l.Id == ligneA)).ArticleId;
        var bl1 = await BrouillonDirectAsync(commande, client, ligneA, articleA, 8);
        await BrouillonDirectAsync(commande, client, ligneA, articleA, 8);

        var (succes, erreur) = await Service().ValiderAsync(bl1);

        Assert.False(succes);
        Assert.Contains("Câble A", erreur);
        Assert.Contains("quantité demandée 8", erreur);
        Assert.Contains("reste disponible 2", erreur);                           // 10 commandés - 8 sur l'autre BL
        Assert.Equal(StatutBonLivraison.Brouillon, (await Db.BonsLivraison.AsNoTracking().SingleAsync(b => b.ID == bl1)).Statut);
        Assert.Empty(await Db.MouvementsStock.ToListAsync());                    // aucune sortie de stock
    }

    [Fact]
    public async Task La_validation_accepte_un_bl_qui_respecte_le_cumul()
    {
        var (commande, ligneA, _, _) = await CommandeAsync(quantiteA: 10);
        var (bl, _) = await CreerAsync(commande, (ligneA, 10));

        var (succes, erreur) = await Service().ValiderAsync(bl!.ID);

        Assert.True(succes, erreur);
    }

    // ------------------------------------------------------------------ facture : le client du BL doit être celui de la commande

    [Fact]
    public async Task La_facture_est_refusee_si_le_client_du_bl_n_est_pas_celui_de_la_commande()
    {
        var p = new ParametresSociete { RaisonSociale = "Société test", TimbreFiscal = 1.000m };
        Db.ParametresSociete.Add(p);
        var (commande, ligneA, _, _) = await CommandeAsync();
        var autre = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Autre client" };
        Db.Clients.Add(autre);
        await Db.SaveChangesAsync();
        var articleA = (await Db.LignesCommandeClient.AsNoTracking().SingleAsync(l => l.Id == ligneA)).ArticleId;
        var bl = new BonLivraison { Numero = "BL-X-" + Guid.NewGuid().ToString("N")[..5], ClientId = autre.Id, CommandeClientId = commande, Statut = StatutBonLivraison.Valide };
        bl.Lignes.Add(new LigneBonLivraison { ArticleId = articleA, LigneCommandeClientId = ligneA, Quantite = 1, PrixUnitaire = 6.500m });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new FactureService(Db, null!).CreerDepuisBonLivraisonAsync(bl.ID));

        Assert.Contains("client", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await Db.Factures.CountAsync());
    }

    [Fact]
    public async Task La_facture_est_refusee_si_le_bl_n_a_pas_de_client()
    {
        Db.ParametresSociete.Add(new ParametresSociete { RaisonSociale = "Société test", TimbreFiscal = 1.000m });
        var (commande, ligneA, _, _) = await CommandeAsync();
        var articleA = (await Db.LignesCommandeClient.AsNoTracking().SingleAsync(l => l.Id == ligneA)).ArticleId;
        var bl = new BonLivraison { Numero = "BL-N-" + Guid.NewGuid().ToString("N")[..5], ClientId = null, CommandeClientId = commande, Statut = StatutBonLivraison.Valide };
        bl.Lignes.Add(new LigneBonLivraison { ArticleId = articleA, LigneCommandeClientId = ligneA, Quantite = 1, PrixUnitaire = 6.500m });
        Db.BonsLivraison.Add(bl);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new FactureService(Db, null!).CreerDepuisBonLivraisonAsync(bl.ID));

        Assert.Contains("client", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------ S10b (précisions) : validation d'un brouillon et commande Annulée ou Livrée

    [Theory]
    [InlineData(StatutCommande.Annulee, "Annulée")]
    [InlineData(StatutCommande.Livree, "Livrée")]
    public async Task La_validation_d_un_brouillon_est_refusee_si_la_commande_est_annulee_ou_livree_sans_aucune_sortie_de_stock(StatutCommande statut, string libelle)
    {
        var (commande, ligneA, _, client) = await CommandeAsync(StatutCommande.Confirmee, quantiteA: 10);
        var articleA = (await Db.LignesCommandeClient.AsNoTracking().SingleAsync(l => l.Id == ligneA)).ArticleId;
        var bl = await BrouillonDirectAsync(commande, client, ligneA, articleA, 4);
        var cmd = await Db.CommandesClient.SingleAsync(c => c.Id == commande);
        cmd.Statut = statut;                                                      // la commande change de statut APRÈS la création du brouillon
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var (succes, erreur) = await Service().ValiderAsync(bl);

        Assert.False(succes);
        Assert.Contains(libelle, erreur);
        Assert.Contains("ne peut pas être validé", erreur);
        Assert.Equal(StatutBonLivraison.Brouillon, (await Db.BonsLivraison.AsNoTracking().SingleAsync(b => b.ID == bl)).Statut);
        Assert.Empty(await Db.MouvementsStock.ToListAsync());
        Assert.Equal(1000, (await Db.Articles.AsNoTracking().SingleAsync(a => a.ID == articleA)).Quantite);
    }

    // ------------------------------------------------------------------ un BL ne se compte pas lui-même à la validation

    [Fact]
    public async Task Un_bl_qui_livre_toute_la_ligne_ne_se_compte_pas_lui_meme_a_la_validation()
    {
        var (commande, ligneA, _, _) = await CommandeAsync(quantiteA: 10);
        var (bl, _) = await CreerAsync(commande, (ligneA, 10));                   // 10 sur 10 : les AUTRES BL valent 0

        var (succes, erreur) = await Service().ValiderAsync(bl!.ID);

        Assert.True(succes, erreur);                                              // si le BL se comptait lui-même : 10 + 10 > 10
    }

    [Fact]
    public async Task Deux_bl_qui_se_partagent_exactement_la_ligne_se_valident_chacun_contre_le_cumul_des_autres()
    {
        var (commande, ligneA, _, _) = await CommandeAsync(quantiteA: 10);
        var (bl1, _) = await CreerAsync(commande, (ligneA, 6));
        var (bl2, _) = await CreerAsync(commande, (ligneA, 4));

        var (ok1, e1) = await Service().ValiderAsync(bl1!.ID);                    // autres = 4 ; 6 + 4 <= 10
        var (ok2, e2) = await Service().ValiderAsync(bl2!.ID);                    // autres = 6 ; 4 + 6 <= 10

        Assert.True(ok1, e1);
        Assert.True(ok2, e2);
        Db.ChangeTracker.Clear();
        Assert.Equal(10, (await Db.LignesCommandeClient.AsNoTracking().SingleAsync(l => l.Id == ligneA)).QuantiteLivree);
    }

    // ------------------------------------------------------------------ client forgé

    [Fact]
    public async Task Un_client_forge_different_de_celui_de_la_commande_est_refuse_sans_creer_de_bl_ni_consommer_de_numero()
    {
        var (commande, ligneA, _, client) = await CommandeAsync();
        var autre = new Client { Code = "CLI-" + Guid.NewGuid().ToString("N")[..8], Nom = "Client forgé" };
        Db.Clients.Add(autre);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var (bl, erreur) = await Service().CreerBrouillonAsync(commande, DateTime.Today, null, new (int?, int)[] { (ligneA, 1) }, Numero, clientIdPoste: autre.Id);

        Assert.Null(bl);
        Assert.Contains("client", erreur, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, _compteur);
        Assert.Equal(0, await Db.BonsLivraison.CountAsync());

        var (ok, erreurOk) = await Service().CreerBrouillonAsync(commande, DateTime.Today, null, new (int?, int)[] { (ligneA, 1) }, Numero, clientIdPoste: client);
        Assert.Null(erreurOk);                                                    // le bon client posté est accepté (et ignoré : le client vient de la commande)
        Assert.Equal(client, ok!.ClientId);
    }
}
