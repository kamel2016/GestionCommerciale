using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.ViewModel
{
    // ViewModel spécifique pour la création de client
    public class ClientCreateViewModel
    {
        [Required(ErrorMessage = "Le code client est obligatoire")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Le code client doit contenir entre 3 et 20 caractères")]
        public string CodeClient { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Le format de l'email n'est pas valide")]
        [StringLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Le format du téléphone n'est pas valide")]
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        public string? Telephone { get; set; }

        [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
        public string? Adresse { get; set; }

        [StringLength(10, ErrorMessage = "Le code postal ne peut pas dépasser 10 caractères")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Le code postal doit contenir 5 chiffres")]
        public string? CodePostal { get; set; }

        [StringLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
        public string? Ville { get; set; }

        [StringLength(500, ErrorMessage = "L'adresse de livraison ne peut pas dépasser 500 caractères")]
        public string? AdresseLivraison { get; set; }

        [StringLength(50, ErrorMessage = "La zone ne peut pas dépasser 50 caractères")]
        public string? Zone { get; set; }

        [StringLength(50, ErrorMessage = "Le type de catégorie ne peut pas dépasser 50 caractères")]
        public string? TypeCategorie { get; set; }

        [Range(0, 100, ErrorMessage = "La remise doit être comprise entre 0 et 100%")]
        public decimal? RemiseParDefaut { get; set; }

        [StringLength(100, ErrorMessage = "Le représentant ne peut pas dépasser 100 caractères")]
        public string? Representant { get; set; }

        [StringLength(50, ErrorMessage = "Le matricule fiscal ne peut pas dépasser 50 caractères")]
        public string? MatriculeFiscal { get; set; }

        [StringLength(50, ErrorMessage = "Le code TVA ne peut pas dépasser 50 caractères")]
        public string? CodeTva { get; set; }

        [StringLength(20, ErrorMessage = "Le CIN ne peut pas dépasser 20 caractères")]
        public string? Cin { get; set; }

        [StringLength(100, ErrorMessage = "La banque ne peut pas dépasser 100 caractères")]
        public string? Banque { get; set; }

        [StringLength(50, ErrorMessage = "Le compte ne peut pas dépasser 50 caractères")]
        public string? Compte { get; set; }

        [StringLength(50, ErrorMessage = "Le RIB ne peut pas dépasser 50 caractères")]
        public string? Rib { get; set; }

        [StringLength(2000, ErrorMessage = "Les critères ne peuvent pas dépasser 2000 caractères")]
        public string? Criteres { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le montant de crédit ne peut pas être négatif")]
        public decimal? MontantCredit { get; set; }

        public decimal? Solde { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}