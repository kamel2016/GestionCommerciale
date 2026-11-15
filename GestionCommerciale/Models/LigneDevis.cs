using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GestionCommerciale.Models
{
    public class LigneDevis
    {
        public int Id { get; set; }

        public int DevisId { get; set; }
        [ValidateNever]
        public virtual Devis Devis { get; set; } = null!;

        [Required, Display(Name = "Article")]
        public int ArticleId { get; set; }
        [ValidateNever]
        public virtual Article Article { get; set; } = null!;

        // Libellé figé : si l'article est renommé, le devis ne change pas
        [Required, StringLength(200)]
        [Display(Name = "Désignation")]
        public string Designation { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
        [Display(Name = "Quantité")]
        public int Quantite { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "P.U. HT")]
        public decimal PrixUnitaireHT { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100), Display(Name = "Remise %")]
        public decimal TauxRemise { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "TVA %")]
        public decimal TauxTVA { get; set; }

        // Montants calculés et stockés (figés)
        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantBrutHT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantRemise { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantHT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantTVA { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantTTC { get; set; }

        public int Ordre { get; set; }
    }
}