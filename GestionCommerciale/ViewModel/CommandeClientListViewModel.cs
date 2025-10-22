using GestionCommerciale.Enums;
using GestionCommerciale.Models;

namespace GestionCommerciale.ViewModel
{
    public class CommandeClientListViewModel
    {
        public string? SearchTerm { get; set; }
        public int? ClientId { get; set; }
        public StatutCommande? Statut { get; set; }
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<CommandeClient> Commandes { get; set; } = new();
        public List<Client> Clients { get; set; } = new();
    }
}