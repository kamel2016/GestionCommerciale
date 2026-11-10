namespace GestionCommerciale.Services.Interface
{
    public interface IFacturePdfService
    {
        Task<byte[]> GenererAsync(int factureId);
    }
}