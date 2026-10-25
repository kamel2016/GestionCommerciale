using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Data;
using GestionCommerciale.Models;

namespace GestionCommerciale.Pages.Categories
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

        public List<Category> Categories { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; } // "active", "inactive" ou vide

        [BindProperty(SupportsGet = true, Name = "p")]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "order";
        // Statistiques
        public int TotalActive { get; set; }
        public int TotalInactive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                query = query.Where(c =>
                    c.Name.Contains(term) ||
                    c.Code.Contains(term) ||
                    (c.Description != null && c.Description.Contains(term)));
            }

            if (StatusFilter == "active") query = query.Where(c => c.IsActive);
            else if (StatusFilter == "inactive") query = query.Where(c => !c.IsActive);

            TotalCount = await query.CountAsync();

            if (PageNumber < 1) PageNumber = 1;

            Categories = await query
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Compteurs d'articles et de sous-catégories sans charger les collections complètes
            var ids = Categories.Select(c => c.Id).ToList();

            var articleCounts = await _context.Articles
                .Where(a => a.CategoryId != null && ids.Contains(a.CategoryId.Value))
                .GroupBy(a => a.CategoryId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            var subCounts = await _context.Categories
                .Where(c => c.ParentCategoryId != null && ids.Contains(c.ParentCategoryId.Value))
                .GroupBy(c => c.ParentCategoryId!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            ArticleCounts = articleCounts;
            SubCategoryCounts = subCounts;

            TotalActive = await _context.Categories.CountAsync(c => c.IsActive);
            TotalInactive = await _context.Categories.CountAsync(c => !c.IsActive);
        }

        public Dictionary<int, int> ArticleCounts { get; set; } = new();
        public Dictionary<int, int> SubCategoryCounts { get; set; } = new();

        // ----- Activer / désactiver -----
        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Catégorie introuvable.";
                return RedirectToPage();
            }

            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.Now;
            category.UpdatedBy = User.Identity?.Name;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"La catégorie '{category.Name}' a été {(category.IsActive ? "activée" : "désactivée")}.";
            return RedirectToPage(new { SearchTerm, StatusFilter, p = PageNumber });
        }

        // ----- Suppression -----
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Catégorie introuvable.";
                return RedirectToPage();
            }

            if (await _context.Articles.AnyAsync(a => a.CategoryId == id))
            {
                TempData["ErrorMessage"] =
                    $"Impossible de supprimer '{category.Name}' : des articles y sont rattachés.";
                return RedirectToPage();
            }

            if (await _context.Categories.AnyAsync(c => c.ParentCategoryId == id))
            {
                TempData["ErrorMessage"] =
                    $"Impossible de supprimer '{category.Name}' : elle contient des sous-catégories.";
                return RedirectToPage();
            }

            _context.Categories.Remove(category);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"La catégorie '{category.Name}' a été supprimée.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la catégorie {Id}", id);
                TempData["ErrorMessage"] = "Erreur lors de la suppression de la catégorie.";
            }

            return RedirectToPage();
        }
    }
}