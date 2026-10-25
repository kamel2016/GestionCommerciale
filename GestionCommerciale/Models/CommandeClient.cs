using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using GestionCommerciale.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GestionCommerciale.Models
{
    public class CommandeClient
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Numéro de commande")]
        public string NumeroCommande { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date de commande")]
        [DataType(DataType.Date)]
        public DateTime DateCommande { get; set; } = DateTime.Now;

        [Display(Name = "Date de livraison")]
        [DataType(DataType.Date)]
        public DateTime? DateLivraison { get; set; }

        //[Required]
        [Display(Name = "Client")]
        public int? ClientId { get; set; }

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
        [ValidateNever]
        public virtual Client? Client { get; set; }
        public virtual ICollection<LigneCommandeClient> LignesCommande { get; set; } = new List<LigneCommandeClient>();
    }
}