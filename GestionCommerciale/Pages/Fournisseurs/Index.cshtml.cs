using GestionCommerciale.Data;
using GestionCommerciale.Helpers;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Fournisseurs
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================
        // PROPRIÉTÉS DE PAGINATION
        // ============================================
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        // ============================================
        // PROPRIÉTÉS DE FILTRAGE
        // ============================================
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string GouvernoratFilter { get; set; } = "";

        [BindProperty(SupportsGet = true)]
        public int PageSizeParam { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "name";

        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; } = false;

        // ============================================
        // DONNÉES
        // ============================================
        public List<Fournisseur> Fournisseurs { get; set; } = new();

        // ============================================
        // LISTES DE FILTRAGE
        // ============================================
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> GouvernoratOptions { get; set; } = new();

        // ============================================
        // HANDLER: GET (Chargement de la page)
        // ============================================
        public async Task OnGetAsync(int? pageIndex)
        {
            try
            {
                // Définir le PageSize à partir du paramètre
                PageSize = PageSizeParam > 0 ? PageSizeParam : 10;

                // Initialiser les options de filtrage
                await InitializeFilterOptions();

                // Récupérer la requête de base
                IQueryable<Fournisseur> query = _context.Fournisseurs
                    .AsNoTracking()
                    .Include(f => f.Articles)
                    .Include(f => f.CommandesFournisseur);

                // Appliquer les filtres
                query = ApplyFilters(query);

                // Compter le total avant pagination
                TotalCount = await query.CountAsync();

                // Appliquer le tri
                query = ApplySorting(query);

                // Appliquer la pagination
                CurrentPage = pageIndex ?? 1;
                if (CurrentPage < 1) CurrentPage = 1;

                Fournisseurs = await query
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Calculer le nombre total de pages
                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
                if (TotalPages < 1) TotalPages = 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des fournisseurs");
                TempData["ErrorMessage"] = "Erreur lors du chargement des fournisseurs.";
            }
        }

        // ============================================
        // HANDLER: POST - Toggle Statut
        // ============================================
        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            try
            {
                var fournisseur = await _context.Fournisseurs.FindAsync(id);
                if (fournisseur == null)
                {
                    TempData["ErrorMessage"] = "Fournisseur non trouvé.";
                    return RedirectToPage();
                }

                fournisseur.EstActif = !fournisseur.EstActif;
                fournisseur.DateModification = DateTime.Now;

                _context.Fournisseurs.Update(fournisseur);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Fournisseur {(fournisseur.EstActif ? "activé" : "désactivé")} avec succès.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du changement de statut du fournisseur {id}");
                TempData["ErrorMessage"] = "Erreur lors du changement de statut.";
            }

            return RedirectToPage(new
            {
                searchTerm = SearchTerm,
                statusFilter = StatusFilter,
                gouvernoratFilter = GouvernoratFilter,
                pageSizeParam = PageSize,
                pageIndex = CurrentPage,
                sortBy = SortBy,
                sortDescending = SortDescending
            });
        }

        // ============================================
        // HANDLER: POST - Supprimer
        // ============================================
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var fournisseur = await _context.Fournisseurs
                    .Include(f => f.Articles)
                    .Include(f => f.CommandesFournisseur)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (fournisseur == null)
                {
                    TempData["ErrorMessage"] = "Fournisseur non trouvé.";
                    return RedirectToPage();
                }

                // Vérifier les dépendances
                if (fournisseur.Articles.Any() || fournisseur.CommandesFournisseur.Any())
                {
                    TempData["ErrorMessage"] = "Impossible de supprimer ce fournisseur. Il possède des articles ou des commandes associés.";
                    return RedirectToPage(new { pageIndex = CurrentPage });
                }

                _context.Fournisseurs.Remove(fournisseur);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Fournisseur supprimé avec succès.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression du fournisseur {id}");
                TempData["ErrorMessage"] = "Erreur lors de la suppression du fournisseur.";
            }

            return RedirectToPage(new
            {
                searchTerm = SearchTerm,
                statusFilter = StatusFilter,
                gouvernoratFilter = GouvernoratFilter,
                pageSizeParam = PageSize,
                pageIndex = CurrentPage,
                sortBy = SortBy,
                sortDescending = SortDescending
            });
        }

        // ============================================
        // MÉTHODES PRIVÉES
        // ============================================

        /// <summary>
        /// Initialise les listes déroulantes de filtrage
        /// </summary>
        private async Task InitializeFilterOptions()
        {
            // Options de statut
            StatusOptions = new List<SelectListItem>
            {
                new() { Text = "-- Tous les statuts --", Value = "all", Selected = StatusFilter == "all" },
                new() { Text = "Actifs", Value = "active", Selected = StatusFilter == "active" },
                new() { Text = "Inactifs", Value = "inactive", Selected = StatusFilter == "inactive" }
            };

            // Récupérer les gouvernorats distincts depuis la base
            var gouvernorats = await _context.Fournisseurs
                .Where(f => f.Gouvernorat != null)
                .Select(f => f.Gouvernorat)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            GouvernoratOptions = new List<SelectListItem>
            {
                new() { Text = "-- Tous les gouvernorats --", Value = "", Selected = string.IsNullOrEmpty(GouvernoratFilter) }
            };

            foreach (var gov in gouvernorats)
            {
                if (!string.IsNullOrEmpty(gov))
                {
                    GouvernoratOptions.Add(new SelectListItem
                    {
                        Text = gov,
                        Value = gov,
                        Selected = GouvernoratFilter == gov
                    });
                }
            }
        }

        /// <summary>
        /// Applique les filtres à la requête
        /// </summary>
        private IQueryable<Fournisseur> ApplyFilters(IQueryable<Fournisseur> query)
        {
            // Filtre par terme de recherche
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(f =>
                    f.Nom.ToLower().Contains(searchLower) ||
                    (f.MatriculeFiscale != null && f.MatriculeFiscale.ToLower().Contains(searchLower)) ||
                    (f.Email != null && f.Email.ToLower().Contains(searchLower)) ||
                    (f.Telephone != null && f.Telephone.ToLower().Contains(searchLower)) ||
                    (f.PersonneContact != null && f.PersonneContact.ToLower().Contains(searchLower)) ||
                    (f.Ville != null && f.Ville.ToLower().Contains(searchLower)));
            }

            // Filtre par statut
            if (StatusFilter != "all")
            {
                var isActive = StatusFilter == "active";
                query = query.Where(f => f.EstActif == isActive);
            }

            // Filtre par gouvernorat
            if (!string.IsNullOrWhiteSpace(GouvernoratFilter))
            {
                query = query.Where(f => f.Gouvernorat == GouvernoratFilter);
            }

            return query;
        }

        /// <summary>
        /// Applique le tri à la requête
        /// </summary>
        private IQueryable<Fournisseur> ApplySorting(IQueryable<Fournisseur> query)
        {
            return SortBy switch
            {
                "name" => SortDescending
                    ? query.OrderByDescending(f => f.Nom)
                    : query.OrderBy(f => f.Nom),

                "city" => SortDescending
                    ? query.OrderByDescending(f => f.Ville)
                    : query.OrderBy(f => f.Ville),

                "created" => SortDescending
                    ? query.OrderByDescending(f => f.DateCreation)
                    : query.OrderBy(f => f.DateCreation),

                _ => query.OrderBy(f => f.Nom)
            };
        }

        /// <summary>
        /// Génère l'URL de tri pour les en-têtes cliquables
        /// </summary>
        public string GetSortUrl(string sortField)
        {
            var newSortDescending = (SortBy == sortField) ? !SortDescending : false;

            return $"?searchTerm={Uri.EscapeDataString(SearchTerm)}" +
                   $"&statusFilter={Uri.EscapeDataString(StatusFilter)}" +
                   $"&gouvernoratFilter={Uri.EscapeDataString(GouvernoratFilter)}" +
                   $"&pageSizeParam={PageSize}" +
                   $"&pageIndex=1" +
                   $"&sortBy={sortField}" +
                   $"&sortDescending={newSortDescending}";
        }

        /// <summary>
        /// Génère l'URL pour la pagination
        /// </summary>
        public string GetPageUrl(int pageNumber)
        {
            return $"?searchTerm={Uri.EscapeDataString(SearchTerm)}" +
                   $"&statusFilter={Uri.EscapeDataString(StatusFilter)}" +
                   $"&gouvernoratFilter={Uri.EscapeDataString(GouvernoratFilter)}" +
                   $"&pageSizeParam={PageSize}" +
                   $"&pageIndex={pageNumber}" +
                   $"&sortBy={SortBy}" +
                   $"&sortDescending={SortDescending}";
        }
    }
}