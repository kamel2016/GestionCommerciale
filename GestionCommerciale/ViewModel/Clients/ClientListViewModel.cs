using GestionCommerciale.Models;
using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.ViewModel
{
    public class ClientListViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Code Client")]
        public string CodeClient { get; set; } = string.Empty;

        [Display(Name = "Nom complet")]
        public string NomComplet { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Téléphone")]
        public string? Telephone { get; set; }

        [Display(Name = "Ville")]
        public string? Ville { get; set; }

        [Display(Name = "Zone")]
        public string? Zone { get; set; }

        [Display(Name = "Type")]
        public string? TypeCategorie { get; set; }

        [Display(Name = "Date d'inscription")]
        public DateTime? DateCreation { get; set; }

        [Display(Name = "Solde")]
        public decimal? Solde { get; set; }

        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;
    }
}