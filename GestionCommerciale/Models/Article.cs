using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Models
{
    public class Article
    {
        public int ID { get; set; }

        [Required(ErrorMessage = "La référence est obligatoire")]
        [StringLength(50, ErrorMessage = "La référence ne peut pas dépasser 50 caractères")]
        [Display(Name = "Référence")]
        public string Reference { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [StringLength(200, ErrorMessage = "Le libellé ne peut pas dépasser 200 caractères")]
        [Display(Name = "Libellé")]
        public string Libelle { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Le code barre ne peut pas dépasser 50 caractères")]
        [Display(Name = "Code Barre")]
        public string CodeBarre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La quantité est obligatoire")]
        [Range(0, int.MaxValue, ErrorMessage = "La quantité doit être positive")]
        [Display(Name = "Quantité")]
        public int Quantite { get; set; }

        [Required(ErrorMessage = "Le prix de vente HT est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le prix de vente HT doit être positif")]
        [Display(Name = "Prix Vente HT")]
        public decimal PrixVenteHT { get; set; }

        [Required(ErrorMessage = "Le prix de vente TTC est obligatoire")]
        [Range(0, double.MaxValue, ErrorMessage = "Le prix de vente TTC doit être positif")]
        [Display(Name = "Prix Vente TTC")]
        public decimal PrixVenteTTC { get; set; }

        public bool EstActif { get; set; }
    }
}