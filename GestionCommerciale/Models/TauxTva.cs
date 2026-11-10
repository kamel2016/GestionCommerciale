using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Models
{
    public class TauxTva
    {
        public int ID { get; set; }

        [Required, StringLength(50)]
        [Display(Name = "Libellé")]
        public string Libelle { get; set; } = string.Empty;   // ex: "TVA 19 %"

        [Range(0, 100)]
        [Display(Name = "Taux (%)")]
        public decimal Taux { get; set; }                     // ex: 19

        [Display(Name = "Actif")]
        public bool Actif { get; set; } = true;
    }
}