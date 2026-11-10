using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestionCommerciale.Models.Enums;

namespace GestionCommerciale.Models
{
    public class Reglement
    {
        public int Id { get; set; }

        public int FactureId { get; set; }
        public Facture Facture { get; set; } = null!;

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateReglement { get; set; } = DateTime.Today;

        [Column(TypeName = "decimal(18,3)")]
        [Range(0.001, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0.")]
        public decimal Montant { get; set; }

        public ModeReglement Mode { get; set; } = ModeReglement.Especes;

        [StringLength(50)]
        public string? Reference { get; set; }   // n° chèque, n° virement...

        [StringLength(100)]
        public string? Banque { get; set; }

        [StringLength(250)]
        public string? Note { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}