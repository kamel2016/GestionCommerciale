using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Models
{
    public class Fournisseur
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        [Display(Name = "Téléphone")]
        public string Telephone { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
        [Display(Name = "Adresse")]
        public string Adresse { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Le contact ne peut pas dépasser 200 caractères")]
        [Display(Name = "Personne de contact")]
        public string PersonneContact { get; set; } = string.Empty;

        [Display(Name = "Date de création")]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [Display(Name = "Date de modification")]
        public DateTime? DateModification { get; set; }

        // Navigation properties
        public virtual ICollection<CommandeFournisseur> CommandesFournisseur { get; set; } = new List<CommandeFournisseur>();
    }
}