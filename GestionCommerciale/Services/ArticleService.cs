using GestionCommerciale.Data;
using GestionCommerciale.Helpers;
using GestionCommerciale.Models;
using GestionCommerciale.Services.Interface;
using Microsoft.Data.SqlClient;
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

        // ------------------------------------------------------------------ création et modification (S4 étape 3)

        public async Task<ResultatArticle> CreerAsync(Article article, CancellationToken cancellationToken = default)
        {
            ArticleValidation.Normaliser(article);
            var resultat = new ResultatArticle { Reference = article.Reference };

            await ControlerAsync(article, idExclu: null, resultat, cancellationToken);
            if (!resultat.Succes)
                return resultat;

            // Nouvel objet : seuls les champs du formulaire sont repris. Le stock (0), le CMUP (0), l'Id, la version
            // et la date de création ne viennent JAMAIS de la saisie (valeurs par défaut du modèle / date du jour).
            var nouveau = new Article
            {
                Reference = article.Reference,
                CodeBarre = article.CodeBarre,
                Libelle = article.Libelle,
                CategoryId = article.CategoryId,
                Unite = article.Unite,
                PrixAchatHT = article.PrixAchatHT,
                PrixVenteHT = article.PrixVenteHT,
                PrixVenteTTC = article.PrixVenteTTC,   // libre : aucun calcul (décision C)
                TauxTVA = article.TauxTVA,
                Fodec = article.Fodec,
                StockMin = article.StockMin,
                StockMax = article.StockMax,
                EstActif = article.EstActif,
                Description = article.Description,
                Marque = article.Marque,
                Modele = article.Modele,
                Couleur = article.Couleur,
                Taille = article.Taille,
                Poids = article.Poids,
                DateCreation = DateTime.Now
            };

            try
            {
                _context.Articles.Add(nouveau);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (TraduireErreurDeBase(ex, resultat))
            {
                _context.Entry(nouveau).State = EntityState.Detached;
                return resultat;
            }

            article.ID = nouveau.ID;
            return resultat;
        }

        public async Task<ResultatArticle> ModifierAsync(int id, Article saisie, CancellationToken cancellationToken = default)
        {
            ArticleValidation.Normaliser(saisie);
            var resultat = new ResultatArticle { Reference = saisie.Reference };

            var article = await _context.Articles.FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
            if (article == null)
            {
                resultat.Introuvable = true;
                resultat.Erreurs.Add((string.Empty, "Article introuvable."));
                return resultat;
            }

            await ControlerAsync(saisie, idExclu: id, resultat, cancellationToken);
            if (!resultat.Succes)
                return resultat;

            // Seuls les champs du formulaire sont copiés : stock, CMUP, fournisseur principal, devise, n° de série,
            // version et date de création ne changent jamais, quoi que contienne la saisie.
            article.Reference = saisie.Reference;
            article.CodeBarre = saisie.CodeBarre;
            article.Libelle = saisie.Libelle;
            article.CategoryId = saisie.CategoryId;
            article.Unite = saisie.Unite;
            article.PrixAchatHT = saisie.PrixAchatHT;
            article.PrixVenteHT = saisie.PrixVenteHT;
            article.PrixVenteTTC = saisie.PrixVenteTTC;   // libre : aucun calcul (décision C)
            article.TauxTVA = saisie.TauxTVA;
            article.Fodec = saisie.Fodec;
            article.StockMin = saisie.StockMin;
            article.StockMax = saisie.StockMax;
            article.EstActif = saisie.EstActif;
            article.Description = saisie.Description;
            article.Marque = saisie.Marque;
            article.Modele = saisie.Modele;
            article.Couleur = saisie.Couleur;
            article.Taille = saisie.Taille;
            article.Poids = saisie.Poids;
            article.DateModification = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (TraduireErreurDeBase(ex, resultat))
            {
                // Course : la base a refusé après nos contrôles. On abandonne la modification en mémoire.
                _context.Entry(article).State = EntityState.Detached;
                return resultat;
            }

            return resultat;
        }

        /// <summary>Règles sans base (ArticleValidation), puis unicité de la référence et du code-barres, puis existence de la catégorie.</summary>
        private async Task ControlerAsync(Article article, int? idExclu, ResultatArticle resultat, CancellationToken cancellationToken)
        {
            resultat.Erreurs.AddRange(ArticleValidation.Valider(article));

            if (article.Reference.Length > 0
                && await UnAutreArticleALaReferenceAsync(article.Reference, idExclu, cancellationToken))
                resultat.Erreurs.Add((nameof(Article.Reference), "Cette référence existe déjà."));

            if (article.CodeBarre != null
                && await UnAutreArticleALeCodeBarreAsync(article.CodeBarre, idExclu, cancellationToken))
                resultat.Erreurs.Add((nameof(Article.CodeBarre), "Ce code-barres existe déjà."));

            if (article.CategoryId.HasValue
                && !await _context.Categories.AnyAsync(c => c.Id == article.CategoryId.Value, cancellationToken))
                resultat.Erreurs.Add((nameof(Article.CategoryId), "Catégorie introuvable."));
        }

        // Insensible à la casse et aux espaces en début/fin (même résultat que la collation de SQL Server, mais explicite).
        private Task<bool> UnAutreArticleALaReferenceAsync(string reference, int? idExclu, CancellationToken cancellationToken)
        {
            var cle = reference.Trim().ToLower();
            return _context.Articles.AnyAsync(
                a => a.Reference.Trim().ToLower() == cle && (idExclu == null || a.ID != idExclu), cancellationToken);
        }

        private Task<bool> UnAutreArticleALeCodeBarreAsync(string codeBarre, int? idExclu, CancellationToken cancellationToken)
        {
            var cle = codeBarre.Trim().ToLower();
            return _context.Articles.AnyAsync(
                a => a.CodeBarre != null && a.CodeBarre.Trim().ToLower() == cle && (idExclu == null || a.ID != idExclu), cancellationToken);
        }

        /// <summary>
        /// Course : un doublon (index unique de la référence ou du code-barres) ou une catégorie supprimée (clé étrangère)
        /// survenu entre nos contrôles et l'enregistrement. Ajoute l'erreur au résultat, sur le BON champ, et renvoie vrai ;
        /// toute autre erreur de base remonte telle quelle.
        /// </summary>
        private static bool TraduireErreurDeBase(DbUpdateException exception, ResultatArticle resultat)
        {
            switch (ChampEnViolationIndexUnique(exception))
            {
                case nameof(Article.Reference):
                    resultat.Erreurs.Add((nameof(Article.Reference), "Cette référence existe déjà."));
                    return true;
                case nameof(Article.CodeBarre):
                    resultat.Erreurs.Add((nameof(Article.CodeBarre), "Ce code-barres existe déjà."));
                    return true;
            }

            if (ClientService.EstViolationCleEtrangere(exception))   // dette connue : à déplacer dans un helper commun
            {
                resultat.Erreurs.Add((nameof(Article.CategoryId), "Catégorie introuvable."));
                return true;
            }

            return false;
        }

        public const string NomIndexReference = "IX_Articles_Reference";
        public const string NomIndexCodeBarre = "IX_Articles_CodeBarre";

        /// <summary>
        /// Champ concerné quand l'erreur vient d'un index unique de la table Articles : « Reference » ou « CodeBarre » ;
        /// null pour toute autre erreur (clé primaire, clé étrangère, autre index...).
        /// SQL Server : erreur 2601 (index unique) ou 2627 (contrainte unique) ET nom de l'index dans le message.
        /// SQLite des tests : « UNIQUE constraint failed: Articles.Reference » ou « ...Articles.CodeBarre ».
        /// </summary>
        public static string? ChampEnViolationIndexUnique(DbUpdateException exception)
        {
            for (Exception? e = exception; e != null; e = e.InnerException)
            {
                if (e is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                {
                    if (sql.Message.Contains(NomIndexReference, StringComparison.OrdinalIgnoreCase))
                        return nameof(Article.Reference);
                    if (sql.Message.Contains(NomIndexCodeBarre, StringComparison.OrdinalIgnoreCase))
                        return nameof(Article.CodeBarre);
                }

                if (e.GetType().Name == "SqliteException")
                {
                    if (e.Message.Contains("UNIQUE constraint failed: Articles.Reference", StringComparison.OrdinalIgnoreCase))
                        return nameof(Article.Reference);
                    if (e.Message.Contains("UNIQUE constraint failed: Articles.CodeBarre", StringComparison.OrdinalIgnoreCase))
                        return nameof(Article.CodeBarre);
                }
            }
            return null;
        }

        public static bool EstViolationIndexReference(DbUpdateException exception)
            => ChampEnViolationIndexUnique(exception) == nameof(Article.Reference);

        public static bool EstViolationIndexCodeBarre(DbUpdateException exception)
            => ChampEnViolationIndexUnique(exception) == nameof(Article.CodeBarre);

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
