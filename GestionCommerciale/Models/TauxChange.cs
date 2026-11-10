using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCommerciale.Models
{
    /// <summary>
    /// Historique des taux de change d'une devise vers la devise principale (TND).
    /// Permet de calculer la valeur d'un montant en devise étrangère à une date donnée.
    /// </summary>
    public class TauxChange
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Devise")]
        public int DeviseId { get; set; }

        [ForeignKey("DeviseId")]
        public virtual Devise? Devise { get; set; }

        /// <summary>Date effective du taux (jour ouvrable).</summary>
        [Required]
        [Display(Name = "Date du taux")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        /// <summary>
        /// Taux de conversion vers la devise principale (TND).
        /// Exemple : 1 EUR = 3.25 TND → Taux = 3.250000.
        /// Stocké en decimal(18,6) pour conserver la précision sur les petites variations.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,6)")]
        [Range(0.000001, double.MaxValue, ErrorMessage = "Le taux doit être strictement positif")]
        [Display(Name = "Taux vers TND")]
        public decimal Taux { get; set; }

        /// <summary>Origine du taux (saisie manuelle, BCT, API, etc.).</summary>
        [StringLength(50)]
        [Display(Name = "Source")]
        public string? Source { get; set; }

        // ============================================
        // AUDIT
        // ============================================

        [Display(Name = "Date de création")]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [Display(Name = "Date de modification")]
        public DateTime? DateModification { get; set; }
    }
}
