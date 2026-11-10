using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCommerciale.Models
{
    /// <summary>
    /// Devise utilisée dans l'application (TND, EUR, USD, ...).
    /// Le code devise (ISO 4217) est la clé métier et doit être unique.
    /// </summary>
    public class Devise
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Code devise ISO 4217 (ex. "TND", "EUR", "USD"). Clé métier unique.</summary>
        [Required(ErrorMessage = "Le code devise est obligatoire")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Le code devise doit comporter 3 caractères (ISO 4217)")]
        [Display(Name = "Code devise")]
        [Column(TypeName = "nvarchar(3)")]
        public string Code { get; set; } = string.Empty;

        /// <summary>Libellé complet de la devise (ex. "Dinar tunisien").</summary>
        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [StringLength(100)]
        [Display(Name = "Libellé")]
        public string Libelle { get; set; } = string.Empty;

        /// <summary>Symbole affiché à côté des montants (ex. "د.ت", "€", "$").</summary>
        [StringLength(5)]
        [Display(Name = "Symbole")]
        public string? Symbole { get; set; }

        /// <summary>Indique s'il s'agit de la devise principale de la société (généralement TND).</summary>
        [Display(Name = "Devise principale")]
        public bool EstDevisePrincipale { get; set; } = false;

        /// <summary>Indique si la devise est active et peut être utilisée.</summary>
        [Display(Name = "Actif")]
        public bool Actif { get; set; } = true;

        // ============================================
        // AUDIT
        // ============================================

        [Display(Name = "Date de création")]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [Display(Name = "Date de modification")]
        public DateTime? DateModification { get; set; }

        // ============================================
        // NAVIGATION
        // ============================================

        public virtual ICollection<TauxChange> TauxChanges { get; set; } = new List<TauxChange>();

        // ============================================
        // MÉTHODES UTILITAIRES
        // ============================================

        public override string ToString()
        {
            return $"{Code} - {Libelle}";
        }
    }
}
