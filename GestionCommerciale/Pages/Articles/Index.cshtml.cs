using GestionCommerciale.Data;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;

namespace GestionCommerciale.Pages.Articles;

public sealed class IndexModel(ApplicationDbContext db, IArticleService articleService, ILogger<IndexModel> logger) : PageModel
{
    private readonly ApplicationDbContext _db = db;
    private readonly IArticleService _articleService = articleService;
    private readonly ILogger<IndexModel> _logger = logger;

    // Les noms de paramètres sont ceux envoyés par la vue (searchTerm, category, status, stockFilter, sortBy, pageSize, page).
    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    /// <summary>Identifiant de la catégorie choisie (texte). Illisible ou vide : filtre ignoré.</summary>
    [BindProperty(Name = "category", SupportsGet = true)] public string? SelectedCategory { get; set; }
    [BindProperty(Name = "status", SupportsGet = true)] public string? SelectedStatus { get; set; }
    /// <summary>« out » = rupture (stock nul), « low » = stock bas (au-dessus de 0 et au plus égal au minimum), « high » = au-dessus du minimum.</summary>
    [BindProperty(Name = "stockFilter", SupportsGet = true)] public string? SelectedStockFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string SortBy { get; set; } = "name";
    [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

    public List<ArticleListDto> Articles { get; private set; } = new();
    /// <summary>Catégories réelles (table Categories), triées par nom : valeur = identifiant, texte = nom.</summary>
    public List<SelectListItem> Categories { get; private set; } = new();
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalRecords { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Categories = await _db.Categories.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync(cancellationToken);

            var query = _db.Articles.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                // Insensible à la casse de façon explicite (SQL Server l'est par sa collation, SQLite non).
                var term = SearchTerm.Trim().ToLower();
                query = query.Where(a => a.Reference.ToLower().Contains(term) || a.Libelle.ToLower().Contains(term) ||
                                         (a.CodeBarre != null && a.CodeBarre.ToLower().Contains(term)));
            }
            if (int.TryParse(SelectedCategory, out var categoryId))
                query = query.Where(a => a.CategoryId == categoryId);
            if (SelectedStatus is "active" or "inactive")
                query = query.Where(a => a.EstActif == (SelectedStatus == "active"));

            // Trois ensembles distincts, comme le statut affiché dans la colonne Stock.
            if (SelectedStockFilter is "out")
                query = query.Where(a => a.Quantite <= 0);
            else if (SelectedStockFilter is "low")
                query = query.Where(a => a.Quantite > 0 && a.Quantite <= a.StockMin);
            else if (SelectedStockFilter is "high")
                query = query.Where(a => a.Quantite > a.StockMin);

            // Clé secondaire = Id : l'ordre est stable, donc deux pages successives ne se chevauchent pas.
            query = SortBy switch
            {
                "name_desc" => query.OrderByDescending(a => a.Libelle).ThenBy(a => a.ID),
                "reference" => query.OrderBy(a => a.Reference).ThenBy(a => a.ID),
                "newest" => query.OrderByDescending(a => a.DateCreation).ThenByDescending(a => a.ID),
                "oldest" => query.OrderBy(a => a.DateCreation).ThenBy(a => a.ID),
                _ => query.OrderBy(a => a.Libelle).ThenBy(a => a.ID)
            };

            PageSize = PageSize is 10 or 25 or 50 or 100 ? PageSize : 25;
            TotalRecords = await query.CountAsync(cancellationToken);
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
            CurrentPage = Math.Clamp(Page, 1, TotalPages);

            Articles = await query.Skip((CurrentPage - 1) * PageSize).Take(PageSize)
                .Select(a => new ArticleListDto
                {
                    Id = a.ID,
                    Reference = a.Reference,
                    Barcode = a.CodeBarre ?? string.Empty,
                    Name = a.Libelle,
                    Category = a.Category != null ? a.Category.Name : null,
                    Brand = a.Marque,
                    Model = a.Modele,
                    CurrentStock = a.Quantite,
                    StockMinimum = a.StockMin,
                    Unit = a.Unite,
                    IsActive = a.EstActif,
                    CreatedAt = a.DateCreation,
                    StockStatus = a.Quantite <= 0 ? "out" : (a.Quantite <= a.StockMin ? "low" : "good"),
                    // Nombre réel de tarifs fournisseur de l'article (et non le seul fournisseur principal).
                    SupplierCount = _db.ArticleFournisseurs.Count(af => af.ArticleId == a.ID)
                }).ToListAsync(cancellationToken);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement de la liste d'articles");
            TempData["ErrorMessage"] = "Erreur lors du chargement des articles.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int articleId, CancellationToken cancellationToken)
    {
        try
        {
            // Le contrôle (article utilisé ou non) est fait par le service, au moment de la suppression.
            var resultat = await _articleService.SupprimerAsync(articleId, cancellationToken);
            if (resultat.Introuvable) TempData["ErrorMessage"] = "Article introuvable.";
            else if (!resultat.Succes) TempData["ErrorMessage"] = resultat.Erreurs[0].Message;
            else TempData["SuccessMessage"] = $"Article {resultat.Reference} supprimé.";
        }
        catch (Exception ex) { _logger.LogError(ex, "Erreur lors de la suppression de l'article {ArticleId}", articleId); TempData["ErrorMessage"] = "Erreur lors de la suppression de l'article."; }
        return RedirectToPage("./Index");
    }
}

public sealed class ArticleListDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal StockMinimum { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SupplierCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string StockStatus { get; set; } = "good";
}
