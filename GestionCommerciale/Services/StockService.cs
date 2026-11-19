﻿using GestionCommerciale.Data;
using GestionCommerciale.Models;

/// <summary>
/// Seul endroit qui crée un mouvement de stock et modifie <c>Article.Quantite</c> (via <see cref="AppliquerMouvement"/>).
/// CONVENTION : la quantité d'un mouvement est TOUJOURS positive ; le sens vient du type (<see cref="TypeMouvementStockExtensions.Sens"/>).
/// </summary>
public class StockService : IStockService
{
    private readonly ApplicationDbContext _context;

    public StockService(ApplicationDbContext context) => _context = context;

    public async Task EnregistrerMouvementAsync(
        int articleId, TypeMouvementStock type, int quantite,
        string? commentaire = null, string? reference = null,
        string? sourceType = null, int? sourceId = null)
    {
        var article = await _context.Articles.FindAsync(articleId)
            ?? throw new InvalidOperationException($"Article {articleId} introuvable.");

        AppliquerMouvement(article, type, quantite, commentaire, reference, sourceType, sourceId);
    }

    /// <summary>Quantité maximale par mouvement manuel.</summary>
    public const int QuantiteMaximaleParMouvement = 9_999_999;

    /// <summary>Types qu'un utilisateur peut saisir à la main depuis la fiche article.</summary>
    private static readonly TypeMouvementStock[] TypesManuelsAutorises =
    {
        TypeMouvementStock.EntreeReception,
        TypeMouvementStock.RetourClient,
        TypeMouvementStock.AjustementPositif,
        TypeMouvementStock.AjustementNegatif,
        TypeMouvementStock.RetourFournisseur
    };

    public async Task<ResultatStock> EnregistrerMouvementManuelAsync(
        int articleId, TypeMouvementStock type, int quantite, string? motif,
        CancellationToken cancellationToken = default)
    {
        if (!TypesManuelsAutorises.Contains(type))
        {
            return ResultatStock.Echec(type switch
            {
                TypeMouvementStock.SortieLivraison => "La sortie de livraison est réservée aux bons de livraison : elle ne peut pas être saisie ici.",
                TypeMouvementStock.EntreeAnnulationLivraison => "L'entrée d'annulation de livraison est réservée aux bons de livraison : elle ne peut pas être saisie ici.",
                TypeMouvementStock.AjustementInventaire => "L'ancien ajustement signé n'est plus utilisable : choisissez un ajustement positif ou négatif.",
                _ => "Ce type de mouvement n'est pas autorisé depuis la fiche article."
            });
        }

        motif = string.IsNullOrWhiteSpace(motif) ? null : motif.Trim();
        if (motif is { Length: > 250 })
            return ResultatStock.Echec("Le motif ne doit pas dépasser 250 caractères.");

        if (Math.Abs((long)quantite) > QuantiteMaximaleParMouvement)
            return ResultatStock.Echec($"La quantité ne doit pas dépasser {QuantiteMaximaleParMouvement:N0} par mouvement.");

        if (quantite <= 0)
            return ResultatStock.Echec("La quantité doit être supérieure à zéro.");

        if (type is TypeMouvementStock.AjustementPositif or TypeMouvementStock.AjustementNegatif && motif is null)
            return ResultatStock.Echec("Le motif est obligatoire pour un ajustement d'inventaire.");

        var article = await _context.Articles.FindAsync(new object?[] { articleId }, cancellationToken);
        if (article is null)
            return ResultatStock.ArticleIntrouvable();

        long apres = (long)article.Quantite + type.Sens() * quantite;
        if (apres < 0)
            return ResultatStock.Echec($"Stock insuffisant : disponible {article.Quantite}, le mouvement demandé le rendrait négatif.");
        if (apres > int.MaxValue)
            return ResultatStock.Echec("Quantité trop grande.");

        AppliquerMouvement(article, type, quantite, motif, reference: null, sourceType: null, sourceId: null);
        return ResultatStock.Ok();
    }

    /// <summary>
    /// UNIQUE point de création d'un mouvement : refuse toute quantité inférieure ou égale à zéro et tout type sans sens,
    /// refuse un stock négatif, applique le sens d'après le type, renseigne QuantiteAvant et QuantiteApres.
    /// Ne sauvegarde pas : l'appelant sauvegarde dans SA transaction.
    /// </summary>
    private void AppliquerMouvement(Article article, TypeMouvementStock type, int quantite, string? commentaire,
        string? reference, string? sourceType, int? sourceId)
    {
        if (quantite <= 0)
            throw new ArgumentException("La quantité doit être strictement positive (le sens est porté par le type).", nameof(quantite));

        var sens = type.Sens();
        if (sens == 0)
            throw new NotSupportedException($"Le type de mouvement {type} ne peut pas être utilisé pour un nouveau mouvement.");

        long apres = (long)article.Quantite + sens * quantite;
        if (apres < 0)
            throw new InvalidOperationException($"Stock insuffisant pour « {article.Libelle} » (disponible : {article.Quantite}).");
        if (apres > int.MaxValue)
            throw new InvalidOperationException("Quantité trop grande.");

        var avant = article.Quantite;
        article.Quantite = (int)apres;
        article.DateModification = DateTime.Now;

        _context.MouvementsStock.Add(new MouvementStock
        {
            ArticleId = article.ID,
            Type = type,
            Quantite = quantite,              // toujours positive
            QuantiteAvant = avant,
            QuantiteApres = article.Quantite,
            Commentaire = commentaire,
            Reference = reference,
            SourceType = sourceType,
            SourceId = sourceId,
            Date = DateTime.Now
            // CoutUnitaire non renseigné : aucune source fiable (Article.CMUP n'est calculé nulle part).
        });
    }
}
