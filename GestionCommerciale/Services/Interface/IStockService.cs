public interface IStockService
{
    Task EnregistrerMouvementAsync(
        int articleId,
        TypeMouvementStock type,
        int quantite,           // toujours positive
        string? commentaire = null,
        string? reference = null);
}