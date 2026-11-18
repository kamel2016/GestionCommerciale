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
    /// Mouvement « technique » (appelé par d'autres services, par ex. les bons de livraison). Lève une exception en cas d'erreur.
    /// Quantité toujours positive ; le signe est calculé ici selon le type.
    /// L'ajustement d'inventaire n'est PAS géré ici (il est signé) : utiliser <see cref="EnregistrerMouvementManuelAsync"/>.
    /// Ne sauvegarde pas : l'appelant appelle SaveChanges dans SA transaction.
    /// </summary>
    Task EnregistrerMouvementAsync(
        int articleId,
        TypeMouvementStock type,
        int quantite,           // toujours positive
        string? commentaire = null,
        string? reference = null);

    /// <summary>
    /// Mouvement saisi à la main depuis la fiche article. Types autorisés : entrée réception, retour client,
    /// retour fournisseur, ajustement d'inventaire. La sortie livraison est réservée aux bons de livraison.
    /// <para>Quantité : strictement positive, sauf pour l'ajustement où elle est SIGNÉE (+ ajoute, - retire, jamais 0)
    /// et où le motif est obligatoire. Le signe appliqué au stock est calculé ICI, pas par la page.</para>
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