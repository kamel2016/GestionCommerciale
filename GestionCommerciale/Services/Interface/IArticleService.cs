namespace GestionCommerciale.Services.Interface
{
    /// <summary>Résultat d'une opération sur un article (suppression pour l'instant).</summary>
    public class ResultatArticle
    {
        public bool Succes => Erreurs.Count == 0;

        /// <summary>Vrai si l'article demandé n'existe pas.</summary>
        public bool Introuvable { get; set; }

        /// <summary>Référence de l'article concerné (renseignée par la suppression, pour les messages).</summary>
        public string? Reference { get; set; }

        /// <summary>Couples (nom du champ, message). Champ vide = erreur générale.</summary>
        public List<(string Champ, string Message)> Erreurs { get; } = new();
    }

    /// <summary>Où un article est utilisé : une ligne par type de document ou d'historique, avec le nombre d'occurrences (> 0 seulement).</summary>
    public class UtilisationArticle
    {
        public IReadOnlyList<(string Libelle, int Nombre)> Elements { get; init; } = Array.Empty<(string, int)>();

        /// <summary>Vrai si l'article est utilisé quelque part : il ne peut alors pas être supprimé.</summary>
        public bool EstUtilise => Elements.Count > 0;

        /// <summary>Quantité en stock de l'article au moment du contrôle.</summary>
        public int Stock { get; init; }

        /// <summary>Vrai si le stock n'est pas à zéro (positif ou négatif).</summary>
        public bool StockNonNul => Stock != 0;

        /// <summary>Vrai si la suppression doit être refusée : article utilisé OU stock non nul.</summary>
        public bool SuppressionImpossible => EstUtilise || StockNonNul;

        /// <summary>Vrai si l'article est utilisé ailleurs que dans ses tarifs fournisseur (qui, eux, peuvent être retirés de la fiche).</summary>
        public bool UtiliseAutrementQueParTarifs => Elements.Any(e => e.Libelle != UtilisationLibelles.TarifsFournisseur);
    }

    public static class UtilisationLibelles
    {
        public const string MouvementsStock = "mouvement(s) de stock";
        public const string LignesBonLivraison = "ligne(s) de bon de livraison";
        public const string LignesFacture = "ligne(s) de facture";
        public const string LignesBonReception = "ligne(s) de bon de réception";
        public const string LignesCommandeFournisseur = "ligne(s) de commande fournisseur";
        public const string LignesCommandeClient = "ligne(s) de commande client";
        public const string LignesDevis = "ligne(s) de devis";
        public const string TarifsFournisseur = "tarif(s) fournisseur";
    }

    /// <summary>Contenu de la fiche article (page Détails) : données réelles, lues en base.</summary>
    public class FicheArticle
    {
        public Models.Article Article { get; init; } = null!;

        /// <summary>Nom de la catégorie, ou null si l'article n'en a pas.</summary>
        public string? CategorieNom => Article.Category?.Name;

        /// <summary>Tarifs par fournisseur, triés par nom de fournisseur.</summary>
        public IReadOnlyList<Models.ArticleFournisseur> Tarifs { get; init; } = Array.Empty<Models.ArticleFournisseur>();

        /// <summary>Derniers mouvements de stock, du plus récent au plus ancien (limités à <see cref="ArticleService.NombreMouvementsAffiches"/>).</summary>
        public IReadOnlyList<Models.MouvementStock> DerniersMouvements { get; init; } = Array.Empty<Models.MouvementStock>();

        /// <summary>Nombre TOTAL de mouvements de stock de l'article (pas seulement ceux affichés).</summary>
        public int NombreMouvements { get; init; }

        /// <summary>Date du mouvement de stock le plus récent, ou null s'il n'y en a aucun.</summary>
        public DateTime? DernierMouvement { get; init; }
    }

    public interface IArticleService
    {
        /// <summary>
        /// Charge la fiche réelle d'un article : l'article et sa catégorie, ses tarifs par fournisseur et ses derniers mouvements
        /// de stock. Null si l'article n'existe pas. Lecture seule. Pas de stock « réservé » ni « en commande » : ces notions ne sont
        /// pas encore définies (décision métier en attente), elles ne sont donc pas calculées ici.
        /// </summary>
        Task<FicheArticle?> ObtenirFicheAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Supprime physiquement un article, seulement s'il n'est utilisé nulle part : aucun mouvement de stock,
        /// aucune ligne de BL, facture, bon de réception, commande (client ou fournisseur) ou devis, aucun tarif fournisseur.
        /// Le contrôle est refait ICI, au moment de la suppression, quoi que la page ait affiché avant.
        /// En cas de refus, rien n'est supprimé et le résultat contient un message explicite.
        /// </summary>
        Task<ResultatArticle> SupprimerAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Nettoie (espaces retirés, facultatifs vides = NULL), valide, vérifie l'unicité (référence et code-barres)
        /// et la catégorie, puis enregistre un NOUVEL article. Seuls les champs du formulaire sont repris : le stock
        /// démarre à 0, le CMUP à 0, l'Id, la version et la date de création ne viennent jamais de la saisie.
        /// Le prix TTC est libre : aucun calcul, aucune cohérence imposée avec le HT ou la TVA.
        /// En cas d'erreur (y compris doublon ou catégorie supprimée entre le contrôle et l'enregistrement),
        /// rien n'est enregistré et le résultat contient un message par champ ; la méthode ne lève pas d'exception pour ces cas.
        /// </summary>
        Task<ResultatArticle> CreerAsync(Models.Article article, CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifie un article existant. Seuls les champs modifiables de la fiche sont copiés : le stock, le CMUP,
        /// le fournisseur principal, la devise, le n° de série, la version et la date de création ne changent jamais,
        /// quoi que contienne <paramref name="saisie"/>. Mêmes règles que <see cref="CreerAsync"/> ; l'article lui-même
        /// est exclu des contrôles d'unicité. Introuvable = résultat avec <c>Introuvable</c>.
        /// </summary>
        Task<ResultatArticle> ModifierAsync(int id, Models.Article saisie, CancellationToken cancellationToken = default);

        /// <summary>Compte où l'article est utilisé (pour la page de confirmation et pour les messages de refus).</summary>
        Task<UtilisationArticle> ObtenirUtilisationAsync(int articleId, CancellationToken cancellationToken = default);
    }
}
