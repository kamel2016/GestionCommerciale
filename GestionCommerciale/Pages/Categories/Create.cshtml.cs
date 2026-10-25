using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Data;
using GestionCommerciale.Models;

namespace GestionCommerciale.Pages.Categories
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Category Category { get; set; } = new Category
        {
            IsActive = true,
            DisplayOrder = 0,
            Icon = "fas fa-box",
            Color = "#2196F3"
        };

        public SelectList ParentCategoriesList { get; set; } = null!;

        public async Task OnGetAsync()
        {
            await LoadParentCategoriesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Normalisation avant validation métier
            Category.Code = (Category.Code ?? string.Empty).Trim().ToUpperInvariant();
            Category.Name = (Category.Name ?? string.Empty).Trim();

            // Unicité du code
            if (await _context.Categories.AnyAsync(c => c.Code == Category.Code))
            {
                ModelState.AddModelError("Category.Code", "Ce code est déjà utilisé par une autre catégorie.");
            }

            // La catégorie parente doit exister
            if (Category.ParentCategoryId.HasValue &&
                !await _context.Categories.AnyAsync(c => c.Id == Category.ParentCategoryId.Value))
            {
                ModelState.AddModelError("Category.ParentCategoryId", "La catégorie parente n'existe pas.");
            }

            if (!ModelState.IsValid)
            {
                await LoadParentCategoriesAsync();
                return Page();
            }

            Category.CreatedAt = DateTime.Now;
            Category.UpdatedAt = null;
            Category.CreatedBy = User.Identity?.Name;

            _context.Categories.Add(Category);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la catégorie {Code}", Category.Code);
                ModelState.AddModelError(string.Empty, "Impossible d'enregistrer la catégorie. Vérifiez que le code est unique.");
                await LoadParentCategoriesAsync();
                return Page();
            }

            _logger.LogInformation("Catégorie créée : {Name} ({Code})", Category.Name, Category.Code);
            TempData["SuccessMessage"] = $"La catégorie '{Category.Name}' a été créée avec succès.";
            return RedirectToPage("./Index");
        }

        private async Task LoadParentCategoriesAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ParentCategoriesList = new SelectList(categories, "Id", "Name", Category?.ParentCategoryId);
        }
    }
}