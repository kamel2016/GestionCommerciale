/// <summary>Résultat d'un mouvement de stock manuel : réussi, ou refusé avec un message pour l'utilisateur.</summary>
public class ResultatStock
{
    public bool Succes => Message is null;

    /// <summary>Vrai si l'article demandé n'existe pas.</summary>
    public bool Introuvable { get; init; }

    /// <summary>Message d'erreur affichable ; null si le mouvement a été préparé avec succès.</summary>
    public string? Message { get; init; }

    public static ResultatStock Ok() => new();
    public static ResultatStock Echec(string message) => new() { Message = message };
    public static ResultatStock ArticleIntrouvable() => new() { Introuvable = true, Message = "Article introuvable." };
}

public interface IStockService
{
    /// <summary>
    /// Mouvement « technique » (appelé par d'autres services, par ex. les bons de livraison). Lève une exception en cas d'erreur :
    /// ArgumentException si la quantité est nulle ou négative, InvalidOperationException si le stock deviendrait négatif,
    /// NotSupportedException pour un type sans sens (ancien ajustement signé, transfert).
    /// Convention : quantité TOUJOURS positive ; le sens est porté par le type (TypeMouvementStock.Sens()).
    /// Ne sauvegarde pas : l'appelant appelle SaveChanges dans SA transaction.
    /// </summary>
    Task EnregistrerMouvementAsync(
        int articleId,
        TypeMouvementStock type,
        int quantite,           // toujours positive
        string? commentaire = null,
        string? reference = null,
        string? sourceType = null,   // ex. "BonLivraison"
        int? sourceId = null);

    /// <summary>
    /// Mouvement saisi à la main depuis la fiche article. Types autorisés : entrée réception, retour client,
    /// retour fournisseur, ajustement positif, ajustement négatif. La sortie livraison et l'entrée d'annulation de livraison
    /// sont réservées aux bons de livraison.
    /// <para>Quantité : toujours strictement positive pour tous les types ; le sens vient du type ; le motif est obligatoire
    /// pour un ajustement (positif ou négatif).</para>
    /// Refusé (rien n'est modifié) si le stock deviendrait négatif. Renseigne QuantiteAvant et QuantiteApres.
    /// Ne sauvegarde pas : l'appelant appelle SaveChanges.
    /// </summary>
    Task<ResultatStock> EnregistrerMouvementManuelAsync(
        int articleId,
        TypeMouvementStock type,
        int quantite,
        string? motif,
        CancellationToken cancellationToken = default);
}