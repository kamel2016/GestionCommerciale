using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Pages.Articles;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IStockService _stock;
    private readonly IArticleService _articleService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(ApplicationDbContext context, IStockService stock, IArticleService articleService, ILogger<EditModel> logger)
    {
        _context = context;
        _stock = stock;
        _articleService = articleService;
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

        /// <summary>
        /// Positive pour une entrée, un retour client ou un retour fournisseur (le signe réel est calculé par StockService).
        /// Pour un ajustement d'inventaire : signée (+ ajoute, - retire).
        /// </summary>
        public int Quantite { get; set; }

        /// <summary>Motif (obligatoire pour un ajustement). Enregistré dans le commentaire du mouvement.</summary>
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
        try
        {
            // Le contrôle (article utilisé ou non) est fait par le service, au moment de la suppression.
            var resultat = await _articleService.SupprimerAsync(Article.ID);

            if (resultat.Introuvable) return NotFound();

            if (!resultat.Succes)
            {
                TempData["ErrorMessage"] = resultat.Erreurs[0].Message;
                return RedirectToPage("./Edit", new { id = Article.ID });
            }

            TempData["SuccessMessage"] = $"Article {resultat.Reference} supprimé.";
            return RedirectToPage("./Index");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Suppression impossible article {Id}", Article.ID);
            TempData["ErrorMessage"] = "Suppression impossible : une erreur est survenue lors de l'enregistrement.";
            return RedirectToPage("./Edit", new { id = Article.ID });
        }
    }

    // ---------------- Fournisseurs ----------------
    public async Task<IActionResult> OnPostAddSupplierAsync()
    {
        var articleId = Article.ID;

        // La fenêtre n'envoie que Article.ID et NewSupplier.* ; MVC valide pourtant l'objet Article lié
        // (Libelle et Reference vides => erreurs), et TryValidateModel renvoie faux dès que le ModelState
        // contient UNE erreur. On écarte donc les erreurs des autres objets liés avant de valider le fournisseur.
        RetirerErreursDesAutresObjets();

        // FournisseurId <= 0 : champ absent de la requête ([Required] ne protège pas un int non nul). Sans ce garde,
        // l'enregistrement échouait sur la clé étrangère (erreur 500) au lieu d'un refus propre.
        if (NewSupplier.FournisseurId <= 0 || !TryValidateModel(NewSupplier, nameof(NewSupplier)))
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
        else
        {
            // Lien introuvable (déjà retiré, autre article, identifiant absent) : on le dit au lieu de ne rien afficher.
            TempData["ErrorMessage"] = "Ce fournisseur n'est plus lié à l'article.";
        }
        return RedirectToPage("./Edit", new { id = articleId });
    }

    // ---------------- Mouvement de stock ----------------
    public async Task<IActionResult> OnPostAddStockMovementAsync()
    {
        var articleId = Article.ID;

        // La page ne fait que transmettre type, quantité et motif : le signe, le contrôle du stock
        // et l'historique (QuantiteAvant / QuantiteApres) sont gérés par StockService.
        var resultat = await _stock.EnregistrerMouvementManuelAsync(
            articleId, NewStock.Type, NewStock.Quantite, NewStock.Commentaire);

        if (resultat.Introuvable) return NotFound();

        if (!resultat.Succes)
        {
            TempData["ErrorMessage"] = resultat.Message;
            return RedirectToPage("./Edit", new { id = articleId });
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Quelqu'un d'autre a modifié le stock entre la lecture et l'enregistrement (RowVersion).
            TempData["ErrorMessage"] = "Le stock a été modifié par une autre opération. Rien n'a été enregistré : vérifiez le stock puis recommencez.";
            return RedirectToPage("./Edit", new { id = articleId });
        }

        TempData["SuccessMessage"] = "Mouvement de stock enregistré.";
        return RedirectToPage("./Edit", new { id = articleId });
    }

    // ---------------- Utilitaires ----------------

    /// <summary>
    /// Retire du ModelState les entrées dont la clé commence par « Article. » ou « NewStock. » (autres objets liés).
    /// Les clés « NewSupplier.* », la clé vide et les erreurs d'infrastructure sont conservées.
    /// </summary>
    private void RetirerErreursDesAutresObjets()
    {
        foreach (var cle in ModelState.Keys
                     .Where(k => k.StartsWith("Article.", StringComparison.Ordinal)
                              || k.StartsWith("NewStock.", StringComparison.Ordinal))
                     .ToList())
            ModelState.Remove(cle);
    }

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