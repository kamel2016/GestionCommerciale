using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestionCommerciale.Enums;
using GestionCommerciale.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GestionCommerciale.Models
{
    public class CommandeClient : IValidatableObject
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Numéro de commande")]
        public string NumeroCommande { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date de commande")]
        [DataType(DataType.Date)]
        public DateTime DateCommande { get; set; } = DateTime.Now;

        [Display(Name = "Date de livraison prévue")]
        [DataType(DataType.Date)]
        public DateTime? DateLivraison { get; set; }

        [Required(ErrorMessage = "Le client est obligatoire.")]
        [Display(Name = "Client")]
        public int ClientId { get; set; }

        // ⏭️ À activer quand le module Devis sera créé
        // [Display(Name = "Devis d'origine")]
        // public int? DevisId { get; set; }

        [Required]
        [Display(Name = "Statut")]
        public StatutCommande Statut { get; set; } = StatutCommande.EnAttente;

        // ===== Totaux =====

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Total HT brut")]
        [DisplayFormat(DataFormatString = "{0:N3}")]
        public decimal MontantHTBrut { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Range(0, 100, ErrorMessage = "La remise doit être comprise entre 0 et 100 %.")]
        [Display(Name = "Remise globale (%)")]
        public decimal TauxRemiseGlobale { get; set; } = 0;

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Montant remise globale")]
        [DisplayFormat(DataFormatString = "{0:N3}")]
        public decimal MontantRemiseGlobale { get; set; }

        /// <summary>HT net, après remise globale.</summary>
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Montant HT")]
        [DisplayFormat(DataFormatString = "{0:N3}")]
        public decimal MontantHT { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Montant TVA")]
        [DisplayFormat(DataFormatString = "{0:N3}")]
        public decimal MontantTVA { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Montant TTC")]
        [DisplayFormat(DataFormatString = "{0:N3}")]
        public decimal MontantTTC { get; set; }

        [StringLength(1000)]
        [Display(Name = "Commentaire")]
        [DataType(DataType.MultilineText)]
        public string? Commentaire { get; set; }

        [Display(Name = "Date de création")]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        public DateTime? DateModification { get; set; }

        // ===== Navigation =====

        [ValidateNever]
        public virtual Client Client { get; set; } = null!;

        // ⏭️ À activer quand le module Devis sera créé
        // [ValidateNever]
        // public virtual Devis? Devis { get; set; }

        [ValidateNever]
        public virtual ICollection<LigneCommandeClient> LignesCommande { get; set; } = new List<LigneCommandeClient>();

        // ===== Calcul =====

        /// <summary>
        /// Recalcule tous les totaux à partir des lignes et de la remise globale.
        /// À appeler juste avant chaque SaveChangesAsync (création et modification).
        /// </summary>
        public void RecalculerTotaux()
        {
            // ⚠️ Si ça ne compile pas ici : envoie-moi LigneCommandeClient.cs,
            // les noms MontantHT / TauxTVA sont peut-être différents chez toi.
            foreach (var ligne in LignesCommande)
                ligne.CalculerMontant();

            var totaux = CalculTotaux.CalculerTotaux(
                LignesCommande.Select(l => (l.MontantLigne, l.TauxTVA)),
                TauxRemiseGlobale,
                timbre: 0m);  // pas de timbre sur une commande

            MontantHTBrut = totaux.TotalHTBrut;
            MontantRemiseGlobale = totaux.MontantRemiseGlobale;
            MontantHT = totaux.TotalHTNet;
            MontantTVA = totaux.TotalTVA;
            MontantTTC = totaux.TotalTTC;
        }

        // ===== Validation =====

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateLivraison.HasValue && DateLivraison.Value.Date < DateCommande.Date)
                yield return new ValidationResult(
                    "La date de livraison ne peut pas être antérieure à la date de commande.",
                    new[] { nameof(DateLivraison) });
        }
    }
}