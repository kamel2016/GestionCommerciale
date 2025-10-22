using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.ViewModel
{
    public class ClientSearchFiltersViewModel
    {
        [Display(Name = "Recherche")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Ville")]
        public string? Ville { get; set; }

        [Display(Name = "Zone")]
        public string? Zone { get; set; }

        [Display(Name = "Type")]
        public string? TypeCategorie { get; set; }

        [Display(Name = "Date de début")]
        [DataType(DataType.Date)]
        public DateTime? DateCreationDebut { get; set; }

        [Display(Name = "Date de fin")]
        [DataType(DataType.Date)]
        public DateTime? DateCreationFin { get; set; }

        [Display(Name = "Clients actifs seulement")]
        public bool ClientsActifsUniquement { get; set; } = true;
    }
}