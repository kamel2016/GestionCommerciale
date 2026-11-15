using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestionCommerciale.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GestionCommerciale.Models
{
    public class Devis : IValidatableObject
    {
        public int Id { get; set; }

        [Required, StringLength(30)]
        [Display(Name = "Numéro")]
        public string Numero { get; set; } = string.Empty;      // DEV-2026-00001

        [Display(Name = "Version")]
        public int Version { get; set; } = 1;

        public int? DevisParentId { get; set; }
        [ValidateNever]
        public virtual Devis? DevisParent { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Date du devis")]
        public DateTime DateDevis { get; set; } = DateTime.Today;

        [Required, DataType(DataType.Date)]
        [Display(Name = "Valable jusqu'au")]
        public DateTime DateValidite { get; set; } = DateTime.Today.AddDays(30);

        // ===== Destinataire : client OU prospect =====
        [Display(Name = "Client")]
        public int? ClientId { get; set; }
        [ValidateNever]
        public virtual Client? Client { get; set; }

        [StringLength(150), Display(Name = "Nom du prospect")]
        public string? NomProspect { get; set; }

        [StringLength(30), Display(Name = "Téléphone")]
        public string? TelephoneProspect { get; set; }

        [StringLength(150), EmailAddress, Display(Name = "Email")]
        public string? EmailProspect { get; set; }

        [StringLength(300), Display(Name = "Adresse")]
        public string? AdresseProspect { get; set; }

        [StringLength(30), Display(Name = "Matricule fiscal")]
        public string? MatriculeFiscalProspect { get; set; }

        // ===== Statut et contenu =====
        [Display(Name = "Statut")]
        public StatutDevis Statut { get; set; } = StatutDevis.Brouillon;

        [StringLength(1000), DataType(DataType.MultilineText)]
        [Display(Name = "Conditions")]
        public string? Conditions { get; set; }

        [StringLength(1000), DataType(DataType.MultilineText)]
        [Display(Name = "Notes internes")]
        public string? Notes { get; set; }

        // ===== Totaux (recalculés par le service) =====
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Total brut HT")]
        public decimal MontantBrutHT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Total remises")]
        public decimal MontantRemise { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Total HT")]
        public decimal MontantHT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Total TVA")]
        public decimal MontantTVA { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Total TTC")]
        public decimal MontantTTC { get; set; }

        // ===== Transformation =====
        public int? CommandeClientId { get; set; }
        [ValidateNever]
        public virtual CommandeClient? CommandeClient { get; set; }

        // ===== Audit =====
        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime? DateModification { get; set; }

        public virtual ICollection<LigneDevis> Lignes { get; set; } = new List<LigneDevis>();

        // ===== Propriétés calculées =====
        [NotMapped]
        public bool EstProspect => ClientId == null;

        [NotMapped]
        public string NumeroComplet => Version > 1 ? $"{Numero}-V{Version}" : Numero;

        [NotMapped]
        public bool EstModifiable => Statut == StatutDevis.Brouillon;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ClientId == null && string.IsNullOrWhiteSpace(NomProspect))
                yield return new ValidationResult(
                    "Sélectionnez un client ou saisissez le nom du prospect.",
                    new[] { nameof(ClientId), nameof(NomProspect) });

            if (DateValidite < DateDevis)
                yield return new ValidationResult(
                    "La date de validité doit être postérieure à la date du devis.",
                    new[] { nameof(DateValidite) });
        }
    }
}