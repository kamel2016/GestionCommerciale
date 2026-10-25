using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCommerciale.Models
{
    public class Fournisseur
    {
        [Key]
        public int Id { get; set; }

        // ============================================
        // IDENTITÉ
        // ============================================

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100)]
        [Display(Name = "Raison sociale")]
        public string Nom { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Matricule Fiscale")]
        public string? MatriculeFiscale { get; set; }

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
        [Display(Name = "Fax")]
        public string? Fax { get; set; }

        [StringLength(100)]
        [Display(Name = "Personne de contact")]
        public string? PersonneContact { get; set; }

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

        [Display(Name = "Délai de livraison (jours)")]
        public int DelaiLivraison { get; set; } = 0;

        [Display(Name = "Délai de paiement (jours)")]
        public int DelaiPaiement { get; set; } = 0;

        [Display(Name = "Remise accordée (%)")]
        [Range(0, 100)]
        public decimal RemiseAccordee { get; set; } = 0;

        // ============================================
        // STATUT ET AUDIT
        // ============================================

        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;

        [Display(Name = "Date de création")]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [Display(Name = "Date de modification")]
        public DateTime? DateModification { get; set; }

        // ============================================
        // NAVIGATION
        // ============================================

        public virtual ICollection<CommandeFournisseur> CommandesFournisseur { get; set; } = new List<CommandeFournisseur>();
        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
    }
}