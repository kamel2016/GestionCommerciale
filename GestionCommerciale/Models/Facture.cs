using GestionCommerciale.Enums;
using GestionCommerciale.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCommerciale.Models
{
    public class Facture
    {
        public int Id { get; set; }
        public string? Numero { get; set; }              // null tant que brouillon
        public DateTime Date { get; set; } = DateTime.Today;
        public DateTime? DateEcheance { get; set; }
        public int? ClientId { get; set; }
        public Client Client { get; set; } = null!;
        public int? BonLivraisonId { get; set; }
        public StatutFacture Statut { get; set; }

        /// <summary>
        /// Règle de calcul des montants. 1 = factures créées avant S10 (prix de l'article du jour, arrondi ToEven, colonnes à 2 décimales) :
        /// leurs montants ne sont JAMAIS recalculés. 2 = règle actuelle (lignes copiées du BL et de la commande liée, CalculTotaux).
        /// Une facture neuve est toujours de règle 2 ; les lignes existantes en base reçoivent 1 par la migration.
        /// </summary>
        public int RegleCalcul { get; set; } = 2;

        public decimal RemiseGlobalePourcent { get; set; }
        public decimal TotalHt { get; set; }
        public decimal MontantFodec { get; set; }
        public decimal TotalTva { get; set; }
        public decimal TimbreFiscal { get; set; }
        public decimal TotalTtc { get; set; }
        public bool RetenueSourceApplicable { get; set; }
        public decimal MontantRetenueSource { get; set; }
        public decimal NetAPayer { get; set; }

        public List<LigneFacture> Lignes { get; set; } = new();
        [Timestamp] public byte[]? RowVersion { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Taux RS (%)")]
        [Range(0, 100)]
        public decimal TauxRetenueSource { get; set; }

        [NotMapped]
        public decimal NetAEncaisser => NetAPayer - MontantRetenueSource;

        public ICollection<Reglement> Reglements { get; set; } = new List<Reglement>();

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantRegle { get; set; }

        [NotMapped]
        public decimal ResteAPayer => Math.Max(0, NetAPayer - MontantRetenueSource - MontantRegle);

        public StatutPaiement StatutPaiement { get; set; } = StatutPaiement.Impayee;
    }
}