using GestionCommerciale.Models;

namespace GestionCommerciale.Services
{
    public interface IRetenueSourceService
    {
        void AppliquerRetenueSource(BonReception bon);
        Task<List<RetenueAReverser>> GetRetenuesMoisAsync(int annee, int mois);
    }
}