using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Articles;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IArticleService _articleService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(ApplicationDbContext db, IArticleService articleService, ILogger<CreateModel> logger)
    {
        _db = db;
        _articleService = articleService;
        _logger = logger;
    }

    [BindProperty]
    public Article Article { get; set; } = new();

    public List<SelectListItem> Categories { get; private set; } = new();
    public List<SelectListItem> Units { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Article = new Article { EstActif = true, TauxTVA = 19m, Unite = "Pièce" };
        await InitializeSelectListsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await InitializeSelectListsAsync();
            return Page();
        }

        try
        {
            // Toutes les règles (nettoyage, validation, unicité, catégorie, valeurs imposées) sont dans ArticleService.
            var resultat = await _articleService.CreerAsync(Article);

            if (!resultat.Succes)
            {
                foreach (var (champ, message) in resultat.Erreurs)
                    ModelState.AddModelError(string.IsNullOrEmpty(champ) ? string.Empty : "Article." + champ, message);

                await InitializeSelectListsAsync();
                return Page();
            }

            TempData["SuccessMessage"] = $"L'article {Article.Libelle} a été créé avec succès.";
            return RedirectToPage("./Index");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erreur EF Core lors de la création de l'article");
            ModelState.AddModelError(string.Empty, "L'article n'a pas pu être enregistré.");
            await InitializeSelectListsAsync();
            return Page();
        }
    }

    private async Task InitializeSelectListsAsync()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        Categories = categories
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToList();

        Units = new List<SelectListItem>
        {
            new("Pièce(s)", "Pièce"),
            new("Kilogramme(s)", "Kg"),
            new("Litre(s)", "L"),
            new("Mètre(s)", "M"),
            new("Boîte(s)", "Boîte"),
            new("Paquet(s)", "Paquet")
        };
    }
}
