//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using GestionCommerciale.Models;

//namespace GestionCommerciale.Pages.Articles
//{
//    public class IndexModel : PageModel
//    {
//        public List<Article> Articles { get; set; } = new List<Article>();

//        [BindProperty(SupportsGet = true)]
//        public string? Filtre { get; set; }

//        public void OnGet()
//        {
//            var tousLesArticles = new List<Article>
//            {
//                new Article { ID = 1, Reference = "ART001", Libelle = "Ordinateur portable", CodeBarre = "123456789", Quantite = 10, PrixVenteHT = 800.00m, PrixVenteTTC = 960.00m },
//                new Article { ID = 2, Reference = "ART002", Libelle = "Souris sans fil", CodeBarre = "987654321", Quantite = 25, PrixVenteHT = 25.00m, PrixVenteTTC = 30.00m },
//                new Article { ID = 3, Reference = "ART003", Libelle = "Clavier mécanique", CodeBarre = "456789123", Quantite = 15, PrixVenteHT = 120.00m, PrixVenteTTC = 144.00m },
//                new Article { ID = 4, Reference = "ART004", Libelle = "Écran 24 pouces", CodeBarre = "789123456", Quantite = 8, PrixVenteHT = 200.00m, PrixVenteTTC = 240.00m },
//                new Article { ID = 5, Reference = "ART005", Libelle = "Imprimante laser", CodeBarre = "321654987", Quantite = 5, PrixVenteHT = 300.00m, PrixVenteTTC = 360.00m }
//            };

//            // Filtrage si un filtre est appliqué
//            if (!string.IsNullOrEmpty(Filtre))
//            {
//                Articles = tousLesArticles.Where(a =>
//                    a.Reference.Contains(Filtre, StringComparison.OrdinalIgnoreCase) ||
//                    a.Libelle.Contains(Filtre, StringComparison.OrdinalIgnoreCase) ||
//                    a.CodeBarre.Contains(Filtre, StringComparison.OrdinalIgnoreCase))
//                    .ToList();
//            }
//            else
//            {
//                Articles = tousLesArticles;
//            }
//        }
//    }
//}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestionCommerciale.Pages.Articles
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        // Propriétés de recherche et filtrage
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedCategory { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SelectedStockFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "name";

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 25;

        // Données
        public List<ArticleListDto> Articles { get; set; } = new();
        public List<string> Categories { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await LoadCategories();
                await LoadArticles();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la liste d'articles");
                TempData["ErrorMessage"] = "Erreur lors du chargement des articles.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int articleId)
        {
            try
            {
                _logger.LogInformation($"Article supprimé - ID: {articleId}");
                TempData["SuccessMessage"] = "L'article a été supprimé avec succès.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'article");
                TempData["ErrorMessage"] = "Erreur lors de la suppression de l'article.";
                return RedirectToPage("./Index");
            }
        }

        #region Méthodes privées

        private async Task LoadCategories()
        {
            await Task.Delay(50);

            Categories = new List<string>
            {
                "Informatique",
                "Bureautique",
                "Fournitures",
                "Mobilier",
                "Outils",
                "Services"
            };
        }

        private async Task LoadArticles()
        {
            // Simulation du chargement
            await Task.Delay(100);

            // Données simulées
            var allArticles = new List<ArticleListDto>
            {
                new()
                {
                    Id = 1,
                    Reference = "LAPTOP-DELL-XPS13",
                    Barcode = "8717285912345",
                    Name = "Ordinateur Portable Dell XPS 13",
                    Category = "Informatique",
                    Brand = "Dell",
                    Model = "XPS 13",
                    CurrentStock = 45,
                    StockMinimum = 5,
                    Unit = "pcs",
                    IsActive = true,
                    SupplierCount = 2,
                    CreatedAt = DateTime.Now.AddMonths(-6),
                    StockStatus = "good"
                },
                new()
                {
                    Id = 2,
                    Reference = "MONITOR-LG-27",
                    Barcode = "8806098123456",
                    Name = "Écran LG 27 pouces",
                    Category = "Informatique",
                    Brand = "LG",
                    Model = "27 IPS",
                    CurrentStock = 12,
                    StockMinimum = 5,
                    Unit = "pcs",
                    IsActive = true,
                    SupplierCount = 1,
                    CreatedAt = DateTime.Now.AddMonths(-4),
                    StockStatus = "good"
                },
                new()
                {
                    Id = 3,
                    Reference = "SOURIS-LOGITECH",
                    Barcode = "5099206061123",
                    Name = "Souris sans fil Logitech",
                    Category = "Informatique",
                    Brand = "Logitech",
                    Model = "MX Master 3",
                    CurrentStock = 2,
                    StockMinimum = 10,
                    Unit = "pcs",
                    IsActive = true,
                    SupplierCount = 3,
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    StockStatus = "low"
                },
                new()
                {
                    Id = 4,
                    Reference = "CLAVIER-MECH",
                    Barcode = "4718755094726",
                    Name = "Clavier mécanique RGB",
                    Category = "Informatique",
                    Brand = "Corsair",
                    Model = "K95",
                    CurrentStock = 0,
                    StockMinimum = 5,
                    Unit = "pcs",
                    IsActive = true,
                    SupplierCount = 2,
                    CreatedAt = DateTime.Now.AddMonths(-2),
                    StockStatus = "out"
                },
                new()
                {
                    Id = 5,
                    Reference = "PAPIER-A4",
                    Barcode = "5028252012475",
                    Name = "Papier A4 blanc 80g (500 feuilles)",
                    Category = "Fournitures",
                    Brand = "Canson",
                    Model = string.Empty,
                    CurrentStock = 250,
                    StockMinimum = 100,
                    Unit = "box",
                    IsActive = true,
                    SupplierCount = 1,
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    StockStatus = "good"
                },
                new()
                {
                    Id = 6,
                    Reference = "STYLO-BIC",
                    Barcode = "3086123456789",
                    Name = "Stylo bille BIC cristal (boîte 50)",
                    Category = "Fournitures",
                    Brand = "BIC",
                    Model = "Cristal",
                    CurrentStock = 150,
                    StockMinimum = 50,
                    Unit = "box",
                    IsActive = true,
                    SupplierCount = 2,
                    CreatedAt = DateTime.Now.AddDays(-30),
                    StockStatus = "good"
                },
                new()
                {
                    Id = 7,
                    Reference = "BUREAU-OFFICE",
                    Barcode = "1234567890123",
                    Name = "Bureau en bois 140x60cm",
                    Category = "Mobilier",
                    Brand = "IKEA",
                    Model = "Bekant",
                    CurrentStock = 8,
                    StockMinimum = 3,
                    Unit = "pcs",
                    IsActive = true,
                    SupplierCount = 1,
                    CreatedAt = DateTime.Now.AddDays(-15),
                    StockStatus = "good"
                },
                new()
                {
                    Id = 8,
                    Reference = "CHAISE-OFFICE",
                    Barcode = "5555555555555",
                    Name = "Chaise de bureau ergonomique",
                    Category = "Mobilier",
                    Brand = "Herman Miller",
                    Model = "Aeron",
                    CurrentStock = 0,
                    StockMinimum = 2,
                    Unit = "pcs",
                    IsActive = false,
                    SupplierCount = 1,
                    CreatedAt = DateTime.Now.AddDays(-20),
                    StockStatus = "out"
                },
                new()
                {
                    Id = 9,
                    Reference = "CABLE-HDMI",
                    Barcode = "6666666666666",
                    Name = "Câble HDMI 2.1 haute performance",
                    Category = "Informatique",
                    Brand = "Belkin",
                    Model = "2.1",
                    CurrentStock = 80,
                    StockMinimum = 20,
                    Unit = "pcs",
                    IsActive = true,
                    SupplierCount = 2,
                    CreatedAt = DateTime.Now.AddDays(-10),
                    StockStatus = "good"
                },
                new()
                {
                    Id = 10,
                    Reference = "CARTRIDGE-NOIR",
                    Barcode = "7777777777777",
                    Name = "Cartouche d'encre noire pour imprimante",
                    Category = "Fournitures",
                    Brand = "HP",
                    Model = "CF279A",
                    CurrentStock = 25,
                    StockMinimum = 10,
                    Unit = "pcs",
                    IsActive = true,
                    SupplierCount = 1,
                    CreatedAt = DateTime.Now.AddDays(-5),
                    StockStatus = "good"
                }
            };

            // Filtrer
            var filtered = ApplyFilters(allArticles);

            // Trier
            var sorted = ApplySorting(filtered);

            // Paginer
            TotalRecords = sorted.Count;
            CurrentPage = Math.Max(1, Page);
            TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
            CurrentPage = Math.Min(CurrentPage, TotalPages);

            Articles = sorted
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private List<ArticleListDto> ApplyFilters(List<ArticleListDto> articles)
        {
            var result = articles;

            // Filtrer par terme de recherche
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var search = SearchTerm.ToLower();
                result = result.Where(a =>
                    a.Reference.ToLower().Contains(search) ||
                    a.Barcode.ToLower().Contains(search) ||
                    a.Name.ToLower().Contains(search) ||
                    (!string.IsNullOrEmpty(a.Brand) && a.Brand.ToLower().Contains(search))
                ).ToList();
            }

            // Filtrer par catégorie
            if (!string.IsNullOrEmpty(SelectedCategory))
            {
                result = result.Where(a => a.Category == SelectedCategory).ToList();
            }

            // Filtrer par statut
            if (!string.IsNullOrEmpty(SelectedStatus))
            {
                result = result.Where(a =>
                    a.IsActive == (SelectedStatus == "active")
                ).ToList();
            }

            // Filtrer par stock
            if (!string.IsNullOrEmpty(SelectedStockFilter))
            {
                result = result.Where(a => a.StockStatus == SelectedStockFilter).ToList();
            }

            return result;
        }

        private List<ArticleListDto> ApplySorting(List<ArticleListDto> articles)
        {
            return SortBy switch
            {
                "name_desc" => articles.OrderByDescending(a => a.Name).ToList(),
                "reference" => articles.OrderBy(a => a.Reference).ToList(),
                "newest" => articles.OrderByDescending(a => a.CreatedAt).ToList(),
                "oldest" => articles.OrderBy(a => a.CreatedAt).ToList(),
                _ => articles.OrderBy(a => a.Name).ToList()
            };
        }

        #endregion
    }

    #region DTOs

    public class ArticleListDto
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
        public string StockStatus { get; set; } = "good"; // good, low, out
    }

    #endregion
}