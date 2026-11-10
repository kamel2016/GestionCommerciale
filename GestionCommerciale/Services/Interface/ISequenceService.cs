namespace GestionCommerciale.Services.Interface
{
    public interface ISequenceService
    {
        Task<string> ProchainNumeroAsync(string type, int exercice);
    }
}