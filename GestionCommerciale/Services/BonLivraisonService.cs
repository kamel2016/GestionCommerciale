// Services/BonLivraisonService.cs
using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Extensions;

namespace GestionCommerciale.Services
{
    public class BonLivraisonService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockService _stock;

        /// <summary>Valeur de MouvementStock.SourceType pour les mouvements créés par un bon de livraison.</summary>
        public const string SourceBonLivraison = "BonLivraison";

        public BonLivraisonService(ApplicationDbContext context, IStockService stock)
        {
            _context = context;
            _stock = stock;
        }

        /// <summary>
        /// Construit les lignes d'un BL à partir des SEULES données de la commande : article et prix viennent de la ligne de commande
        /// liée (jamais d'une valeur postée par le navigateur). Chaque ligne doit être liée à une ligne de la commande du BL.
        /// Les lignes de quantité nulle sont ignorées. Retourne soit les lignes, soit un message d'erreur clair.
        /// </summary>
        public async Task<(List<LigneBonLivraison>? Lignes, string? Erreur)> ConstruireLignesAsync(
            int? commandeClientId, IEnumerable<(int? LigneCommandeClientId, int Quantite)> saisies)
        {
            if (commandeClientId == null)
                return (null, "Un bon de livraison doit être créé à partir d'une commande client.");

            var retenues = saisies.Where(s => s.Quantite > 0).ToList();
            if (!retenues.Any())
                return (null, "Saisissez au moins une quantité à livrer.");
            if (retenues.Any(s => s.LigneCommandeClientId == null))
                return (null, "Chaque ligne du bon de livraison doit être liée à une ligne de la commande.");

            var lignesCommande = await _context.LignesCommandeClient.AsNoTracking()
                .Where(l => l.CommandeClientId == commandeClientId)
                .ToDictionaryAsync(l => l.Id);

            var lignes = new List<LigneBonLivraison>();
            foreach (var s in retenues)
            {
                if (!lignesCommande.TryGetValue(s.LigneCommandeClientId!.Value, out var ligneCommande))
                    return (null, "Chaque ligne du bon de livraison doit être liée à une ligne de la commande.");

                lignes.Add(new LigneBonLivraison
                {
                    ArticleId = ligneCommande.ArticleId,
                    LigneCommandeClientId = ligneCommande.Id,
                    Quantite = s.Quantite,
                    PrixUnitaire = ligneCommande.PrixUnitaire
                });
            }
            return (lignes, null);
        }

        /// <summary>Statuts de commande pour lesquels un bon de livraison peut être créé (décision du 2026-11-21).</summary>
        public static readonly StatutCommande[] StatutsCommandeAutorisantUnBl =
            { StatutCommande.Confirmee, StatutCommande.EnPreparation, StatutCommande.Expediee };

        public static string LibelleStatutCommande(StatutCommande statut) => statut switch
        {
            StatutCommande.EnAttente => "En attente",
            StatutCommande.Confirmee => "Confirmée",
            StatutCommande.EnPreparation => "En préparation",
            StatutCommande.Expediee => "Expédiée",
            StatutCommande.Livree => "Livrée",
            StatutCommande.Annulee => "Annulée",
            _ => statut.ToString()
        };

        /// <summary>
        /// VERROU DE COMMANDE (ordre de verrouillage unique : la COMMANDE d'abord, puis ses lignes et les articles). Une transaction seule
        /// ne ferme pas la course sur le contrôle de quantité : en READ COMMITTED, deux sessions lisent le même cumul avant que l'une
        /// n'insère. UPDLOCK + HOLDLOCK sur la ligne de la commande sérialise les créations, validations et annulations de BL d'une même
        /// commande jusqu'à la fin de la transaction. À poser AVANT toute lecture du cumul ou des lignes.
        /// SQL Server seulement. Chemin SQLite (tests) : aucun verrou, volontairement : SQLite n'a pas de verrou de ligne ni de hint,
        /// et nos tests y utilisent une seule connexion ; la course est prouvée uniquement par les tests SQL Server réels.
        /// </summary>
        private async Task VerrouillerCommandeAsync(int commandeId)
        {
            if (!_context.Database.IsSqlServer()) return;
            if (_context.Database.CurrentTransaction == null)
                throw new InvalidOperationException("Le verrou de commande exige une transaction ouverte (il serait libéré aussitôt).");

            _ = await _context.Database
                .SqlQuery<int>($"SELECT Id AS Value FROM CommandesClient WITH (UPDLOCK, HOLDLOCK) WHERE Id = {commandeId}")
                .ToListAsync();
        }

        /// <summary>Somme des quantités des lignes de BL NON ANNULÉS (brouillons inclus) par ligne de commande, hors le BL exclu. Seule source du cumul.</summary>
        private async Task<Dictionary<int, int>> QuantitesAutresBonsAsync(IReadOnlyCollection<int> ligneCommandeIds, int? exclureBonLivraisonId)
        {
            return await _context.LignesBonLivraison.AsNoTracking()
                .Where(l => l.LigneCommandeClientId != null && ligneCommandeIds.Contains(l.LigneCommandeClientId.Value)
                            && (exclureBonLivraisonId == null || l.BonLivraisonId != exclureBonLivraisonId)
                            && l.BonLivraison!.Statut != StatutBonLivraison.Annule)
                .GroupBy(l => l.LigneCommandeClientId!.Value)
                .Select(g => new { Id = g.Key, Quantite = g.Sum(x => x.Quantite) })
                .ToDictionaryAsync(x => x.Id, x => x.Quantite);
        }

        private static string MessageReste(string libelle, int demandee, int commandee, int dejaPris)
            => $"Ligne « {libelle} » : quantité demandée {demandee}, reste disponible {Math.Max(0, commandee - dejaPris)} " +
               $"(commandée {commandee}, déjà prise par d'autres bons de livraison {dejaPris}).";

        /// <summary>Reste à livrer par ligne de commande (même règle que la création : cumul des BL non annulés), pour l'affichage du formulaire.</summary>
        public async Task<Dictionary<int, int>> ResteParLigneAsync(int commandeClientId)
        {
            var lignes = await _context.LignesCommandeClient.AsNoTracking().Where(l => l.CommandeClientId == commandeClientId).ToListAsync();
            var pris = await QuantitesAutresBonsAsync(lignes.Select(l => l.Id).ToList(), null);
            return lignes.ToDictionary(l => l.Id, l => Math.Max(0, l.Quantite - pris.GetValueOrDefault(l.Id)));
        }

        /// <summary>
        /// Crée un BL BROUILLON depuis une commande, dans une transaction, après verrou de la commande : le client est TOUJOURS celui de la
        /// commande (un client posté différent est refusé, sinon ignoré) ; statut de commande autorisé ; chaque ligne liée à une ligne de la
        /// commande, une seule fois par BL ; quantité demandée + cumul des BL non annulés (brouillons inclus) &lt;= quantité commandée.
        /// Le générateur de numéro n'est appelé qu'après tous les contrôles (aucun numéro consommé en cas de refus).
        /// </summary>
        public async Task<(BonLivraison? Bl, string? Erreur)> CreerBrouillonAsync(
            int? commandeClientId, DateTime date, string? commentaire,
            IEnumerable<(int? LigneCommandeClientId, int Quantite)> saisies, Func<Task<string>> genererNumero, int? clientIdPoste = null)
        {
            if (commandeClientId == null)
                return (null, "Un bon de livraison doit être créé à partir d'une commande client.");
            var saisiesListe = saisies.ToList();

            var transactionPropre = _context.Database.CurrentTransaction == null;
            await using var tx = transactionPropre ? await _context.Database.BeginTransactionAsync() : null;

            await VerrouillerCommandeAsync(commandeClientId.Value);

            var commande = await _context.CommandesClient.AsNoTracking()
                .Include(c => c.LignesCommande).ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(c => c.Id == commandeClientId.Value);
            if (commande == null)
                return (null, "Commande introuvable.");

            if (clientIdPoste.HasValue && clientIdPoste.Value != commande.ClientId)
                return (null, $"Le client indiqué ne correspond pas au client de la commande {commande.NumeroCommande} : le client d'un bon de livraison est toujours celui de sa commande.");

            if (!StatutsCommandeAutorisantUnBl.Contains(commande.Statut))
                return (null, $"La commande {commande.NumeroCommande} est au statut « {LibelleStatutCommande(commande.Statut)} » : " +
                              "un bon de livraison n'est possible que pour une commande Confirmée, En préparation ou Expédiée.");

            var (lignes, erreur) = await ConstruireLignesAsync(commande.Id, saisiesListe);
            if (lignes == null)
                return (null, erreur);

            var parLigne = commande.LignesCommande.ToDictionary(l => l.Id);
            var doublon = lignes.GroupBy(l => l.LigneCommandeClientId!.Value).FirstOrDefault(g => g.Count() > 1);
            if (doublon != null)
                return (null, $"Ligne « {parLigne[doublon.Key].Article.Libelle} » : une ligne de commande ne peut apparaître qu'une seule fois dans un bon de livraison.");

            var dejaPris = await QuantitesAutresBonsAsync(lignes.Select(l => l.LigneCommandeClientId!.Value).ToList(), null);
            foreach (var l in lignes)
            {
                var ligneCommande = parLigne[l.LigneCommandeClientId!.Value];
                var pris = dejaPris.GetValueOrDefault(ligneCommande.Id);
                if (l.Quantite + pris > ligneCommande.Quantite)
                    return (null, MessageReste(ligneCommande.Article.Libelle, l.Quantite, ligneCommande.Quantite, pris));
            }

            var bl = new BonLivraison
            {
                Numero = await genererNumero(),
                Date = date,
                ClientId = commande.ClientId,
                CommandeClientId = commande.Id,
                Commentaire = commentaire,
                Statut = StatutBonLivraison.Brouillon,
                Lignes = lignes
            };
            _context.BonsLivraison.Add(bl);
            await _context.SaveChangesAsync();
            if (tx != null) await tx.CommitAsync();
            return (bl, null);
        }

        public async Task<(bool Succes, string? Erreur)> ValiderAsync(int bonLivraisonId)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            // Verrou de la commande AVANT toute lecture des lignes et du cumul (ordre unique : commande, puis lignes, puis articles).
            var commandeIdVerrou = await _context.BonsLivraison.AsNoTracking()
                .Where(b => b.ID == bonLivraisonId).Select(b => b.CommandeClientId).FirstOrDefaultAsync();
            if (commandeIdVerrou != null) await VerrouillerCommandeAsync(commandeIdVerrou.Value);

            var bl = await _context.BonsLivraison
                .Include(b => b.Lignes).ThenInclude(l => l.Article)
                .Include(b => b.Lignes).ThenInclude(l => l.LigneCommandeClient)
                .Include(b => b.CommandeClient).ThenInclude(c => c!.LignesCommande)
                .FirstOrDefaultAsync(b => b.ID == bonLivraisonId);

            if (bl == null) return (false, "Bon de livraison introuvable.");
            if (bl.Statut != StatutBonLivraison.Brouillon)
                return (false, "Seul un brouillon peut être validé.");
            if (!bl.Lignes.Any())
                return (false, "Le bon de livraison ne contient aucune ligne.");

            // 0. Commande Annulée ou Livrée : le brouillon ne peut plus être validé
            if (bl.CommandeClient != null && bl.CommandeClient.Statut is StatutCommande.Annulee or StatutCommande.Livree)
                return (false, $"La commande {bl.CommandeClient.NumeroCommande} est au statut « {LibelleStatutCommande(bl.CommandeClient.Statut)} » : " +
                               "ce bon de livraison ne peut pas être validé.");

            // 0 bis. Cumul : les lignes de CE BL contre le cumul des AUTRES BL non annulés (un BL ne se compte pas lui-même)
            var lignesLiees = bl.Lignes.Where(l => l.LigneCommandeClientId != null).GroupBy(l => l.LigneCommandeClientId!.Value).ToList();
            if (lignesLiees.Count > 0)
            {
                var autres = await QuantitesAutresBonsAsync(lignesLiees.Select(g => g.Key).ToList(), bl.ID);
                foreach (var g in lignesLiees)
                {
                    var premiere = g.First();
                    var demandee = g.Sum(l => l.Quantite);
                    var pris = autres.GetValueOrDefault(g.Key);
                    if (demandee + pris > premiere.LigneCommandeClient!.Quantite)
                        return (false, MessageReste(premiere.Article!.Libelle, demandee, premiere.LigneCommandeClient.Quantite, pris));
                }
            }

            // 1. Contrôle du stock
            foreach (var ligne in bl.Lignes)
            {
                if (ligne.Quantite <= 0)
                    return (false, "Les quantités doivent être supérieures à zéro.");

                if (ligne.Article!.Quantite < ligne.Quantite)
                    return (false,
                        $"Stock insuffisant pour {ligne.Article.Libelle} " +
                        $"(disponible : {ligne.Article.Quantite}, demandé : {ligne.Quantite}).");
            }

            // 2. Sortie de stock + mouvements (point d'entrée unique : StockService, quantité positive, type SortieLivraison)
            foreach (var ligne in bl.Lignes)
            {
                try
                {
                    await _stock.EnregistrerMouvementAsync(ligne.ArticleId, TypeMouvementStock.SortieLivraison, ligne.Quantite,
                        $"Bon de livraison {bl.Numero}", bl.Numero, SourceBonLivraison, bl.ID);
                }
                catch (InvalidOperationException ex)   // stock insuffisant (par exemple deux lignes du même article)
                {
                    return (false, ex.Message);
                }

                if (ligne.LigneCommandeClient != null)
                    ligne.LigneCommandeClient.QuantiteLivree += ligne.Quantite;
            }

            // 3. Statut de la commande
            if (bl.CommandeClient != null)
            {
                bool toutLivre = bl.CommandeClient.LignesCommande.All(l => l.QuantiteRestante <= 0);
                // Adapte à ton enum de statut de commande :
                // bl.CommandeClient.Statut = toutLivre
                //     ? StatutCommandeClient.Livree
                //     : StatutCommandeClient.PartiellementLivree;
            }

            bl.Statut = StatutBonLivraison.Valide;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, null);
        }

        public async Task<(bool Succes, string? Erreur)> AnnulerAsync(int bonLivraisonId)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

            // Même ordre de verrouillage que la validation : la commande d'abord (évite la perte de QuantiteLivree en cas d'annulation et
            // de validation simultanées de deux BL de la même commande).
            var commandeIdVerrou = await _context.BonsLivraison.AsNoTracking()
                .Where(b => b.ID == bonLivraisonId).Select(b => b.CommandeClientId).FirstOrDefaultAsync();
            if (commandeIdVerrou != null) await VerrouillerCommandeAsync(commandeIdVerrou.Value);

            var bl = await _context.BonsLivraison
                .Include(b => b.Lignes).ThenInclude(l => l.Article)
                .Include(b => b.Lignes).ThenInclude(l => l.LigneCommandeClient)
                .FirstOrDefaultAsync(b => b.ID == bonLivraisonId);

            if (bl == null) return (false, "Bon de livraison introuvable.");
            if (bl.Statut == StatutBonLivraison.Facture)
                return (false, "Impossible d'annuler un BL déjà facturé.");
            if (bl.Statut == StatutBonLivraison.Annule)
                return (false, "Ce BL est déjà annulé.");

            // Garde-fou indépendant du statut : une annulation est déjà enregistrée pour ce BL (jamais deux fois l'effet).
            if (await _context.MouvementsStock.AnyAsync(m => m.Type == TypeMouvementStock.EntreeAnnulationLivraison
                    && m.SourceType == SourceBonLivraison && m.SourceId == bl.ID))
                return (false, "Ce BL est déjà annulé.");

            // Règle réelle : le statut Facture du BL n'est jamais posé par FactureService (la facture ne fait que référencer
            // Facture.BonLivraisonId), donc on refuse dès qu'une facture NON annulée est liée à ce BL.
            if (await _context.Factures.AnyAsync(f => f.BonLivraisonId == bl.ID && f.Statut != StatutFacture.Annulee))
                return (false, "Impossible d'annuler ce BL : il est lié à une facture non annulée. Annulez d'abord la facture.");

            // Si validé, on remet le stock
            if (bl.Statut == StatutBonLivraison.Valide)
            {
                foreach (var ligne in bl.Lignes)
                {
                    // Mouvement inverse (jamais de suppression du mouvement d'origine), type dédié, quantité positive.
                    await _stock.EnregistrerMouvementAsync(ligne.ArticleId, TypeMouvementStock.EntreeAnnulationLivraison, ligne.Quantite,
                        $"Annulation BL {bl.Numero}", bl.Numero, SourceBonLivraison, bl.ID);

                    if (ligne.LigneCommandeClient != null)
                        ligne.LigneCommandeClient.QuantiteLivree -= ligne.Quantite;
                }
            }

            bl.Statut = StatutBonLivraison.Annule;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, null);
        }
    }
}