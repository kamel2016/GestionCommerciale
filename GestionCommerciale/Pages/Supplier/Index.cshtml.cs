using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Suppliers
{
    public class IndexModel : PageModel
    {
        private static readonly int[] AllowedPageSizes = { 10, 25, 50, 100 };

        private readonly ApplicationDbContext _db;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext db, ILogger<IndexModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ---------- Filtres / tri / pagination ----------
        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string StatusFilter { get; set; } = "all";
        [BindProperty(SupportsGet = true)] public string? GouvernoratFilter { get; set; }
        [BindProperty(SupportsGet = true)] public string SortBy { get; set; } = "name";
        [BindProperty(SupportsGet = true)] public string SortOrder { get; set; } = "asc";
        [BindProperty(SupportsGet = true, Name = "page")] public int CurrentPage { get; set; } = 1;
        [BindProperty(SupportsGet = true, Name = "pageSize")] public int PageSize { get; set; } = 10;

        // ---------- Résultats ----------
        public List<FournisseurListItem> Fournisseurs { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public FournisseurStats Stats { get; set; } = new();

        public List<SelectListItem> GouvernoratOptions { get; set; } = new();
        public List<SelectListItem> PageSizeOptions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                NormalizeParameters();

                var query = ApplyFilters(_db.Fournisseurs.AsNoTracking());

                TotalCount = await query.CountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;

                Fournisseurs = await ApplySorting(query)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .Select(f => new FournisseurListItem
                    {
                        Id = f.Id,
                        Nom = f.Nom,
                        MatriculeFiscale = f.MatriculeFiscale,
                        PersonneContact = f.PersonneContact,
                        Email = f.Email,
                        Telephone = f.Telephone,
                        Ville = f.Ville,
                        Gouvernorat = f.Gouvernorat,
                        DelaiLivraison = f.DelaiLivraison,
                        DelaiPaiement = f.DelaiPaiement,
                        RemiseAccordee = f.RemiseAccordee,
                        EstActif = f.EstActif,
                        DateCreation = f.DateCreation,
                        DateModification = f.DateModification,
                        ArticlesCount = f.Articles.Count(),
                        CommandesCount = f.CommandesFournisseur.Count()
                    })
                    .ToListAsync();

                await LoadStatsAsync();
                await LoadOptionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des fournisseurs");
                TempData["ErrorMessage"] = "Erreur lors du chargement des fournisseurs.";
            }

            return Page();
        }

        // ---------- Toggle statut ----------
        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var f = await _db.Fournisseurs.FindAsync(id);
            if (f == null)
            {
                TempData["ErrorMessage"] = "Fournisseur introuvable.";
                return RedirectToCurrent();
            }

            f.EstActif = !f.EstActif;
            f.DateModification = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Fournisseur « {f.Nom} » " + (f.EstActif ? "activé." : "désactivé.");
            return RedirectToCurrent();
        }

        // ---------- Suppression ----------
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var f = await _db.Fournisseurs
                    .Include(x => x.Articles)
                    .Include(x => x.CommandesFournisseur)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (f == null)
                {
                    TempData["ErrorMessage"] = "Fournisseur introuvable.";
                    return RedirectToCurrent();
                }

                if (f.Articles.Any())
                {
                    TempData["ErrorMessage"] =
                        $"Suppression impossible : {f.Articles.Count} article(s) sont liés à « {f.Nom} ».";
                    return RedirectToCurrent();
                }

                if (f.CommandesFournisseur.Any())
                {
                    TempData["ErrorMessage"] =
                        $"Suppression impossible : {f.CommandesFournisseur.Count} commande(s) sont liées à « {f.Nom} ». Désactivez-le plutôt.";
                    return RedirectToCurrent();
                }

                _db.Fournisseurs.Remove(f);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Fournisseur « {f.Nom} » supprimé.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur de suppression du fournisseur {Id}", id);
                TempData["ErrorMessage"] = "Suppression impossible : le fournisseur est référencé ailleurs.";
            }

            return RedirectToCurrent();
        }

        // ---------- Helpers ----------
        private IActionResult RedirectToCurrent() =>
            RedirectToPage(new
            {
                SearchTerm,
                StatusFilter,
                GouvernoratFilter,
                SortBy,
                SortOrder,
                page = CurrentPage,
                pageSize = PageSize
            });

        private void NormalizeParameters()
        {
            if (CurrentPage < 1) CurrentPage = 1;
            if (!AllowedPageSizes.Contains(PageSize)) PageSize = 10;
            SortOrder = SortOrder == "desc" ? "desc" : "asc";
            if (StatusFilter != "active" && StatusFilter != "inactive") StatusFilter = "all";
            SearchTerm = SearchTerm?.Trim();
        }

        private IQueryable<Fournisseur> ApplyFilters(IQueryable<Fournisseur> q)
        {
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var s = SearchTerm;
                q = q.Where(f =>
                    f.Nom.Contains(s) ||
                    (f.MatriculeFiscale != null && f.MatriculeFiscale.Contains(s)) ||
                    (f.PersonneContact != null && f.PersonneContact.Contains(s)) ||
                    (f.Email != null && f.Email.Contains(s)) ||
                    (f.Telephone != null && f.Telephone.Contains(s)) ||
                    (f.Ville != null && f.Ville.Contains(s)));
            }

            if (StatusFilter == "active") q = q.Where(f => f.EstActif);
            else if (StatusFilter == "inactive") q = q.Where(f => !f.EstActif);

            if (!string.IsNullOrWhiteSpace(GouvernoratFilter))
                q = q.Where(f => f.Gouvernorat == GouvernoratFilter);

            return q;
        }

        private IQueryable<Fournisseur> ApplySorting(IQueryable<Fournisseur> q)
        {
            var desc = SortOrder == "desc";
            return SortBy.ToLower() switch
            {
                "id" => desc ? q.OrderByDescending(f => f.Id) : q.OrderBy(f => f.Id),
                "contact" => desc ? q.OrderByDescending(f => f.PersonneContact) : q.OrderBy(f => f.PersonneContact),
                "city" => desc ? q.OrderByDescending(f => f.Ville) : q.OrderBy(f => f.Ville),
                "gouvernorat" => desc ? q.OrderByDescending(f => f.Gouvernorat) : q.OrderBy(f => f.Gouvernorat),
                "delai" => desc ? q.OrderByDescending(f => f.DelaiLivraison) : q.OrderBy(f => f.DelaiLivraison),
                "paiement" => desc ? q.OrderByDescending(f => f.DelaiPaiement) : q.OrderBy(f => f.DelaiPaiement),
                "remise" => desc ? q.OrderByDescending(f => f.RemiseAccordee) : q.OrderBy(f => f.RemiseAccordee),
                "created" => desc ? q.OrderByDescending(f => f.DateCreation) : q.OrderBy(f => f.DateCreation),
                "modified" => desc ? q.OrderByDescending(f => f.DateModification) : q.OrderBy(f => f.DateModification),
                _ => desc ? q.OrderByDescending(f => f.Nom) : q.OrderBy(f => f.Nom)
            };
        }

        private async Task LoadStatsAsync()
        {
            var now = DateTime.Now;
            var monthAgo = now.AddMonths(-1);
            var weekAgo = now.AddDays(-7);

            Stats = new FournisseurStats
            {
                Total = await _db.Fournisseurs.CountAsync(),
                Actifs = await _db.Fournisseurs.CountAsync(f => f.EstActif),
                Inactifs = await _db.Fournisseurs.CountAsync(f => !f.EstActif),
                NouveauxCeMois = await _db.Fournisseurs.CountAsync(f => f.DateCreation >= monthAgo),
                ModifiesCetteSemaine = await _db.Fournisseurs.CountAsync(f => f.DateModification >= weekAgo)
            };
        }

        private async Task LoadOptionsAsync()
        {
            var gouvs = await _db.Fournisseurs.AsNoTracking()
                .Where(f => f.Gouvernorat != null && f.Gouvernorat != "")
                .Select(f => f.Gouvernorat!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            GouvernoratOptions = gouvs
                .Select(g => new SelectListItem(g, g, g == GouvernoratFilter))
                .ToList();

            PageSizeOptions = AllowedPageSizes
                .Select(n => new SelectListItem(n.ToString(), n.ToString(), n == PageSize))
                .ToList();
        }

        // ---------- Liens de tri ----------
        public string GetSortUrl(string sortBy)
        {
            var newOrder = (SortBy == sortBy && SortOrder == "asc") ? "desc" : "asc";
            return Url.Page("./Index", new
            {
                SearchTerm,
                StatusFilter,
                GouvernoratFilter,
                SortBy = sortBy,
                SortOrder = newOrder,
                page = 1,
                pageSize = PageSize
            })!;
        }

        public string GetPageUrl(int page) =>
            Url.Page("./Index", new
            {
                SearchTerm,
                StatusFilter,
                GouvernoratFilter,
                SortBy,
                SortOrder,
                page,
                pageSize = PageSize
            })!;

        public string GetSortIcon(string sortBy)
        {
            if (SortBy != sortBy) return "fas fa-sort text-muted";
            return SortOrder == "asc" ? "fas fa-sort-up text-primary" : "fas fa-sort-down text-primary";
        }
    }

    // ---------- ViewModels ----------
    public class FournisseurListItem
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? MatriculeFiscale { get; set; }
        public string? PersonneContact { get; set; }
        public string? Email { get; set; }
        public string? Telephone { get; set; }
        public string? Ville { get; set; }
        public string? Gouvernorat { get; set; }
        public int DelaiLivraison { get; set; }
        public int DelaiPaiement { get; set; }
        public decimal RemiseAccordee { get; set; }
        public bool EstActif { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
        public int ArticlesCount { get; set; }
        public int CommandesCount { get; set; }
    }

    public class FournisseurStats
    {
        public int Total { get; set; }
        public int Actifs { get; set; }
        public int Inactifs { get; set; }
        public int NouveauxCeMois { get; set; }
        public int ModifiesCetteSemaine { get; set; }
    }
}