using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestionCommerciale.Pages.Suppliers
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        // Propriétés pour la pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalSuppliers { get; set; }
        public int TotalPages { get; set; }

        // Propriétés pour les filtres
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string TypeFilter { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string ZoneFilter { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string CategoryFilter { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "name";

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "asc";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int ItemsPerPage { get; set; } = 10;

        // Liste des fournisseurs
        public List<SupplierListItemViewModel> Suppliers { get; set; } = new();

        // Listes pour les filtres
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> TypeOptions { get; set; } = new();
        public List<SelectListItem> ZoneOptions { get; set; } = new();
        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<SelectListItem> PageSizeOptions { get; set; } = new();

        // Statistiques
        public SupplierStatisticsViewModel Statistics { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Initialiser les options de filtrage
                await InitializeFilterOptions();

                // Définir les paramètres de pagination
                CurrentPage = PageNumber;
                PageSize = ItemsPerPage;

                // Charger les fournisseurs avec filtres et pagination
                var (suppliers, totalCount) = await LoadSuppliersAsync();

                Suppliers = suppliers;
                TotalSuppliers = totalCount;
                TotalPages = (int)Math.Ceiling((double)TotalSuppliers / PageSize);

                // Charger les statistiques
                Statistics = await LoadStatisticsAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la liste des fournisseurs");
                TempData["ErrorMessage"] = "Erreur lors du chargement des fournisseurs.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                // Simuler la suppression
                await DeleteSupplierSimulation(id);

                _logger.LogInformation($"Fournisseur {id} supprimé");
                TempData["SuccessMessage"] = "Fournisseur supprimé avec succès.";

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression du fournisseur {id}");
                TempData["ErrorMessage"] = "Erreur lors de la suppression du fournisseur.";
                return RedirectToPage();
            }
        }

        // Méthode pour charger les fournisseurs avec filtres et pagination
        private async Task<(List<SupplierListItemViewModel>, int)> LoadSuppliersAsync()
        {
            await Task.Delay(100); // Simuler délai base de données

            // Base de données simulée de fournisseurs
            var allSuppliers = GetSimulatedSuppliers();

            // Appliquer les filtres
            var filteredSuppliers = ApplyFilters(allSuppliers);

            // Appliquer le tri
            filteredSuppliers = ApplySorting(filteredSuppliers);

            // Pagination
            var totalCount = filteredSuppliers.Count();
            var pagedSuppliers = filteredSuppliers
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return (pagedSuppliers, totalCount);
        }

        private List<SupplierListItemViewModel> GetSimulatedSuppliers()
        {
            return new List<SupplierListItemViewModel>
            {
                new SupplierListItemViewModel
                {
                    Id = 1,
                    CodeFournisseur = "FOUR001",
                    RaisonSociale = "TECH SUPPLIES SARL",
                    ContactPrincipal = "Marie TECHNOLOGIE",
                    Email = "contact@techsupplies.fr",
                    Telephone = "01.23.45.67.89",
                    Ville = "Paris",
                    Zone = "Île-de-France",
                    TypeFournisseur = "Distributeur",
                    TypeCategorie = "Matériel Informatique",
                    ConditionPaiement = "30 jours fin de mois",
                    DelaiLivraison = 3,
                    RemiseNegociee = 12.5m,
                    SoldeActuel = 15000.75m,
                    DateCreation = DateTime.Now.AddMonths(-12),
                    DateModification = DateTime.Now.AddDays(-3),
                    IsActive = true
                },
                new SupplierListItemViewModel
                {
                    Id = 2,
                    CodeFournisseur = "FOUR002",
                    RaisonSociale = "FOURNITURES BUREAU PLUS",
                    ContactPrincipal = "Jean BUREAU",
                    Email = "commandes@bureauplus.com",
                    Telephone = "02.34.56.78.90",
                    Ville = "Lyon",
                    Zone = "Rhône-Alpes",
                    TypeFournisseur = "Grossiste",
                    TypeCategorie = "Fournitures Bureau",
                    ConditionPaiement = "45 jours net",
                    DelaiLivraison = 2,
                    RemiseNegociee = 8.0m,
                    SoldeActuel = 5500.30m,
                    DateCreation = DateTime.Now.AddMonths(-8),
                    DateModification = DateTime.Now.AddDays(-1),
                    IsActive = true
                },
                new SupplierListItemViewModel
                {
                    Id = 3,
                    CodeFournisseur = "FOUR003",
                    RaisonSociale = "SERVICES MAINTENANCE SA",
                    ContactPrincipal = "Sophie MAINTENANCE",
                    Email = "support@servicesmaintenance.fr",
                    Telephone = "03.45.67.89.01",
                    Ville = "Marseille",
                    Zone = "PACA",
                    TypeFournisseur = "Prestataire de services",
                    TypeCategorie = "Maintenance",
                    ConditionPaiement = "60 jours fin de mois",
                    DelaiLivraison = 1,
                    RemiseNegociee = 5.0m,
                    SoldeActuel = -2500.00m,
                    DateCreation = DateTime.Now.AddMonths(-6),
                    DateModification = DateTime.Now.AddDays(-15),
                    IsActive = false
                },
                new SupplierListItemViewModel
                {
                    Id = 4,
                    CodeFournisseur = "FOUR004",
                    RaisonSociale = "MOBILIER DESIGN CORP",
                    ContactPrincipal = "Luc DESIGN",
                    Email = "commercial@mobilierdesign.fr",
                    Telephone = "04.56.78.90.12",
                    Ville = "Bordeaux",
                    Zone = "Aquitaine",
                    TypeFournisseur = "Fabricant",
                    TypeCategorie = "Mobilier",
                    ConditionPaiement = "30 jours net",
                    DelaiLivraison = 14,
                    RemiseNegociee = 15.0m,
                    SoldeActuel = 8750.45m,
                    DateCreation = DateTime.Now.AddMonths(-4),
                    DateModification = DateTime.Now.AddDays(-7),
                    IsActive = true
                },
                new SupplierListItemViewModel
                {
                    Id = 5,
                    CodeFournisseur = "FOUR005",
                    RaisonSociale = "CONSOMMABLES EXPRESS",
                    ContactPrincipal = "Emma RAPIDE",
                    Email = "livraisons@consommables-express.com",
                    Telephone = "05.67.89.01.23",
                    Ville = "Lille",
                    Zone = "Nord-Pas-de-Calais",
                    TypeFournisseur = "Distributeur",
                    TypeCategorie = "Consommables",
                    ConditionPaiement = "Comptant",
                    DelaiLivraison = 1,
                    RemiseNegociee = 3.0m,
                    SoldeActuel = 1200.80m,
                    DateCreation = DateTime.Now.AddMonths(-2),
                    DateModification = null,
                    IsActive = true
                },
                new SupplierListItemViewModel
                {
                    Id = 6,
                    CodeFournisseur = "FOUR006",
                    RaisonSociale = "ÉLECTRONIQUE AVANCÉE",
                    ContactPrincipal = "Paul ÉLECTRO",
                    Email = "ventes@electronique-avancee.fr",
                    Telephone = "06.78.90.12.34",
                    Ville = "Nice",
                    Zone = "PACA",
                    TypeFournisseur = "Fabricant",
                    TypeCategorie = "Électronique",
                    ConditionPaiement = "45 jours fin de mois",
                    DelaiLivraison = 7,
                    RemiseNegociee = 10.0m,
                    SoldeActuel = 3200.15m,
                    DateCreation = DateTime.Now.AddMonths(-10),
                    DateModification = DateTime.Now.AddDays(-20),
                    IsActive = true
                },
                new SupplierListItemViewModel
                {
                    Id = 7,
                    CodeFournisseur = "FOUR007",
                    RaisonSociale = "OUTILLAGE PROFESSIONNEL",
                    ContactPrincipal = "Marc OUTILS",
                    Email = "contact@outillage-pro.com",
                    Telephone = "07.89.01.23.45",
                    Ville = "Toulouse",
                    Zone = "Midi-Pyrénées",
                    TypeFournisseur = "Grossiste",
                    TypeCategorie = "Outillage",
                    ConditionPaiement = "60 jours net",
                    DelaiLivraison = 5,
                    RemiseNegociee = 12.0m,
                    SoldeActuel = 6750.90m,
                    DateCreation = DateTime.Now.AddMonths(-7),
                    DateModification = DateTime.Now.AddDays(-4),
                    IsActive = true
                },
                new SupplierListItemViewModel
                {
                    Id = 8,
                    CodeFournisseur = "FOUR008",
                    RaisonSociale = "SERVICES GÉNÉRAUX INACTIF",
                    ContactPrincipal = "Julie INACTIF",
                    Email = "inactive@services-generaux.fr",
                    Telephone = "08.90.12.34.56",
                    Ville = "Strasbourg",
                    Zone = "Alsace",
                    TypeFournisseur = "Prestataire de services",
                    TypeCategorie = "Services",
                    ConditionPaiement = "30 jours net",
                    DelaiLivraison = 3,
                    RemiseNegociee = 0.0m,
                    SoldeActuel = 0.00m,
                    DateCreation = DateTime.Now.AddMonths(-15),
                    DateModification = DateTime.Now.AddDays(-90),
                    IsActive = false
                }
            };
        }

        private IEnumerable<SupplierListItemViewModel> ApplyFilters(List<SupplierListItemViewModel> suppliers)
        {
            var filtered = suppliers.AsEnumerable();

            // Filtre par terme de recherche
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                filtered = filtered.Where(s =>
                    s.CodeFournisseur.ToLower().Contains(searchLower) ||
                    s.RaisonSociale.ToLower().Contains(searchLower) ||
                    s.ContactPrincipal?.ToLower().Contains(searchLower) == true ||
                    s.Email?.ToLower().Contains(searchLower) == true ||
                    s.Ville?.ToLower().Contains(searchLower) == true
                );
            }

            // Filtre par statut
            if (StatusFilter != "all")
            {
                var isActive = StatusFilter == "active";
                filtered = filtered.Where(s => s.IsActive == isActive);
            }

            // Filtre par type de fournisseur
            if (!string.IsNullOrWhiteSpace(TypeFilter))
            {
                filtered = filtered.Where(s => s.TypeFournisseur == TypeFilter);
            }

            // Filtre par zone
            if (!string.IsNullOrWhiteSpace(ZoneFilter))
            {
                filtered = filtered.Where(s => s.Zone == ZoneFilter);
            }

            // Filtre par catégorie
            if (!string.IsNullOrWhiteSpace(CategoryFilter))
            {
                filtered = filtered.Where(s => s.TypeCategorie == CategoryFilter);
            }

            return filtered;
        }

        private IEnumerable<SupplierListItemViewModel> ApplySorting(IEnumerable<SupplierListItemViewModel> suppliers)
        {
            var isDescending = SortOrder == "desc";

            return SortBy.ToLower() switch
            {
                "code" => isDescending ? suppliers.OrderByDescending(s => s.CodeFournisseur) : suppliers.OrderBy(s => s.CodeFournisseur),
                "name" => isDescending ? suppliers.OrderByDescending(s => s.RaisonSociale) : suppliers.OrderBy(s => s.RaisonSociale),
                "contact" => isDescending ? suppliers.OrderByDescending(s => s.ContactPrincipal) : suppliers.OrderBy(s => s.ContactPrincipal),
                "city" => isDescending ? suppliers.OrderByDescending(s => s.Ville) : suppliers.OrderBy(s => s.Ville),
                "type" => isDescending ? suppliers.OrderByDescending(s => s.TypeFournisseur) : suppliers.OrderBy(s => s.TypeFournisseur),
                "zone" => isDescending ? suppliers.OrderByDescending(s => s.Zone) : suppliers.OrderBy(s => s.Zone),
                "balance" => isDescending ? suppliers.OrderByDescending(s => s.SoldeActuel) : suppliers.OrderBy(s => s.SoldeActuel),
                "created" => isDescending ? suppliers.OrderByDescending(s => s.DateCreation) : suppliers.OrderBy(s => s.DateCreation),
                "modified" => isDescending ? suppliers.OrderByDescending(s => s.DateModification ?? DateTime.MinValue) : suppliers.OrderBy(s => s.DateModification ?? DateTime.MinValue),
                _ => suppliers.OrderBy(s => s.RaisonSociale)
            };
        }

        private async Task InitializeFilterOptions()
        {
            // Options de statut
            StatusOptions = new List<SelectListItem>
            {
                new() { Text = "Tous", Value = "all", Selected = StatusFilter == "all" },
                new() { Text = "Actifs", Value = "active", Selected = StatusFilter == "active" },
                new() { Text = "Inactifs", Value = "inactive", Selected = StatusFilter == "inactive" }
            };

            // Options de type
            TypeOptions = new List<SelectListItem>
            {
                new() { Text = "Tous les types", Value = "", Selected = string.IsNullOrEmpty(TypeFilter) },
                new() { Text = "Fabricant", Value = "Fabricant", Selected = TypeFilter == "Fabricant" },
                new() { Text = "Distributeur", Value = "Distributeur", Selected = TypeFilter == "Distributeur" },
                new() { Text = "Grossiste", Value = "Grossiste", Selected = TypeFilter == "Grossiste" },
                new() { Text = "Détaillant", Value = "Détaillant", Selected = TypeFilter == "Détaillant" },
                new() { Text = "Prestataire de services", Value = "Prestataire de services", Selected = TypeFilter == "Prestataire de services" },
                new() { Text = "Sous-traitant", Value = "Sous-traitant", Selected = TypeFilter == "Sous-traitant" }
            };

            // Options de zone
            ZoneOptions = new List<SelectListItem>
            {
                new() { Text = "Toutes les zones", Value = "", Selected = string.IsNullOrEmpty(ZoneFilter) },
                new() { Text = "Île-de-France", Value = "Île-de-France", Selected = ZoneFilter == "Île-de-France" },
                new() { Text = "Rhône-Alpes", Value = "Rhône-Alpes", Selected = ZoneFilter == "Rhône-Alpes" },
                new() { Text = "PACA", Value = "PACA", Selected = ZoneFilter == "PACA" },
                new() { Text = "Nord-Pas-de-Calais", Value = "Nord-Pas-de-Calais", Selected = ZoneFilter == "Nord-Pas-de-Calais" },
                new() { Text = "Aquitaine", Value = "Aquitaine", Selected = ZoneFilter == "Aquitaine" },
                new() { Text = "Midi-Pyrénées", Value = "Midi-Pyrénées", Selected = ZoneFilter == "Midi-Pyrénées" },
                new() { Text = "Alsace", Value = "Alsace", Selected = ZoneFilter == "Alsace" }
            };

            // Options de catégorie
            CategoryOptions = new List<SelectListItem>
            {
                new() { Text = "Toutes les catégories", Value = "", Selected = string.IsNullOrEmpty(CategoryFilter) },
                new() { Text = "Matériel Informatique", Value = "Matériel Informatique", Selected = CategoryFilter == "Matériel Informatique" },
                new() { Text = "Fournitures Bureau", Value = "Fournitures Bureau", Selected = CategoryFilter == "Fournitures Bureau" },
                new() { Text = "Maintenance", Value = "Maintenance", Selected = CategoryFilter == "Maintenance" },
                new() { Text = "Consommables", Value = "Consommables", Selected = CategoryFilter == "Consommables" },
                new() { Text = "Mobilier", Value = "Mobilier", Selected = CategoryFilter == "Mobilier" },
                new() { Text = "Services", Value = "Services", Selected = CategoryFilter == "Services" },
                new() { Text = "Électronique", Value = "Électronique", Selected = CategoryFilter == "Électronique" },
                new() { Text = "Outillage", Value = "Outillage", Selected = CategoryFilter == "Outillage" }
            };

            // Options de taille de page
            PageSizeOptions = new List<SelectListItem>
            {
                new() { Text = "10", Value = "10", Selected = ItemsPerPage == 10 },
                new() { Text = "25", Value = "25", Selected = ItemsPerPage == 25 },
                new() { Text = "50", Value = "50", Selected = ItemsPerPage == 50 },
                new() { Text = "100", Value = "100", Selected = ItemsPerPage == 100 }
            };

            await Task.CompletedTask;
        }

        private async Task<SupplierStatisticsViewModel> LoadStatisticsAsync()
        {
            await Task.Delay(50);

            var allSuppliers = GetSimulatedSuppliers();

            return new SupplierStatisticsViewModel
            {
                TotalSuppliers = allSuppliers.Count,
                ActiveSuppliers = allSuppliers.Count(s => s.IsActive),
                InactiveSuppliers = allSuppliers.Count(s => !s.IsActive),
                TotalBalance = allSuppliers.Sum(s => s.SoldeActuel ?? 0),
                AverageBalance = allSuppliers.Where(s => s.SoldeActuel.HasValue).Average(s => s.SoldeActuel.Value),
                SuppliersWithCredit = allSuppliers.Count(s => (s.SoldeActuel ?? 0) < 0),
                SuppliersWithDebit = allSuppliers.Count(s => (s.SoldeActuel ?? 0) > 0),
                NewThisMonth = allSuppliers.Count(s => s.DateCreation >= DateTime.Now.AddMonths(-1)),
                UpdatedThisWeek = allSuppliers.Count(s => s.DateModification >= DateTime.Now.AddDays(-7))
            };
        }

        private async Task DeleteSupplierSimulation(int id)
        {
            await Task.Delay(100);
            // Simulation de suppression
        }

        // Méthodes helper pour les liens de tri
        public string GetSortUrl(string sortBy)
        {
            var newSortOrder = (SortBy == sortBy && SortOrder == "asc") ? "desc" : "asc";
            return Url.Page("./Index", new
            {
                SearchTerm,
                StatusFilter,
                TypeFilter,
                ZoneFilter,
                CategoryFilter,
                SortBy = sortBy,
                SortOrder = newSortOrder,
                PageNumber = 1,
                ItemsPerPage
            });
        }

        public string GetSortIcon(string sortBy)
        {
            if (SortBy != sortBy) return "fas fa-sort text-muted";
            return SortOrder == "asc" ? "fas fa-sort-up text-primary" : "fas fa-sort-down text-primary";
        }
    }

    // ViewModels
    public class SupplierListItemViewModel
    {
        public int Id { get; set; }
        public string CodeFournisseur { get; set; } = string.Empty;
        public string RaisonSociale { get; set; } = string.Empty;
        public string? ContactPrincipal { get; set; }
        public string? Email { get; set; }
        public string? Telephone { get; set; }
        public string? Ville { get; set; }
        public string? Zone { get; set; }
        public string? TypeFournisseur { get; set; }
        public string? TypeCategorie { get; set; }
        public string? ConditionPaiement { get; set; }
        public int? DelaiLivraison { get; set; }
        public decimal? RemiseNegociee { get; set; }
        public decimal? SoldeActuel { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
        public bool IsActive { get; set; }
    }

    public class SupplierStatisticsViewModel
    {
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int InactiveSuppliers { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal AverageBalance { get; set; }
        public int SuppliersWithCredit { get; set; }
        public int SuppliersWithDebit { get; set; }
        public int NewThisMonth { get; set; }
        public int UpdatedThisWeek { get; set; }
    }
}