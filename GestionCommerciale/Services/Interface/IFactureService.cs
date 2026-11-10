using GestionCommerciale.Models;

namespace GestionCommerciale.Services.Interface
{
    public interface IFactureService
    {
        Task<Facture> CreerDepuisBonLivraisonAsync(int blId);
        void Recalculer(Facture f, ParametresSociete p);
        Task<(bool, string?)> ValiderAsync(int id);
        Task<(bool, string?)> AnnulerAsync(int id);
    }
}