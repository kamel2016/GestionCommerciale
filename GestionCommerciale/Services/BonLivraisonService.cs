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

        public BonLivraisonService(ApplicationDbContext context)
        {
            _context = context;
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

            // 2. Sortie de stock + mouvements
            foreach (var ligne in bl.Lignes)
            {
                var article = ligne.Article!;
                article.Quantite -= ligne.Quantite;

                _context.MouvementsStock.Add(new MouvementStock
                {
                    ArticleId = article.ID,
                    Date = DateTime.Now,
                    Type = TypeMouvementStock.SortieLivraison,
                    Quantite = ligne.Quantite,
                    QuantiteApres = article.Quantite,
                    Commentaire = $"Bon de livraison {bl.Numero}"
                });

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

            // Si validé, on remet le stock
            if (bl.Statut == StatutBonLivraison.Valide)
            {
                foreach (var ligne in bl.Lignes)
                {
                    var article = ligne.Article!;
                    article.Quantite += ligne.Quantite;

                    _context.MouvementsStock.Add(new MouvementStock
                    {
                        ArticleId = article.ID,
                        Date = DateTime.Now,
                        Type = TypeMouvementStock.EntreeReception,
                        Quantite = ligne.Quantite,
                        QuantiteApres = article.Quantite,
                        Commentaire = $"Annulation BL {bl.Numero}"
                    });

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