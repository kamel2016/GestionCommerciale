using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using GestionCommerciale.Enums;

namespace GestionCommerciale.Models
{
    public class BonReception
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        public string NumeroInterne { get; set; } = string.Empty;

        [Required]
        public int FournisseurId { get; set; }
        public virtual Fournisseur Fournisseur { get; set; } = null!;

        [StringLength(50)]
        public string? ReferenceFournisseur { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateReception { get; set; } = DateTime.Today;

        [DataType(DataType.Time)]
        public TimeSpan HeureReception { get; set; } = DateTime.Now.TimeOfDay;

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantHT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantTVA { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal MontantTTC { get; set; }

        public StatutBonReception Statut { get; set; } = StatutBonReception.Brouillon;

        [StringLength(1000)]
        public string? Commentaire { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
        [Required]
        [Display(Name = "Commande fournisseur")]
        public int CommandeFournisseurId { get; set; }

        public virtual CommandeFournisseur CommandeFournisseur { get; set; } = null!;

        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime? DateModification { get; set; }

        public virtual ICollection<LigneBonReception> Lignes { get; set; } = new List<LigneBonReception>();
    }
}