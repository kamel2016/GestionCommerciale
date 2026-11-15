namespace GestionCommerciale.Services.Interface
{
    public interface INumerotationService
    {
        Task<string> ProchainNumeroAsync(string typeDocument, DateTime date);
    }
}