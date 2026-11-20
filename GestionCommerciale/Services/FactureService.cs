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

        /// <summary>
        /// Crée une facture BROUILLON depuis un bon de livraison VALIDÉ, non encore facturé. Chaque ligne reprend l'article et la quantité
        /// du BL, et le prix, la TVA et la remise de la LIGNE DE COMMANDE liée (figés à la création de la commande) : jamais ceux de
        /// l'article du jour. La remise globale de la commande est reprise. Lève InvalidOperationException avec un message clair si le BL
        /// est introuvable, non validé, déjà facturé, vide, ou contient une ligne non liée à une ligne de commande.
        /// </summary>
        public async Task<Facture> CreerDepuisBonLivraisonAsync(int blId)
        {
            var bl = await _db.BonsLivraison
                .Include(b => b.Client)
                .Include(b => b.CommandeClient)
                .Include(b => b.Lignes).ThenInclude(l => l.Article)
                .Include(b => b.Lignes).ThenInclude(l => l.LigneCommandeClient)
                .FirstOrDefaultAsync(b => b.ID == blId)
                ?? throw new InvalidOperationException("Bon de livraison introuvable.");

            if (bl.Statut == StatutBonLivraison.Facture)
                throw new InvalidOperationException("Ce bon de livraison est déjà facturé.");
            if (bl.Statut != StatutBonLivraison.Valide)
                throw new InvalidOperationException($"Seul un bon de livraison validé peut être facturé (statut actuel : {bl.Statut}).");
            if (!bl.Lignes.Any())
                throw new InvalidOperationException("Le bon de livraison ne contient aucune ligne.");
            if (bl.Lignes.Any(l => l.LigneCommandeClient == null))
                throw new InvalidOperationException("Le bon de livraison contient une ligne non liée à une ligne de commande : facturation impossible.");

            var existante = await _db.Factures
                .Where(f => f.BonLivraisonId == bl.ID && f.Statut != StatutFacture.Annulee)
                .Select(f => new { f.Id, f.Numero })
                .FirstOrDefaultAsync();
            if (existante != null)
                throw new InvalidOperationException(
                    $"Ce bon de livraison est déjà facturé ({(existante.Numero != null ? "facture " + existante.Numero : "facture brouillon n° " + existante.Id)}).");

            var facture = new Facture
            {
                ClientId = bl.ClientId,
                Client = bl.Client,
                BonLivraisonId = bl.ID,
                Statut = StatutFacture.Brouillon,
                DateEcheance = DateTime.Today.AddDays(30),
                RegleCalcul = 2,
                RemiseGlobalePourcent = bl.CommandeClient?.TauxRemiseGlobale ?? 0m
            };

            foreach (var l in bl.Lignes)
                facture.Lignes.Add(new LigneFacture
                {
                    ArticleId = l.ArticleId,
                    Designation = l.Article.Libelle,
                    Quantite = l.Quantite,
                    PrixUnitaireHt = l.LigneCommandeClient!.PrixUnitaire,   // prix figé de la commande
                    TauxTva = l.LigneCommandeClient.TauxTVA,                // TVA figée de la commande
                    RemisePourcent = 0m,                                    // pas de remise par ligne sur la commande
                    SoumiseFodec = l.Article.Fodec                          // marquage de l'article, copié puis figé sur la ligne
                });

            var p = await _db.ParametresSociete.FirstAsync();
            Recalculer(facture, p);
            _db.Factures.Add(facture);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (EstViolationUniciteBonLivraison(ex))
            {
                // Course : une autre facture non annulée a été créée pour ce BL entre le contrôle et l'enregistrement (index unique).
                throw new InvalidOperationException("Ce bon de livraison est déjà facturé (une facture vient d'être créée en même temps).", ex);
            }
            return facture;
        }

        /// <summary>Vrai si l'exception vient de l'index unique filtré IX_Factures_BonLivraisonId (SQL Server 2601 ou 2627, ou SQLite).</summary>
        private static bool EstViolationUniciteBonLivraison(DbUpdateException ex)
        {
            if (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
                return (sql.Number == 2601 || sql.Number == 2627) && sql.Message.Contains("IX_Factures_BonLivraisonId");
            return (ex.InnerException?.Message ?? ex.Message).Contains("UNIQUE constraint failed: Factures.BonLivraisonId");
        }

        /// <summary>
        /// Calcule les montants d'un brouillon de règle 2 par CalculTotaux : ligne = Quantité x Prix x (1 - remise ligne), arrondie au millime ;
        /// puis remise globale, FODEC sur les lignes marquées, TVA par taux (base selon le paramètre société), timbre : un seul mode
        /// d'arrondi (au millime, demi loin de zéro). Ne fait RIEN pour une facture de règle 1 (créée avant S10) ni pour une facture validée :
        /// leurs montants ne sont jamais recalculés, même si les paramètres société changent.
        /// </summary>
        public void Recalculer(Facture f, ParametresSociete p)
        {
            if (f.RegleCalcul < 2 || f.Statut == StatutFacture.Validee) return;

            foreach (var l in f.Lignes)
                l.TotalHt = CalculTotaux.Arrondir(l.Quantite * l.PrixUnitaireHt * (1 - l.RemisePourcent / 100m));

            var totaux = CalculTotaux.CalculerTotaux(
                f.Lignes.Select(l => (l.TotalHt, l.TauxTva, l.SoumiseFodec)),
                f.RemiseGlobalePourcent, p.TauxFodec, p.BaseTvaInclutFodec, p.TimbreFiscal);

            f.TotalHt = totaux.TotalHTNet;
            f.MontantFodec = totaux.MontantFodec;
            f.TotalTva = totaux.TotalTVA;
            f.TimbreFiscal = totaux.Timbre;
            f.TotalTtc = totaux.TotalTTC;

            // ---- Retenue à la source (inchangée : hors périmètre de S10, voir S11) ----
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
            Recalculer(f, p);   // sans effet pour un brouillon de règle 1 : ses montants restent ceux enregistrés
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
