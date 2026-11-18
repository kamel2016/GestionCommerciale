using GestionCommerciale.Data;
using GestionCommerciale.Models;

public class StockService : IStockService
{
    private readonly ApplicationDbContext _context;

    public StockService(ApplicationDbContext context) => _context = context;

    public async Task EnregistrerMouvementAsync(
        int articleId, TypeMouvementStock type, int quantite,
        string? commentaire = null, string? reference = null)
    {
        if (quantite <= 0)
            throw new ArgumentException("La quantité doit être positive.", nameof(quantite));

        var article = await _context.Articles.FindAsync(articleId)
            ?? throw new InvalidOperationException($"Article {articleId} introuvable.");

        int delta = type switch
        {
            TypeMouvementStock.EntreeReception => +quantite,
            TypeMouvementStock.RetourClient => +quantite,
            TypeMouvementStock.SortieLivraison => -quantite,
            TypeMouvementStock.RetourFournisseur => -quantite,
            TypeMouvementStock.AjustementInventaire => throw new NotSupportedException(
                "Un ajustement est signé : utiliser EnregistrerMouvementManuelAsync."),
            _ => throw new NotSupportedException($"Type {type} non géré.")
        };

        if (article.Quantite + delta < 0)
            throw new InvalidOperationException($"Stock insuffisant pour « {article.Libelle} » (disponible : {article.Quantite}).");

        AppliquerMouvement(article, type, delta, commentaire, reference);
    }

    /// <summary>Quantité maximale par mouvement manuel (valeur absolue, tous types).</summary>
    public const int QuantiteMaximaleParMouvement = 9_999_999;

    /// <summary>Types qu'un utilisateur peut saisir à la main depuis la fiche article.</summary>
    private static readonly TypeMouvementStock[] TypesManuelsAutorises =
    {
        TypeMouvementStock.EntreeReception,
        TypeMouvementStock.RetourClient,
        TypeMouvementStock.AjustementInventaire,
        TypeMouvementStock.RetourFournisseur
    };

    public async Task<ResultatStock> EnregistrerMouvementManuelAsync(
        int articleId, TypeMouvementStock type, int quantite, string? motif,
        CancellationToken cancellationToken = default)
    {
        if (!TypesManuelsAutorises.Contains(type))
            return ResultatStock.Echec(type == TypeMouvementStock.SortieLivraison
                ? "La sortie de livraison est réservée aux bons de livraison : elle ne peut pas être saisie ici."
                : "Ce type de mouvement n'est pas autorisé depuis la fiche article.");

        motif = string.IsNullOrWhiteSpace(motif) ? null : motif.Trim();
        if (motif is { Length: > 250 })
            return ResultatStock.Echec("Le motif ne doit pas dépasser 250 caractères.");

        // Borne haute (remplace l'ancien [Range(1, 9999999)] de la page), sur la valeur absolue : vaut aussi pour -N.
        if (Math.Abs((long)quantite) > QuantiteMaximaleParMouvement)
            return ResultatStock.Echec($"La quantité ne doit pas dépasser {QuantiteMaximaleParMouvement:N0} par mouvement.");

        int delta;
        if (type == TypeMouvementStock.AjustementInventaire)
        {
            if (quantite == 0)
                return ResultatStock.Echec("La quantité d'un ajustement ne peut pas être nulle.");
            if (motif is null)
                return ResultatStock.Echec("Le motif est obligatoire pour un ajustement d'inventaire.");
            delta = quantite; // signée : + ajoute, - retire
        }
        else
        {
            if (quantite <= 0)
                return ResultatStock.Echec("La quantité doit être supérieure à zéro.");
            delta = type == TypeMouvementStock.RetourFournisseur ? -quantite : +quantite;
        }

        var article = await _context.Articles.FindAsync(new object?[] { articleId }, cancellationToken);
        if (article is null)
            return ResultatStock.ArticleIntrouvable();

        long apres = (long)article.Quantite + delta;
        if (apres < 0)
            return ResultatStock.Echec($"Stock insuffisant : disponible {article.Quantite}, le mouvement demandé le rendrait négatif.");
        if (apres > int.MaxValue)
            return ResultatStock.Echec("Quantité trop grande.");

        AppliquerMouvement(article, type, delta, motif, reference: null);
        return ResultatStock.Ok();
    }

    /// <summary>Modifie le stock de l'article et ajoute la ligne d'historique (sans SaveChanges).</summary>
    private void AppliquerMouvement(Article article, TypeMouvementStock type, int delta, string? commentaire, string? reference)
    {
        var avant = article.Quantite;
        article.Quantite = avant + delta;
        article.DateModification = DateTime.Now;

        _context.MouvementsStock.Add(new MouvementStock
        {
            ArticleId = article.ID,
            Type = type,
            Quantite = delta,                 // signée : + entrée, - sortie
            QuantiteAvant = avant,
            QuantiteApres = article.Quantite,
            Commentaire = commentaire,
            Reference = reference,
            Date = DateTime.Now
        });
        // Pas de SaveChanges ici : l'appelant sauvegarde dans SA transaction
    }
}