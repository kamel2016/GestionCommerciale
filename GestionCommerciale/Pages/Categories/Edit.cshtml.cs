using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Data;
using GestionCommerciale.Models;

namespace GestionCommerciale.Pages.Categories
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Category Category { get; set; } = null!;

        public SelectList ParentCategoriesList { get; set; } = null!;

        public int ArticlesCount { get; set; }
        public int SubCategoriesCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            Category = category;
            await LoadDataAsync(category.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Category.Code = (Category.Code ?? string.Empty).Trim().ToUpperInvariant();
            Category.Name = (Category.Name ?? string.Empty).Trim();

            // Unicité du code (hors catégorie courante)
            if (await _context.Categories.AnyAsync(c => c.Code == Category.Code && c.Id != Category.Id))
            {
                ModelState.AddModelError("Category.Code", "Ce code est déjà utilisé par une autre catégorie.");
            }

            // Anti-cycle : une catégorie ne peut pas être son propre parent, ni le parent d'un de ses descendants
            if (Category.ParentCategoryId.HasValue)
            {
                if (Category.ParentCategoryId == Category.Id)
                {
                    ModelState.AddModelError("Category.ParentCategoryId", "Une catégorie ne peut pas être sa propre parente.");
                }
                else if (await IsDescendantAsync(Category.Id, Category.ParentCategoryId.Value))
                {
                    ModelState.AddModelError("Category.ParentCategoryId", "Cette catégorie parente est une sous-catégorie de la catégorie courante (cycle interdit).");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDataAsync(Category.Id);
                return Page();
            }

            var existing = await _context.Categories.FirstOrDefaultAsync(c => c.Id == Category.Id);
            if (existing == null) return NotFound();

            // Mise à jour champ par champ (évite l'écrasement de CreatedAt / CreatedBy)
            existing.Code = Category.Code;
            existing.Name = Category.Name;
            existing.Description = Category.Description;
            existing.ParentCategoryId = Category.ParentCategoryId;
            existing.DisplayOrder = Category.DisplayOrder;
            existing.Icon = Category.Icon;
            existing.Color = Category.Color;
            existing.IsActive = Category.IsActive;
            existing.UpdatedAt = DateTime.Now;
            existing.UpdatedBy = User.Identity?.Name;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de la catégorie {Id}", Category.Id);
                ModelState.AddModelError(string.Empty, "Impossible d'enregistrer les modifications.");
                await LoadDataAsync(Category.Id);
                return Page();
            }

            TempData["SuccessMessage"] = $"La catégorie '{existing.Name}' a été modifiée avec succès.";
            return RedirectToPage("./Index");
        }

        // Vrai si candidateParentId est un descendant de categoryId
        private async Task<bool> IsDescendantAsync(int categoryId, int candidateParentId)
        {
            int? current = candidateParentId;
            var visited = new HashSet<int>();

            while (current.HasValue && visited.Add(current.Value))
            {
                if (current.Value == categoryId) return true;

                current = await _context.Categories
                    .Where(c => c.Id == current.Value)
                    .Select(c => c.ParentCategoryId)
                    .FirstOrDefaultAsync();
            }
            return false;
        }

        private async Task LoadDataAsync(int categoryId)
        {
            // On exclut la catégorie elle-même de la liste des parents possibles
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive && c.Id != categoryId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ParentCategoriesList = new SelectList(categories, "Id", "Name", Category?.ParentCategoryId);

            ArticlesCount = await _context.Articles.CountAsync(a => a.CategoryId == categoryId);
            SubCategoriesCount = await _context.Categories.CountAsync(c => c.ParentCategoryId == categoryId);
        }
    }
}