using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Pages.Articles;

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
    public Article Article { get; set; } = new();

    [BindProperty]
    public SupplierInput NewSupplier { get; set; } = new();

    [BindProperty]
    public StockInput NewStock { get; set; } = new();

    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Units { get; set; } = new();
    public List<SelectListItem> AvailableSuppliers { get; set; } = new();
    public List<ArticleFournisseur> SupplierPricings { get; set; } = new();
    public List<MouvementStock> Mouvements { get; set; } = new();

    public class SupplierInput
    {
        [Required(ErrorMessage = "Sélectionnez un fournisseur")]
        public int FournisseurId { get; set; }

        [Range(0, 9999999)] public decimal PrixGrosHT { get; set; }
        [Range(0, 9999999)] public decimal PrixDetailHT { get; set; }
        [Range(0, 9999999)] public decimal? PrixPromoHT { get; set; }
        [Range(0, 100)] public decimal RemiseMax { get; set; }
    }

    public class StockInput
    {
        [Required]
        public TypeMouvementStock Type { get; set; } = TypeMouvementStock.EntreeReception;

        [Range(1, 9999999, ErrorMessage = "Quantité invalide")]
        public int Quantite { get; set; }

        [StringLength(250)]
        public string? Commentaire { get; set; }
    }

    // ---------------- GET ----------------
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var article = await _context.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.ID == id);
        if (article == null) return NotFound();

        Article = article;
        await LoadListsAsync();
        await LoadRelatedAsync(id);
        return Page();
    }

    // ---------------- Enregistrer l'article ----------------
    public async Task<IActionResult> OnPostAsync()
    {
        ClearModalState();
        ModelState.Remove("Article.Category");
        ModelState.Remove("Article.Fournisseur");

        if (await _context.Articles.AnyAsync(a => a.Reference == Article.Reference && a.ID != Article.ID))
            ModelState.AddModelError("Article.Reference", "Cette référence existe déjà.");

        if (Article.StockMax.HasValue && Article.StockMax < Article.StockMin)
            ModelState.AddModelError("Article.StockMax", "Le stock maximum doit être supérieur au stock minimum.");

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            await LoadRelatedAsync(Article.ID);
            return Page();
        }

        var db = await _context.Articles.FirstOrDefaultAsync(a => a.ID == Article.ID);
        if (db == null) return NotFound();

        db.Reference = Article.Reference;
        db.CodeBarre = Article.CodeBarre;
        db.Libelle = Article.Libelle;
        db.CategoryId = Article.CategoryId;
        db.Unite = Article.Unite;
        db.PrixAchatHT = Article.PrixAchatHT;
        db.PrixVenteHT = Article.PrixVenteHT;
        db.PrixVenteTTC = Article.PrixVenteTTC;
        db.TauxTVA = Article.TauxTVA;
        db.Fodec = Article.Fodec;
        db.StockMin = Article.StockMin;
        db.StockMax = Article.StockMax;
        db.EstActif = Article.EstActif;
        db.Description = Article.Description;
        db.Marque = Article.Marque;
        db.Modele = Article.Modele;
        db.Couleur = Article.Couleur;
        db.Taille = Article.Taille;
        db.Poids = Article.Poids;
        db.DateModification = DateTime.Now;
        // Quantite n'est PAS modifiée ici : uniquement via les mouvements de stock

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Article enregistré avec succès.";
        return RedirectToPage("./Edit", new { id = db.ID });
    }

    // ---------------- Suppression ----------------
    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var db = await _context.Articles.FindAsync(Article.ID);
        if (db == null) return NotFound();

        try
        {
            _context.Articles.Remove(db);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Article supprimé.";
            return RedirectToPage("./Index");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Suppression impossible article {Id}", Article.ID);
            TempData["ErrorMessage"] = "Suppression impossible : l'article est utilisé ailleurs.";
            return RedirectToPage("./Edit", new { id = Article.ID });
        }
    }

    // ---------------- Fournisseurs ----------------
    public async Task<IActionResult> OnPostAddSupplierAsync()
    {
        var articleId = Article.ID;

        if (!TryValidateModel(NewSupplier, nameof(NewSupplier)))
        {
            TempData["ErrorMessage"] = "Données fournisseur invalides.";
            return RedirectToPage("./Edit", new { id = articleId });
        }

        if (await _context.ArticleFournisseurs.AnyAsync(af =>
                af.ArticleId == articleId && af.FournisseurId == NewSupplier.FournisseurId))
        {
            TempData["ErrorMessage"] = "Ce fournisseur est déjà associé à l'article.";
            return RedirectToPage("./Edit", new { id = articleId });
        }

        _context.ArticleFournisseurs.Add(new ArticleFournisseur
        {
            ArticleId = articleId,
            FournisseurId = NewSupplier.FournisseurId,
            PrixGrosHT = NewSupplier.PrixGrosHT,
            PrixDetailHT = NewSupplier.PrixDetailHT,
            PrixPromoHT = NewSupplier.PrixPromoHT,
            RemiseMax = NewSupplier.RemiseMax
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Fournisseur ajouté.";
        return RedirectToPage("./Edit", new { id = articleId });
    }

    public async Task<IActionResult> OnPostRemoveSupplierAsync(int supplierId)
    {
        var articleId = Article.ID;
        var link = await _context.ArticleFournisseurs
            .FirstOrDefaultAsync(af => af.ArticleId == articleId && af.FournisseurId == supplierId);

        if (link != null)
        {
            _context.ArticleFournisseurs.Remove(link);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Fournisseur retiré.";
        }
        return RedirectToPage("./Edit", new { id = articleId });
    }

    // ---------------- Mouvement de stock ----------------
    public async Task<IActionResult> OnPostAddStockMovementAsync()
    {
        var articleId = Article.ID;

        //if (!TryValidateModel(NewStock, nameof(NewStock)))
        //{
        //    foreach (var entry in ModelState)
        //    {
        //        foreach (var error in entry.Value.Errors)
        //        {
        //            // entry.Key = nom du champ (ex: "NewStock.Quantite")
        //            // error.ErrorMessage = message défini dans [Required], [Range], etc.
        //            Console.WriteLine($"{entry.Key} : {error.ErrorMessage}");
        //        }
        //    }

        //    TempData["ErrorMessage"] = "Mouvement de stock invalide.";
        //    return RedirectToPage("./Edit", new { id = articleId });
        //}

        if (!Enum.IsDefined(NewStock.Type) || NewStock.Quantite <= 0)
        {
            TempData["ErrorMessage"] = "Mouvement de stock invalide.";
            return RedirectToPage("./Edit", new { id = articleId });
        }

        var db = await _context.Articles.FirstOrDefaultAsync(a => a.ID == articleId);

        if (db == null) return NotFound();

        var delta = NewStock.Type == TypeMouvementStock.EntreeReception ? -NewStock.Quantite : NewStock.Quantite;

        if (db.Quantite + delta < 0)
        {
            TempData["ErrorMessage"] = "Stock insuffisant pour cette sortie.";
            return RedirectToPage("./Edit", new { id = articleId });
        }

        db.Quantite += delta;
        db.DateModification = DateTime.Now;

        _context.MouvementsStock.Add(new MouvementStock
        {
            ArticleId = articleId,
            Type = NewStock.Type,
            Quantite = delta,
            QuantiteApres = db.Quantite,
            Commentaire = NewStock.Commentaire,
            Date = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Mouvement de stock enregistré.";
        return RedirectToPage("./Edit", new { id = articleId });
    }

    // ---------------- Utilitaires ----------------
    private void ClearModalState()
    {
        foreach (var k in ModelState.Keys
                     .Where(k => k.StartsWith("NewSupplier") || k.StartsWith("NewStock"))
                     .ToList())
            ModelState.Remove(k);
    }

    private async Task LoadListsAsync()
    {
        Categories = await _context.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync();
        Categories.Insert(0, new SelectListItem("-- Sélectionner une catégorie --", ""));

        Units = new[] { "Pièce", "Kg", "L", "M", "Boîte", "Paquet" }
            .Select(u => new SelectListItem(u, u)).ToList();
    }

    private async Task LoadRelatedAsync(int articleId)
    {
        SupplierPricings = await _context.ArticleFournisseurs.AsNoTracking()
            .Include(af => af.Fournisseur)
            .Where(af => af.ArticleId == articleId)
            .ToListAsync();

        var linkedIds = SupplierPricings.Select(s => s.FournisseurId).ToList();
        AvailableSuppliers = await _context.Fournisseurs.AsNoTracking()
            .Where(f => !linkedIds.Contains(f.Id))
            .OrderBy(f => f.Nom)
            .Select(f => new SelectListItem(f.Nom, f.Id.ToString()))
            .ToListAsync();

        Mouvements = await _context.MouvementsStock.AsNoTracking()
            .Where(m => m.ArticleId == articleId)
            .OrderByDescending(m => m.Date)
            .Take(50)
            .ToListAsync();
    }
}