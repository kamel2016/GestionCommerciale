using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using GestionCommerciale.Enums;

namespace GestionCommerciale.Models
{
    public class CommandeFournisseur
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Numéro de commande")]
        public string NumeroCommande { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date de commande")]
        [DataType(DataType.Date)]
        public DateTime DateCommande { get; set; } = DateTime.Now;

        [Display(Name = "Date de livraison prévue")]
        [DataType(DataType.Date)]
        public DateTime? DateLivraisonPrevue { get; set; }

        [Display(Name = "Date de livraison réelle")]
        [DataType(DataType.Date)]
        public DateTime? DateLivraisonReelle { get; set; }

        [Required]
        [Display(Name = "Fournisseur")]
        public int FournisseurId { get; set; }

        [Required]
        [Display(Name = "Statut")]
        public StatutCommande Statut { get; set; } = StatutCommande.EnAttente;

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Montant HT")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MontantHT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Montant TTC")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MontantTTC { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Montant TVA")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MontantTVA { get; set; }

        [StringLength(1000)]
        [Display(Name = "Commentaire")]
        [DataType(DataType.MultilineText)]
        public string Commentaire { get; set; } = string.Empty;

        [Display(Name = "Date de création")]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        public DateTime? DateModification { get; set; }

        // Navigation properties
        public virtual Fournisseur Fournisseur { get; set; } = null!;
        public virtual ICollection<LigneCommandeFournisseur> LignesCommande { get; set; } = new List<LigneCommandeFournisseur>();
        public virtual ICollection<BonReception> BonsReception { get; set; } = new List<BonReception>();
    }
}