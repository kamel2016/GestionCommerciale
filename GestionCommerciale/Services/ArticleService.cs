using GestionCommerciale.Data;
using GestionCommerciale.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Services
{
    public class ArticleService : IArticleService
    {
        private readonly ApplicationDbContext _context;

        public ArticleService(ApplicationDbContext context) => _context = context;

        /// <summary>Message de refus affiché à l'utilisateur (page de confirmation, liste, fiche).</summary>
        public static string MessageSuppressionRefusee(string reference, UtilisationArticle utilisation)
        {
            string message;

            if (utilisation.EstUtilise)
            {
                var details = string.Join(", ", utilisation.Elements.Select(e => $"{e.Nombre} {e.Libelle}"));
                message = $"Impossible de supprimer l'article {reference} : il est utilisé ({details}). "
                        + "Vous pouvez le désactiver à la place.";

                // Seuls des tarifs fournisseur : ils se retirent depuis la fiche article, après quoi la suppression redevient possible.
                if (!utilisation.UtiliseAutrementQueParTarifs)
                    message += " Pour le supprimer, retirez d'abord ses fournisseurs associés dans la fiche article.";
            }
            else
            {
                message = $"Impossible de supprimer l'article {reference}.";
            }

            // Règle du stock : un article dont le stock n'est pas à zéro ne se supprime pas (le stock disparaîtrait avec lui).
            if (utilisation.StockNonNul)
            {
                var unites = Math.Abs(utilisation.Stock) == 1 ? "unité" : "unités";
                message += $" Stock non nul ({utilisation.Stock} {unites}) : ajustez le stock à 0 ou désactivez l'article.";
            }

            return message;
        }

        public async Task<UtilisationArticle> ObtenirUtilisationAsync(int articleId, CancellationToken cancellationToken = default)
        {
            // Une entrée par table qui référence Articles (clés étrangères de la base).
            var comptes = new (string Libelle, int Nombre)[]
            {
                (UtilisationLibelles.MouvementsStock, await _context.MouvementsStock.CountAsync(x => x.ArticleId == articleId, cancellationToken)),
                (UtilisationLibelles.LignesBonLivraison, await _context.LignesBonLivraison.CountAsync(x => x.ArticleId == articleId, cancellationToken)),
                (UtilisationLibelles.LignesFacture, await _context.LignesFacture.CountAsync(x => x.ArticleId == articleId, cancellationToken)),
                (UtilisationLibelles.LignesBonReception, await _context.LignesBonReception.CountAsync(x => x.ArticleId == articleId, cancellationToken)),
                (UtilisationLibelles.LignesCommandeFournisseur, await _context.LignesCommandeFournisseur.CountAsync(x => x.ArticleId == articleId, cancellationToken)),
                (UtilisationLibelles.LignesCommandeClient, await _context.LignesCommandeClient.CountAsync(x => x.ArticleId == articleId, cancellationToken)),
                (UtilisationLibelles.LignesDevis, await _context.LigneDevis.CountAsync(x => x.ArticleId == articleId, cancellationToken)),
                (UtilisationLibelles.TarifsFournisseur, await _context.ArticleFournisseurs.CountAsync(x => x.ArticleId == articleId, cancellationToken)),
            };

            var stock = await _context.Articles.Where(a => a.ID == articleId)
                .Select(a => a.Quantite).FirstOrDefaultAsync(cancellationToken);

            return new UtilisationArticle { Elements = comptes.Where(c => c.Nombre > 0).ToList(), Stock = stock };
        }

        public async Task<ResultatArticle> SupprimerAsync(int id, CancellationToken cancellationToken = default)
        {
            var resultat = new ResultatArticle();

            var article = await _context.Articles.FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
            if (article == null)
            {
                resultat.Introuvable = true;
                resultat.Erreurs.Add((string.Empty, "Article introuvable."));
                return resultat;
            }

            resultat.Reference = article.Reference;

            // Contrôle refait au moment de la suppression, quoi que la page ait affiché avant.
            var utilisation = await ObtenirUtilisationAsync(id, cancellationToken);
            if (utilisation.SuppressionImpossible)
            {
                resultat.Erreurs.Add((string.Empty, MessageSuppressionRefusee(article.Reference, utilisation)));
                return resultat;
            }

            try
            {
                _context.Articles.Remove(article);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ClientService.EstViolationCleEtrangere(ex))
            {
                // Une ligne a été créée entre le contrôle et la suppression et la base a refusé : rien n'est supprimé.
                // (Pour les tables en CASCADE, la base ne refuserait pas : c'est le contrôle ci-dessus qui protège.)
                _context.Entry(article).State = EntityState.Unchanged;
                var apres = await ObtenirUtilisationAsync(id, cancellationToken);
                resultat.Erreurs.Add((string.Empty, apres.SuppressionImpossible
                    ? MessageSuppressionRefusee(article.Reference, apres)
                    : $"Impossible de supprimer l'article {article.Reference} : il est utilisé ailleurs. Vous pouvez le désactiver à la place."));
            }
            // Toute autre DbUpdateException remonte telle quelle.

            return resultat;
        }
    }
}
