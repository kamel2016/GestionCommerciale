using GestionCommerciale.Models;

namespace GestionCommerciale.Services.Interface
{
    public interface IReglementService
    {
        Task<List<Reglement>> GetByFactureAsync(int factureId);
        Task<Reglement> AjouterAsync(Reglement reglement);
        Task SupprimerAsync(int reglementId);
        Task RecalculerFactureAsync(int factureId);
    }
}