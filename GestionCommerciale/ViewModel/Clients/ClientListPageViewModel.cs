namespace GestionCommerciale.ViewModel
{
    public class ClientListPageViewModel
    {
        public List<ClientListViewModel> Clients { get; set; } = new();
        public ClientSearchFiltersViewModel Filters { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();

        // Listes pour les filtres dropdown
        public List<string> VillesDisponibles { get; set; } = new();
        public List<string> ZonesDisponibles { get; set; } = new();
        public List<string> TypesCategoriesDisponibles { get; set; } = new();

        // Statistiques
        public int TotalClients { get; set; }
        public int ClientsActifs { get; set; }
        public int ClientsInactifs { get; set; }
    }
}