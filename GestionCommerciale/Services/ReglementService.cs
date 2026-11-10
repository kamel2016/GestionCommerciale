using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Models.Enums;
using GestionCommerciale.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Services
{
    public class ReglementService : IReglementService
    {
        private readonly ApplicationDbContext _db;

        public ReglementService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Reglement>> GetByFactureAsync(int factureId)
        {
            return await _db.Reglements
                .AsNoTracking()
                .Where(r => r.FactureId == factureId)
                .OrderByDescending(r => r.DateReglement)
                .ThenByDescending(r => r.Id)
                .ToListAsync();
        }

        public async Task<Reglement> AjouterAsync(Reglement reglement)
        {
            var facture = await _db.Factures
                .FirstOrDefaultAsync(f => f.Id == reglement.FactureId)
                ?? throw new InvalidOperationException("Facture introuvable.");

            //var reste = facture.NetAPayer - facture.MontantRegle;
            var reste = facture.NetAPayer - facture.MontantRetenueSource - facture.MontantRegle;

            if (reste <= 0)
                throw new InvalidOperationException("Cette facture est déjà entièrement réglée.");


            if (reglement.Montant > reste)
                throw new InvalidOperationException(
                    $"Le montant dépasse le reste à payer ({reste:N3} DT).");
            _db.Reglements.Add(reglement);
            await _db.SaveChangesAsync();

            await RecalculerFactureAsync(reglement.FactureId);
            return reglement;
        }

        public async Task SupprimerAsync(int reglementId)
        {
            var reglement = await _db.Reglements.FindAsync(reglementId)
                ?? throw new InvalidOperationException("Règlement introuvable.");

            var factureId = reglement.FactureId;

            _db.Reglements.Remove(reglement);
            await _db.SaveChangesAsync();

            await RecalculerFactureAsync(factureId);
        }

        public async Task RecalculerFactureAsync(int factureId)
        {
            var facture = await _db.Factures.FirstOrDefaultAsync(f => f.Id == factureId);
            if (facture == null) return;

            var totalRegle = await _db.Reglements
                .Where(r => r.FactureId == factureId)
                .SumAsync(r => (decimal?)r.Montant) ?? 0m;

            facture.MontantRegle = totalRegle;

            //if (totalRegle <= 0)
            //    facture.StatutPaiement = StatutPaiement.Impayee;
            //else if (totalRegle >= facture.NetAPayer)
            //    facture.StatutPaiement = StatutPaiement.Payee;
            //else
            //    facture.StatutPaiement = StatutPaiement.PartiellementPayee;

            facture.MontantRegle = await _db.Reglements
                    .Where(r => r.FactureId == factureId)
                    .SumAsync(r => (decimal?)r.Montant) ?? 0m;

            // La RS compte comme une part déjà acquittée
            var totalAcquitte = facture.MontantRegle + facture.MontantRetenueSource;

            if (facture.MontantRegle <= 0)
                facture.StatutPaiement = StatutPaiement.Impayee;
            else if (facture.MontantRegle + facture.MontantRetenueSource >= facture.NetAPayer)
                facture.StatutPaiement = StatutPaiement.Payee;
            else
                facture.StatutPaiement = StatutPaiement.PartiellementPayee;

            await _db.SaveChangesAsync();
        }
    }
}