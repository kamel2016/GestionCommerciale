namespace GestionCommerciale.Services.Interface
{
    public interface INumerotationService
    {
        Task<string> ProchainNumeroBonLivraisonAsync();
    }
}