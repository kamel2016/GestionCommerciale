// Services/BonLivraisonService.cs
using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.EntityFrameworkCore;

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

        public async Task<(bool Succes, string? Erreur)> ValiderAsync(int bonLivraisonId)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();

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

            // 1. Contrôle du stock
            foreach (var ligne in bl.Lignes)
            {
                if (ligne.Quantite <= 0)
                    return (false, "Les quantités doivent être supérieures à zéro.");

                if (ligne.Article!.Quantite < ligne.Quantite)
                    return (false,
                        $"Stock insuffisant pour {ligne.Article.Libelle} " +
                        $"(disponible : {ligne.Article.Quantite}, demandé : {ligne.Quantite}).");

                if (ligne.LigneCommandeClient != null &&
                    ligne.Quantite > ligne.LigneCommandeClient.QuantiteRestante)
                    return (false,
                        $"La quantité livrée de {ligne.Article.Libelle} dépasse le reliquat de la commande.");
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