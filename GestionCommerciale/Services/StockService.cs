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
            TypeMouvementStock.AjustementInventaire => quantite, // voir note plus bas
            _ => throw new NotSupportedException($"Type {type} non géré.")
        };

        if (article.Quantite + delta < 0)
            throw new InvalidOperationException($"Stock insuffisant pour « {article.Libelle} » (disponible : {article.Quantite}).");

        article.Quantite += delta;

        _context.MouvementsStock.Add(new MouvementStock
        {
            ArticleId = articleId,
            Type = type,
            Quantite = delta,                 // signée : + entrée, - sortie
            QuantiteApres = article.Quantite,
            Commentaire = commentaire,
            Reference = reference,
            Date = DateTime.Now
        });

        // Pas de SaveChanges ici : l'appelant sauvegarde dans SA transaction
    }
}