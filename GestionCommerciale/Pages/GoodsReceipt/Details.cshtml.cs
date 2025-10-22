using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestionCommerciale.Pages.GoodsReceipt
{
    public class DetailsModel : PageModel
    {
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ILogger<DetailsModel> logger)
        {
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        public GoodsReceiptViewModel GoodsReceipt { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (!Id.HasValue || Id.Value <= 0)
                {
                    return NotFound();
                }

                await LoadGoodsReceipt(Id.Value);

                if (GoodsReceipt.Id == 0)
                {
                    return NotFound();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du chargement du bon de réception {Id}");
                TempData["ErrorMessage"] = "Erreur lors du chargement du bon de réception.";
                return RedirectToPage("./Index");
            }
        }

        #region Méthodes privées

        private async Task LoadGoodsReceipt(int receiptId)
        {
            // Simulation de chargement depuis la base de données
            await Task.Delay(100);

            // Données de démonstration
            GoodsReceipt = new GoodsReceiptViewModel
            {
                Id = receiptId,
                InternalNumber = "BR-2025-001",
                SupplierCode = "F055",
                SupplierName = "SOMOCEL",
                SupplierReference = "6066",
                ReceiptDate = DateTime.Now.AddDays(-5),
                ReceiptTime = new TimeSpan(10, 40, 0),
                InvoiceDate = DateTime.Now.AddDays(-4),
                OrderDate = DateTime.Now.AddDays(-10),
                OrderNumber = "COM-2025-001",
                AmountExcludingTax = 2661.282m,
                AmountToReceive = 2661.282m,
                AmountPaid = 0m,
                AmountRegularized = 0m,
                AmountRemaining = 2661.282m,
                AmountCharged = 0m,
                AmountIncludingTax = 3193.538m,
                CreditNote = 0m,
                VatRate = 20,
                IsVatSubject = true,
                Currency = "EUR",
                ExchangeRate = 1m,
                Status = "Validé",
                Comments = "Réception conforme à la commande. Tous les articles ont été vérifiés et acceptés.",
                CreatedAt = DateTime.Now.AddDays(-5),
                UpdatedAt = DateTime.Now.AddDays(-4),
                CreatedBy = "Admin",
                Lines = new List<GoodsReceiptLineViewModel>
                {
                    new()
                    {
                        Id = 1,
                        LineNumber = 1,
                        ArticleCode = "198001008",
                        ArticleDescription = "TRIPLITE 2P+T",
                        SupplierArticleCode = "27041",
                        UnitPrice = 4.474m,
                        Quantity = 160,
                        Unit = "PIEC",
                        VatRate = 25,
                        DiscountPercent = 0,
                        LineAmountExcludingTax = 715.840m,
                        LineVatAmount = 178.960m,
                        LineAmountIncludingTax = 894.800m,
                        Comments = ""
                    },
                    new()
                    {
                        Id = 2,
                        LineNumber = 2,
                        ArticleCode = "103001020",
                        ArticleDescription = "INT SIMPLE ALLUAGE APPARAN",
                        SupplierArticleCode = "",
                        UnitPrice = 2.414m,
                        Quantity = 60,
                        Unit = "PIEC",
                        VatRate = 25,
                        DiscountPercent = 0,
                        LineAmountExcludingTax = 144.840m,
                        LineVatAmount = 36.210m,
                        LineAmountIncludingTax = 181.050m,
                        Comments = ""
                    },
                    new()
                    {
                        Id = 3,
                        LineNumber = 3,
                        ArticleCode = "103001026",
                        ArticleDescription = "PRISE 2POLES",
                        SupplierArticleCode = "APPARAN",
                        UnitPrice = 2.414m,
                        Quantity = 60,
                        Unit = "PIEC",
                        VatRate = 25,
                        DiscountPercent = 0,
                        LineAmountExcludingTax = 144.840m,
                        LineVatAmount = 36.210m,
                        LineAmountIncludingTax = 181.050m,
                        Comments = ""
                    },
                    new()
                    {
                        Id = 4,
                        LineNumber = 4,
                        ArticleCode = "106002008",
                        ArticleDescription = "PRISE 2P+T",
                        SupplierArticleCode = "4310921S",
                        UnitPrice = 3.520m,
                        Quantity = 200,
                        Unit = "",
                        VatRate = 18,
                        DiscountPercent = 0,
                        LineAmountExcludingTax = 704.000m,
                        LineVatAmount = 126.720m,
                        LineAmountIncludingTax = 830.720m,
                        Comments = ""
                    },
                    new()
                    {
                        Id = 5,
                        LineNumber = 5,
                        ArticleCode = "106002001",
                        ArticleDescription = "INT S/A",
                        SupplierArticleCode = "431001 BQ",
                        UnitPrice = 2.742m,
                        Quantity = 200,
                        Unit = "",
                        VatRate = 18,
                        DiscountPercent = 0,
                        LineAmountExcludingTax = 548.400m,
                        LineVatAmount = 98.712m,
                        LineAmountIncludingTax = 647.112m,
                        Comments = ""
                    },
                    new()
                    {
                        Id = 6,
                        LineNumber = 6,
                        ArticleCode = "212002015",
                        ArticleDescription = "MONTURE RECT 3 MOD",
                        SupplierArticleCode = "43A03  S",
                        UnitPrice = 0.667m,
                        Quantity = 400,
                        Unit = "PIEC",
                        VatRate = 18,
                        DiscountPercent = 0,
                        LineAmountExcludingTax = 266.800m,
                        LineVatAmount = 48.024m,
                        LineAmountIncludingTax = 314.824m,
                        Comments = ""
                    },
                    new()
                    {
                        Id = 7,
                        LineNumber = 7,
                        ArticleCode = "106002007",
                        ArticleDescription = "PRISE 2P",
                        SupplierArticleCode = "4310345 B",
                        UnitPrice = 2.884m,
                        Quantity = 100,
                        Unit = "",
                        VatRate = 18,
                        DiscountPercent = 0,
                        LineAmountExcludingTax = 288.400m,
                        LineVatAmount = 51.912m,
                        LineAmountIncludingTax = 340.312m,
                        Comments = ""
                    }
                }
            };

            _logger.LogInformation($"Chargement du bon de réception {GoodsReceipt.InternalNumber}");
        }

        #endregion
    }
}