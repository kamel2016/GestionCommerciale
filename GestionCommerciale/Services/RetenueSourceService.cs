using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Services
{
    public class RetenueSourceService : IRetenueSourceService
    {
        private readonly ApplicationDbContext _context;

        public RetenueSourceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AppliquerRetenueSource(BonReception bon)
        {
            var f = bon.Fournisseur;

            if (f is null || !f.SoumisRetenueSource || f.TauxRetenueSource is null or 0)
            {
                bon.TauxRetenueSource = 0;
                bon.MontantRetenueSource = 0;
                return;
            }

            bon.TauxRetenueSource = f.TauxRetenueSource.Value;
            bon.MontantRetenueSource = Math.Round(
                bon.MontantHT * bon.TauxRetenueSource / 100m, 3, MidpointRounding.AwayFromZero);
        }

        public async Task<List<RetenueAReverser>> GetRetenuesMoisAsync(int annee, int mois)
        {
            return await _context.BonsReception
                .AsNoTracking()
                .Where(b => b.DateReception.Year == annee
                         && b.DateReception.Month == mois
                         && b.MontantRetenueSource > 0)
                .Select(b => new RetenueAReverser
                {
                    Date = b.DateReception,
                    Numero = b.NumeroInterne,
                    Fournisseur = b.Fournisseur.Nom,
                    MatriculeFiscale = b.Fournisseur.MatriculeFiscale,
                    BaseHt = b.MontantHT,
                    Taux = b.TauxRetenueSource,
                    Montant = b.MontantRetenueSource
                })
                .ToListAsync();
        }
    }

    public class RetenueAReverser
    {
        public DateTime Date { get; set; }
        public string? Numero { get; set; }
        public string Fournisseur { get; set; } = "";
        public string? MatriculeFiscale { get; set; }
        public decimal BaseHt { get; set; }
        public decimal Taux { get; set; }
        public decimal Montant { get; set; }
    }
}