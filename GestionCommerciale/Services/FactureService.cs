using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Helpers;
using GestionCommerciale.Models;
using GestionCommerciale.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Services
{
    public class FactureService : IFactureService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISequenceService _seq;
        public FactureService(ApplicationDbContext db, ISequenceService seq)
            => (_db, _seq) = (db, seq);

        public async Task<Facture> CreerDepuisBonLivraisonAsync(int blId)
        {
            var bl = await _db.BonsLivraison
                .Include(b => b.Client)
                .Include(b => b.Lignes).ThenInclude(l => l.Article)
                .FirstAsync(b => b.ID == blId);

            var facture = new Facture
            {
                ClientId = bl.ClientId,
                Client = bl.Client,
                BonLivraisonId = bl.ID,
                Statut = StatutFacture.Brouillon,
                DateEcheance = DateTime.Today.AddDays(30)
            };

            foreach (var l in bl.Lignes)
                facture.Lignes.Add(new LigneFacture
                {
                    ArticleId = l.ArticleId,
                    Designation = l.Article.Libelle,
                    Quantite = l.Quantite,
                    PrixUnitaireHt = l.Article.PrixVenteHT,   // adapte au bon champ
                    TauxTva = l.Article.TauxTVA              // à ajouter sur Article
                });

            var p = await _db.ParametresSociete.FirstAsync();
            Recalculer(facture, p);
            _db.Factures.Add(facture);
            await _db.SaveChangesAsync();
            return facture;
        }

        public void Recalculer(Facture f, ParametresSociete p)
        {
            foreach (var l in f.Lignes)
                l.TotalHt = Math.Round(l.Quantite * l.PrixUnitaireHt * (1 - l.RemisePourcent / 100m), 3);

            var brut = f.Lignes.Sum(l => l.TotalHt);
            f.TotalHt = Math.Round(brut * (1 - f.RemiseGlobalePourcent / 100m), 3);
            var coef = brut == 0 ? 1 : f.TotalHt / brut;

            f.MontantFodec = Math.Round(f.TotalHt * p.TauxFodec / 100m, 3);
            f.TotalTva = Math.Round(f.Lignes.Sum(l => l.TotalHt * coef * l.TauxTva / 100m), 3);
            f.TimbreFiscal = p.TimbreFiscal;
            f.TotalTtc = f.TotalHt + f.MontantFodec + f.TotalTva + f.TimbreFiscal;

            // ---- Retenue à la source ----
            var ttcHorsTimbre = f.TotalTtc - f.TimbreFiscal;

            bool soumise = p.RetenueActive
                           && f.Client != null
                           && f.Client.SoumisRetenueSource;

            f.RetenueSourceApplicable = soumise;

            if (soumise)
            {
                f.TauxRetenueSource = f.Client!.TauxRetenueSource;
                f.MontantRetenueSource = RetenueSourceHelper.Calculer(ttcHorsTimbre, f.TauxRetenueSource);
            }
            else
            {
                f.TauxRetenueSource = 0;
                f.MontantRetenueSource = 0;
            }

            f.NetAPayer = f.TotalTtc - f.MontantRetenueSource;
        }
        public async Task<(bool, string?)> ValiderAsync(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            var f = await _db.Factures
                            .Include(x => x.Client)
                            .Include(x => x.Lignes)
                            .FirstAsync(x => x.Id == id);

            if (f.Statut != StatutFacture.Brouillon) return (false, "Facture déjà validée.");
            if (!f.Lignes.Any()) return (false, "Facture vide.");

            var p = await _db.ParametresSociete.FirstAsync();
            Recalculer(f, p);
            f.Numero = await _seq.ProchainNumeroAsync("FAC", f.Date.Year);
            f.Statut = StatutFacture.Validee;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return (true, null);
        }

        public async Task<(bool, string?)> AnnulerAsync(int id)
        {
            var f = await _db.Factures.FindAsync(id);
            if (f == null) return (false, "Introuvable.");
            if (f.Statut == StatutFacture.Validee)
                return (false, "Une facture validée ne s'annule pas : créez un avoir.");
            f.Statut = StatutFacture.Annulee;
            await _db.SaveChangesAsync();
            return (true, null);
        }
    }
}