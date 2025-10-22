using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionCommerciale.Pages.GoodsReceipt
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        // Propriétés de pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        // Propriétés de filtrage
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        // Données
        public List<GoodsReceiptViewModel> GoodsReceipts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                CurrentPage = Page > 0 ? Page : 1;
                await LoadGoodsReceipts();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la liste des bons de réception");
                TempData["ErrorMessage"] = "Erreur lors du chargement des données.";
                return Page();
            }
        }

        #region Méthodes privées

        private async Task LoadGoodsReceipts()
        {
            // Simulation de chargement depuis la base de données
            await Task.Delay(100);

            // Données de démonstration
            var allReceipts = new List<GoodsReceiptViewModel>
            {
                new()
                {
                    Id = 1,
                    InternalNumber = "BR-2025-001",
                    SupplierCode = "F055",
                    SupplierName = "SOMOCEL",
                    SupplierReference = "6066",
                    ReceiptDate = DateTime.Now.AddDays(-5),
                    ReceiptTime = new TimeSpan(10, 40, 0),
                    AmountExcludingTax = 2661.282m,
                    AmountIncludingTax = 3193.538m,
                    Status = "Validé",
                    CreatedBy = "Admin",
                    CreatedAt = DateTime.Now.AddDays(-5)
                },
                new()
                {
                    Id = 2,
                    InternalNumber = "BR-2025-002",
                    SupplierCode = "F032",
                    SupplierName = "TECH SUPPLIES SARL",
                    SupplierReference = "5432",
                    ReceiptDate = DateTime.Now.AddDays(-3),
                    ReceiptTime = new TimeSpan(14, 15, 30),
                    AmountExcludingTax = 1850.750m,
                    AmountIncludingTax = 2220.900m,
                    Status = "Liquidé",
                    CreatedBy = "User1",
                    CreatedAt = DateTime.Now.AddDays(-3)
                },
                new()
                {
                    Id = 3,
                    InternalNumber = "BR-2025-003",
                    SupplierCode = "F001",
                    SupplierName = "FOURNITURES BUREAU PLUS",
                    SupplierReference = "7890",
                    ReceiptDate = DateTime.Now.AddDays(-1),
                    ReceiptTime = new TimeSpan(09, 30, 0),
                    AmountExcludingTax = 945.300m,
                    AmountIncludingTax = 1134.360m,
                    Status = "Brouillon",
                    CreatedBy = "User2",
                    CreatedAt = DateTime.Now.AddDays(-1)
                },
                new()
                {
                    Id = 4,
                    InternalNumber = "BR-2025-004",
                    SupplierCode = "F055",
                    SupplierName = "SOMOCEL",
                    SupplierReference = "6067",
                    ReceiptDate = DateTime.Now,
                    ReceiptTime = new TimeSpan(11, 0, 0),
                    AmountExcludingTax = 3200.000m,
                    AmountIncludingTax = 3840.000m,
                    Status = "Validé",
                    CreatedBy = "Admin",
                    CreatedAt = DateTime.Now
                },
                new()
                {
                    Id = 5,
                    InternalNumber = "BR-2025-005",
                    SupplierCode = "F020",
                    SupplierName = "DISTRIBUTEUR GÉNÉRAL",
                    SupplierReference = "4567",
                    ReceiptDate = DateTime.Now.AddDays(-7),
                    ReceiptTime = new TimeSpan(15, 45, 0),
                    AmountExcludingTax = 5000.000m,
                    AmountIncludingTax = 6000.000m,
                    Status = "Annulé",
                    CreatedBy = "User3",
                    CreatedAt = DateTime.Now.AddDays(-7)
                }
            };

            // Appliquer les filtres
            var filtered = allReceipts.AsQueryable();

            // Filtre par terme de recherche
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                filtered = filtered.Where(r =>
                    r.InternalNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    r.SupplierName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    r.SupplierCode.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
                );
            }

            // Filtre par statut
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                filtered = filtered.Where(r => r.Status == StatusFilter);
            }

            // Filtre par date
            if (DateFrom.HasValue)
            {
                filtered = filtered.Where(r => r.ReceiptDate >= DateFrom.Value);
            }

            if (DateTo.HasValue)
            {
                filtered = filtered.Where(r => r.ReceiptDate <= DateTo.Value.AddDays(1));
            }

            // Compter le total
            TotalRecords = filtered.Count();
            TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);

            // Appliquer la pagination
            GoodsReceipts = filtered
                .OrderByDescending(r => r.ReceiptDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            _logger.LogInformation($"Chargement de {GoodsReceipts.Count} bons de réception (Page {CurrentPage})");
        }

        #endregion
    }
}