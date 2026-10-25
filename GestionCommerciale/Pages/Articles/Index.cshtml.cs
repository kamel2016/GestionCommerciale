using GestionCommerciale.Data;
using GestionCommerciale.Infrastructure.Persistence;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;

namespace GestionCommerciale.Pages.Articles;

public sealed class IndexModel(ApplicationDbContext db, ILogger<IndexModel> logger) : PageModel
{
    private readonly ApplicationDbContext _db = db;
    private readonly ILogger<IndexModel> _logger = logger;

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? SelectedCategory { get; set; }
    [BindProperty(SupportsGet = true)] public string? SelectedStatus { get; set; }
    [BindProperty(SupportsGet = true)] public string? SelectedStockFilter { get; set; }
    [BindProperty(SupportsGet = true)] public string SortBy { get; set; } = "name";
    [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;

    public List<ArticleListDto> Articles { get; private set; } = new();
    // Conservé pour le contrat de la vue. Remplacer par une requête Categories lorsque l'entité est disponible.
    public List<string> Categories { get; private set; } = new() { "Informatique", "Bureautique", "Fournitures", "Mobilier", "Outils", "Services" };
    public int CurrentPage { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalRecords { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            var query = _db.Articles.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                query = query.Where(a => a.Reference.Contains(term) || a.Libelle.Contains(term) ||
                                         (a.CodeBarre != null && a.CodeBarre.Contains(term)));
            }
            if (SelectedStatus is "active" or "inactive")
                query = query.Where(a => a.EstActif == (SelectedStatus == "active"));
            if (SelectedStockFilter is "out")
                query = query.Where(a => a.Quantite <= a.StockMin);
            else if (SelectedStockFilter is "low")
                query = query.Where(a => a.Quantite > 0 && a.Quantite <= a.StockMin);
            else if (SelectedStockFilter is "high")
                query = query.Where(a => a.Quantite > a.StockMin);

            query = SortBy switch
            {
                "name_desc" => query.OrderByDescending(a => a.Libelle),
                "reference" => query.OrderBy(a => a.Reference),
                "newest" => query.OrderByDescending(a => a.DateCreation),
                "oldest" => query.OrderBy(a => a.DateCreation),
                _ => query.OrderBy(a => a.Libelle)
            };

            PageSize = PageSize is 10 or 25 or 50 or 100 ? PageSize : 25;
            TotalRecords = await query.CountAsync(cancellationToken);
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
            CurrentPage = Math.Clamp(Page, 1, TotalPages);

            var rows = await query.Skip((CurrentPage - 1) * PageSize).Take(PageSize)
                .Select(a => new ArticleListDto
                {
                    Id = a.ID,
                    Reference = a.Reference,
                    Barcode = a.CodeBarre ?? string.Empty,
                    Name = a.Libelle,
                    CurrentStock = a.Quantite,
                    StockMinimum = a.StockMin,
                    Unit = a.Unite,
                    IsActive = a.EstActif,
                    CreatedAt = a.DateCreation,
                    StockStatus = a.Quantite == 0 ? "out" : (a.Quantite <= a.StockMin ? "low" : "good"),
                    SupplierCount = a.FournisseurId.HasValue ? 1 : 0
                }).ToListAsync(cancellationToken);
            Articles = rows;
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
            var article = await _db.Articles.FindAsync(new object?[] { articleId }, cancellationToken);
            if (article is null) TempData["ErrorMessage"] = "Article introuvable.";
            else { _db.Articles.Remove(article); await _db.SaveChangesAsync(cancellationToken); TempData["SuccessMessage"] = "L'article a été supprimé avec succès."; }
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