using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestionCommerciale.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GestionCommerciale.Models
{
    public class LigneCommandeClient
    {
        public int Id { get; set; }

        [Required]
        public int CommandeClientId { get; set; }

        [Required]
        [Display(Name = "Article")]
        public int ArticleId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
        [Display(Name = "Quantité")]
        public int Quantite { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Prix unitaire HT")]
        [DisplayFormat(DataFormatString = "{0:N3}", ApplyFormatInEditMode = false)]
        public decimal PrixUnitaire { get; set; }

        /// <summary>Taux de TVA en % (ex : 19). Copié depuis l'article au moment de la commande.</summary>
        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        [Display(Name = "TVA (%)")]
        public decimal TauxTVA { get; set; }

        /// <summary>Montant HT de la ligne = Quantité × Prix unitaire.</summary>
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Montant HT")]
        [DisplayFormat(DataFormatString = "{0:N3}", ApplyFormatInEditMode = false)]
        public decimal MontantLigne { get; set; }

        public int QuantiteLivree { get; set; }

        [NotMapped]
        public int QuantiteRestante => Quantite - QuantiteLivree;

        // ===== Navigation =====
        [ValidateNever]
        public virtual CommandeClient CommandeClient { get; set; } = null!;

        [ValidateNever]
        public virtual Article Article { get; set; } = null!;

        // ===== Calcul =====
        /// <summary>Recalcule le montant HT de la ligne.</summary>
        public void CalculerMontant()
        {
            MontantLigne = CalculTotaux.Arrondir(Quantite * PrixUnitaire);
        }
    }
}