using GestionCommerciale.Data;
using GestionCommerciale.Infrastructure.Persistence;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Articles;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(ApplicationDbContext db, ILogger<CreateModel> logger)
    {
        _db = db;
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
            Article.DateCreation = DateTime.Now;
            _db.Articles.Add(Article);
            await _db.SaveChangesAsync();
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
            .OrderBy(c => c.Name)          // adapte le nom de la propriété
            .ToListAsync();

        Categories = categories
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),  // la valeur envoyée au serveur
                Text = c.Name             // le libellé affiché
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
