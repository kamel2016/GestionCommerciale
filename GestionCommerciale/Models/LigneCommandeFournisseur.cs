using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Models
{
    public class LigneCommandeFournisseur
    {
        public int Id { get; set; }

        [Required]
        public int CommandeFournisseurId { get; set; }

        [Required]
        [Display(Name = "Article")]
        public int ArticleId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être supérieure à 0")]
        [Display(Name = "Quantité")]
        public int Quantite { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Prix unitaire")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal PrixUnitaire { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Montant ligne")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MontantLigne { get; set; }

        // Navigation properties
        public virtual CommandeFournisseur CommandeFournisseur { get; set; } = null!;
        public virtual Article Article { get; set; } = null!;
    }
}