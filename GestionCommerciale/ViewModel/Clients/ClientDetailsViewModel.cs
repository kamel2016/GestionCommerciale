using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.ViewModel
{
    public class ClientDetailsViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Code Client")]
        [Required(ErrorMessage = "Le code client est obligatoire")]
        [StringLength(20, ErrorMessage = "Le code client ne peut pas dépasser 20 caractères")]
        public string CodeClient { get; set; } = string.Empty;

        [Display(Name = "Nom")]
        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Nom { get; set; } = string.Empty;

        [Display(Name = "Prénom")]
        [StringLength(100, ErrorMessage = "Le prénom ne peut pas dépasser 100 caractères")]
        public string? Prenom { get; set; }

        [Display(Name = "Adresse")]
        [StringLength(200, ErrorMessage = "L'adresse ne peut pas dépasser 200 caractères")]
        public string? Adresse { get; set; }

        [Display(Name = "Code Postal")]
        [StringLength(10, ErrorMessage = "Le code postal ne peut pas dépasser 10 caractères")]
        [RegularExpression(@"^\d{5}$", ErrorMessage = "Le code postal doit contenir 5 chiffres")]
        public string? CodePostal { get; set; }

        [Display(Name = "Ville")]
        [StringLength(100, ErrorMessage = "La ville ne peut pas dépasser 100 caractères")]
        public string? Ville { get; set; }

        [Display(Name = "Téléphone")]
        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        [Phone(ErrorMessage = "Format de téléphone invalide")]
        public string? Telephone { get; set; }

        [Display(Name = "Email")]
        [StringLength(150, ErrorMessage = "L'email ne peut pas dépasser 150 caractères")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string? Email { get; set; }

        [Display(Name = "Zone")]
        [StringLength(50, ErrorMessage = "La zone ne peut pas dépasser 50 caractères")]
        public string? Zone { get; set; }

        [Display(Name = "Remise par défaut (%)")]
        [Range(0, 100, ErrorMessage = "La remise doit être comprise entre 0 et 100%")]
        public decimal? RemiseParDefaut { get; set; }

        [Display(Name = "Type de Catégorie")]
        public string? TypeCategorie { get; set; }

        [Display(Name = "Représentant")]
        [StringLength(100, ErrorMessage = "Le représentant ne peut pas dépasser 100 caractères")]
        public string? Representant { get; set; }

        [Display(Name = "Remise Globale (%)")]
        [Range(0, 100, ErrorMessage = "La remise globale doit être comprise entre 0 et 100%")]
        public decimal? RemiseGlobale { get; set; }

        [Display(Name = "Matricule Fiscal")]
        [StringLength(50, ErrorMessage = "Le matricule fiscal ne peut pas dépasser 50 caractères")]
        public string? MatriculeFiscal { get; set; }

        [Display(Name = "Code TVA")]
        [StringLength(20, ErrorMessage = "Le code TVA ne peut pas dépasser 20 caractères")]
        public string? CodeTva { get; set; }

        [Display(Name = "Imprimé Code dans BL/Facture")]
        public bool ImprimerCodeDansBl { get; set; }

        [Display(Name = "Banque")]
        [StringLength(100, ErrorMessage = "La banque ne peut pas dépasser 100 caractères")]
        public string? Banque { get; set; }

        [Display(Name = "Compte")]
        [StringLength(50, ErrorMessage = "Le compte ne peut pas dépasser 50 caractères")]
        public string? Compte { get; set; }

        [Display(Name = "CIN")]
        [StringLength(20, ErrorMessage = "Le CIN ne peut pas dépasser 20 caractères")]
        public string? Cin { get; set; }

        [Display(Name = "RIB")]
        [StringLength(50, ErrorMessage = "Le RIB ne peut pas dépasser 50 caractères")]
        public string? Rib { get; set; }

        [Display(Name = "Montant Crédit")]
        [Range(0, double.MaxValue, ErrorMessage = "Le montant crédit doit être positif")]
        public decimal? MontantCredit { get; set; }

        [Display(Name = "Solde")]
        [Range(0, double.MaxValue, ErrorMessage = "Le solde doit être positif")]
        public decimal? Solde { get; set; }

        [Display(Name = "Livraison")]
        [StringLength(200, ErrorMessage = "L'adresse de livraison ne peut pas dépasser 200 caractères")]
        public string? AdresseLivraison { get; set; }

        [Display(Name = "Avec Commande Obligatoire")]
        public bool AvecCommandeObligatoire { get; set; }

        [Display(Name = "Nom & Prénom Arabe")]
        [StringLength(200, ErrorMessage = "Le nom arabe ne peut pas dépasser 200 caractères")]
        public string? NomPrenomArabe { get; set; }

        [Display(Name = "Critères")]
        [StringLength(500, ErrorMessage = "Les critères ne peuvent pas dépasser 500 caractères")]
        public string? Criteres { get; set; }

        [Display(Name = "Date de création")]
        public DateTime? DateCreation { get; set; }

        [Display(Name = "Date de modification")]
        public DateTime? DateModification { get; set; }

        public bool IsEditMode { get; set; } = false;
    }
}