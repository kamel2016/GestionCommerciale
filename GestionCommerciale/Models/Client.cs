using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCommerciale.Models
{
    public enum TypeClient
    {
        Particulier,
        Societe
    }

    public class Client
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Type de client")]
        public TypeClient TypeClient { get; set; } = TypeClient.Particulier;

        // ============================================
        // IDENTITÉ
        // ============================================

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100)]
        [Display(Name = "Nom / Raison sociale")]
        public string Nom { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Prénom")]
        public string? Prenom { get; set; }

        [StringLength(20)]
        [Display(Name = "Matricule Fiscale")]
        public string? MatriculeFiscale { get; set; } // Ex: 1234567A/P/M/000

        // ============================================
        // CONTACT
        // ============================================

        [EmailAddress]
        [StringLength(255)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Display(Name = "Téléphone")]
        public string? Telephone { get; set; }

        [StringLength(20)]
        [Display(Name = "Mobile")]
        public string? Mobile { get; set; }

        // ============================================
        // ADRESSE TUNISIE
        // ============================================

        [StringLength(500)]
        [Display(Name = "Adresse")]
        public string? Adresse { get; set; }

        [StringLength(100)]
        [Display(Name = "Ville")]
        public string? Ville { get; set; }

        [StringLength(10)]
        [Display(Name = "Code Postal")]
        public string? CodePostal { get; set; }

        [StringLength(50)]
        [Display(Name = "Gouvernorat")]
        public string? Gouvernorat { get; set; }

        // ============================================
        // CONDITIONS COMMERCIALES
        // ============================================

        [Display(Name = "Plafond crédit (DT)")]
        public decimal PlafondCredit { get; set; } = 0;

        [Display(Name = "Délai de paiement (jours)")]
        public int DelaiPaiement { get; set; } = 0;

        [Display(Name = "Remise par défaut (%)")]
        [Range(0, 100)]
        public decimal RemiseDefaut { get; set; } = 0;

        // ============================================
        // STATUT ET AUDIT
        // ============================================

        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;

        [Display(Name = "Date de création")]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [Display(Name = "Date de modification")]
        public DateTime? DateModification { get; set; }

        [Display(Name = "Soumis à la retenue à la source")]
        public bool SoumisRetenueSource { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Taux RS par défaut (%)")]
        [Range(0, 100)]
        public decimal TauxRetenueSource { get; set; } = 1.5m;

        // ============================================
        // NAVIGATION
        // ============================================

        public virtual ICollection<CommandeClient> CommandesClient { get; set; } = new List<CommandeClient>();

        // ============================================
        // PROPRIÉTÉS CALCULÉES
        // ============================================

        [NotMapped]
        public string NomComplet => TypeClient == TypeClient.Particulier
            ? $"{Prenom} {Nom}".Trim()
            : Nom;
    }
}
