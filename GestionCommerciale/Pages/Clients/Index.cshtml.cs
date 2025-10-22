using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.ViewModel;

namespace GestionCommerciale.Pages.Clients
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public ClientListPageViewModel PageModel { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Ville { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Zone { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TypeCategorie { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateCreationDebut { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateCreationFin { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ClientsActifsUniquement { get; set; } = true;

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "NomComplet";

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; } = "asc";

        public async Task<IActionResult> OnGetAsync()
        {
            // Initialiser les filtres avec les paramètres de l'URL
            PageModel.Filters = new ClientSearchFiltersViewModel
            {
                SearchTerm = SearchTerm,
                Ville = Ville,
                Zone = Zone,
                TypeCategorie = TypeCategorie,
                DateCreationDebut = DateCreationDebut,
                DateCreationFin = DateCreationFin,
                ClientsActifsUniquement = ClientsActifsUniquement
            };

            // Initialiser la pagination
            PageModel.Pagination = new PaginationViewModel
            {
                CurrentPage = CurrentPage,
                PageSize = PageSize,
                SortBy = SortBy,
                SortDirection = SortDirection
            };

            // Charger les données
            await LoadClientsAsync();
            await LoadFilterOptionsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            // Rediriger avec les paramètres de recherche dans l'URL
            var routeValues = new Dictionary<string, object?>
            {
                { nameof(SearchTerm), PageModel.Filters.SearchTerm },
                { nameof(Ville), PageModel.Filters.Ville },
                { nameof(Zone), PageModel.Filters.Zone },
                { nameof(TypeCategorie), PageModel.Filters.TypeCategorie },
                { nameof(DateCreationDebut), PageModel.Filters.DateCreationDebut?.ToString("yyyy-MM-dd") },
                { nameof(DateCreationFin), PageModel.Filters.DateCreationFin?.ToString("yyyy-MM-dd") },
                { nameof(ClientsActifsUniquement), PageModel.Filters.ClientsActifsUniquement },
                { nameof(CurrentPage), 1 }, // Reset à la première page
                { nameof(PageSize), PageModel.Pagination.PageSize },
                { nameof(SortBy), PageModel.Pagination.SortBy },
                { nameof(SortDirection), PageModel.Pagination.SortDirection }
            };

            return RedirectToPage("./Index", routeValues);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                await DeleteClientAsync(id);
                TempData["SuccessMessage"] = "Le client a été supprimé avec succès.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erreur lors de la suppression : " + ex.Message;
            }

            return RedirectToPage();
        }

        private async Task LoadClientsAsync()
        {
            // Simulation du chargement des clients depuis la base de données
            var allClients = await GetAllClientsAsync();

            // Application des filtres
            var filteredClients = ApplyFilters(allClients);

            // Application du tri
            filteredClients = ApplySorting(filteredClients);

            // Mise à jour du nombre total d'éléments
            PageModel.Pagination.TotalItems = filteredClients.Count();

            // Application de la pagination
            var pagedClients = filteredClients
                .Skip((PageModel.Pagination.CurrentPage - 1) * PageModel.Pagination.PageSize)
                .Take(PageModel.Pagination.PageSize)
                .ToList();

            PageModel.Clients = pagedClients;

            // Calcul des statistiques
            PageModel.TotalClients = allClients.Count();
            PageModel.ClientsActifs = allClients.Count(c => c.EstActif);
            PageModel.ClientsInactifs = allClients.Count(c => !c.EstActif);
        }

        private async Task LoadFilterOptionsAsync()
        {
            var allClients = await GetAllClientsAsync();

            PageModel.VillesDisponibles = allClients
                .Where(c => !string.IsNullOrEmpty(c.Ville))
                .Select(c => c.Ville!)
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            PageModel.ZonesDisponibles = allClients
                .Where(c => !string.IsNullOrEmpty(c.Zone))
                .Select(c => c.Zone!)
                .Distinct()
                .OrderBy(z => z)
                .ToList();

            PageModel.TypesCategoriesDisponibles = allClients
                .Where(c => !string.IsNullOrEmpty(c.TypeCategorie))
                .Select(c => c.TypeCategorie!)
                .Distinct()
                .OrderBy(t => t)
                .ToList();
        }

        private IQueryable<ClientListViewModel> ApplyFilters(IQueryable<ClientListViewModel> clients)
        {
            if (!string.IsNullOrEmpty(PageModel.Filters.SearchTerm))
            {
                var searchTerm = PageModel.Filters.SearchTerm.ToLower();
                clients = clients.Where(c =>
                    c.NomComplet.ToLower().Contains(searchTerm) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                    c.CodeClient.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(PageModel.Filters.Ville))
            {
                clients = clients.Where(c => c.Ville == PageModel.Filters.Ville);
            }

            if (!string.IsNullOrEmpty(PageModel.Filters.Zone))
            {
                clients = clients.Where(c => c.Zone == PageModel.Filters.Zone);
            }

            if (!string.IsNullOrEmpty(PageModel.Filters.TypeCategorie))
            {
                clients = clients.Where(c => c.TypeCategorie == PageModel.Filters.TypeCategorie);
            }

            if (PageModel.Filters.DateCreationDebut.HasValue)
            {
                clients = clients.Where(c => c.DateCreation >= PageModel.Filters.DateCreationDebut.Value);
            }

            if (PageModel.Filters.DateCreationFin.HasValue)
            {
                clients = clients.Where(c => c.DateCreation <= PageModel.Filters.DateCreationFin.Value);
            }

            if (PageModel.Filters.ClientsActifsUniquement)
            {
                clients = clients.Where(c => c.EstActif);
            }

            return clients;
        }

        private IQueryable<ClientListViewModel> ApplySorting(IQueryable<ClientListViewModel> clients)
        {
            return PageModel.Pagination.SortBy.ToLower() switch
            {
                "nomcomplet" => PageModel.Pagination.SortDirection == "desc"
                    ? clients.OrderByDescending(c => c.NomComplet)
                    : clients.OrderBy(c => c.NomComplet),
                "email" => PageModel.Pagination.SortDirection == "desc"
                    ? clients.OrderByDescending(c => c.Email)
                    : clients.OrderBy(c => c.Email),
                "ville" => PageModel.Pagination.SortDirection == "desc"
                    ? clients.OrderByDescending(c => c.Ville)
                    : clients.OrderBy(c => c.Ville),
                "datecreation" => PageModel.Pagination.SortDirection == "desc"
                    ? clients.OrderByDescending(c => c.DateCreation)
                    : clients.OrderBy(c => c.DateCreation),
                "solde" => PageModel.Pagination.SortDirection == "desc"
                    ? clients.OrderByDescending(c => c.Solde)
                    : clients.OrderBy(c => c.Solde),
                _ => clients.OrderBy(c => c.NomComplet)
            };
        }

        // Méthodes de simulation - À remplacer par votre logique de données réelle
        private async Task<IQueryable<ClientListViewModel>> GetAllClientsAsync()
        {
            await Task.Delay(1); // Simulation

            var clients = new List<ClientListViewModel>
            {
                new() { Id = 1, CodeClient = "CLI001", NomComplet = "DUPONT Jean", Email = "jean.dupont@email.com", Telephone = "01.23.45.67.89", Ville = "Paris", Zone = "Nord", TypeCategorie = "VIP", DateCreation = DateTime.Now.AddMonths(-6), Solde = 1250.50m, EstActif = true },
                new() { Id = 2, CodeClient = "CLI002", NomComplet = "MARTIN Marie", Email = "marie.martin@email.com", Telephone = "02.34.56.78.90", Ville = "Lyon", Zone = "Sud", TypeCategorie = "Standard", DateCreation = DateTime.Now.AddMonths(-4), Solde = 890.00m, EstActif = true },
                new() { Id = 3, CodeClient = "CLI003", NomComplet = "BERNARD Paul", Email = "paul.bernard@email.com", Telephone = "03.45.67.89.01", Ville = "Marseille", Zone = "Sud", TypeCategorie = "Grossiste", DateCreation = DateTime.Now.AddMonths(-8), Solde = 2150.75m, EstActif = true },
                new() { Id = 4, CodeClient = "CLI004", NomComplet = "DUBOIS Sophie", Email = "sophie.dubois@email.com", Telephone = "04.56.78.90.12", Ville = "Bordeaux", Zone = "Ouest", TypeCategorie = "Détaillant", DateCreation = DateTime.Now.AddMonths(-2), Solde = 450.25m, EstActif = false },
                new() { Id = 5, CodeClient = "CLI005", NomComplet = "LEROY Michel", Email = "michel.leroy@email.com", Telephone = "05.67.89.01.23", Ville = "Lille", Zone = "Nord", TypeCategorie = "VIP", DateCreation = DateTime.Now.AddMonths(-10), Solde = 3200.00m, EstActif = true },
                new() { Id = 6, CodeClient = "CLI006", NomComplet = "MOREAU Claire", Email = "claire.moreau@email.com", Telephone = "06.78.90.12.34", Ville = "Toulouse", Zone = "Sud", TypeCategorie = "Standard", DateCreation = DateTime.Now.AddMonths(-1), Solde = 125.80m, EstActif = true },
                new() { Id = 7, CodeClient = "CLI007", NomComplet = "LAURENT Pierre", Email = "pierre.laurent@email.com", Telephone = "07.89.01.23.45", Ville = "Nantes", Zone = "Ouest", TypeCategorie = "Grossiste", DateCreation = DateTime.Now.AddMonths(-12), Solde = 1875.90m, EstActif = true },
                new() { Id = 8, CodeClient = "CLI008", NomComplet = "SIMON Isabelle", Email = "isabelle.simon@email.com", Telephone = "08.90.12.34.56", Ville = "Strasbourg", Zone = "Est", TypeCategorie = "Standard", DateCreation = DateTime.Now.AddMonths(-3), Solde = 675.40m, EstActif = false },
                new() { Id = 9, CodeClient = "CLI009", NomComplet = "MICHEL Antoine", Email = "antoine.michel@email.com", Telephone = "09.01.23.45.67", Ville = "Rennes", Zone = "Ouest", TypeCategorie = "VIP", DateCreation = DateTime.Now.AddMonths(-7), Solde = 2890.15m, EstActif = true },
                new() { Id = 10, CodeClient = "CLI010", NomComplet = "GARCIA Carmen", Email = "carmen.garcia@email.com", Telephone = "01.12.23.34.45", Ville = "Nice", Zone = "Sud", TypeCategorie = "Détaillant", DateCreation = DateTime.Now.AddMonths(-5), Solde = 1120.65m, EstActif = true }
            };

            return clients.AsQueryable();
        }

        private async Task DeleteClientAsync(int id)
        {
            // Simulation - remplacez par votre logique de suppression en base
            await Task.Delay(100);

            // Ici, vous pourriez vérifier si le client peut être supprimé
            // (pas de commandes en cours, etc.)
            if (id <= 0)
            {
                throw new ArgumentException("ID client invalide");
            }
        }
    }
}