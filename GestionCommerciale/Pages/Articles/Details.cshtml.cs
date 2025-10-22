//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using GestionCommerciale.Models;

//namespace GestionCommerciale.Pages.Articles
//{
//    public class DetailsModel : PageModel
//    {
//        public Article? Article { get; set; }

//        public IActionResult OnGet(int id)
//        {
//            // Données d'exemple - en production, vous utiliseriez une base de données
//            var articles = new List<Article>
//            {
//                new Article { ID = 1, Reference = "ART001", Libelle = "Ordinateur portable", CodeBarre = "123456789", Quantite = 10, PrixVenteHT = 800.00m, PrixVenteTTC = 960.00m },
//                new Article { ID = 2, Reference = "ART002", Libelle = "Souris sans fil", CodeBarre = "987654321", Quantite = 25, PrixVenteHT = 25.00m, PrixVenteTTC = 30.00m },
//                new Article { ID = 3, Reference = "ART003", Libelle = "Clavier mécanique", CodeBarre = "456789123", Quantite = 15, PrixVenteHT = 120.00m, PrixVenteTTC = 144.00m },
//                new Article { ID = 4, Reference = "ART004", Libelle = "Écran 24 pouces", CodeBarre = "789123456", Quantite = 8, PrixVenteHT = 200.00m, PrixVenteTTC = 240.00m },
//                new Article { ID = 5, Reference = "ART005", Libelle = "Imprimante laser", CodeBarre = "321654987", Quantite = 5, PrixVenteHT = 300.00m, PrixVenteTTC = 360.00m }
//            };

//            Article = articles.FirstOrDefault(a => a.ID == id);

//            if (Article == null)
//            {
//                return NotFound();
//            }

//            return Page();
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.ViewModel;
using GestionCommerciale.ViewModel.DTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestionCommerciale.Pages.Articles
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

        public ArticleViewModel Article { get; set; } = new();

        // Données de tarification
        public List<SupplierPricingDto> SupplierPricings { get; set; } = new();

        // Informations de stock détaillées
        public StockHistoryDto StockHistory { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (!Id.HasValue || Id.Value <= 0)
                {
                    return NotFound();
                }

                await LoadArticle(Id.Value);
                await LoadSupplierPricings();
                await LoadStockHistory();

                if (Article.Id == 0)
                {
                    return NotFound();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du chargement des détails de l'article {Id}");
                TempData["ErrorMessage"] = "Erreur lors du chargement de l'article.";
                return RedirectToPage("./Index");
            }
        }

        #region Méthodes privées

        private async Task LoadArticle(int articleId)
        {
            // Simulation de chargement depuis la base de données
            await Task.Delay(100);

            Article = new ArticleViewModel
            {
                Id = articleId,
                Reference = "LAPTOP-DELL-XPS13",
                Barcode = "8717285912345",
                Name = "Ordinateur Portable Dell XPS 13",
                Category = "Informatique",
                Unit = "pcs",
                Brand = "Dell",
                Model = "XPS 13",
                Color = "Argent",
                Size = "13 pouces",
                Weight = 1.20m,
                VatRate = 20,
                Description = "Ordinateur portable professionnel haute performance avec processeur Intel Core i7, 16GB de RAM et SSD 512GB. Écran FHD 13\" très lumineux et batterie longue durée. Idéal pour les professionnels mobiles.",
                StockMinimum = 5,
                StockMaximum = 50,
                CurrentStock = 45,
                ReservedStock = 2,
                OnOrderStock = 10,
                IsActive = true,
                CreatedAt = DateTime.Now.AddMonths(-6),
                UpdatedAt = DateTime.Now.AddDays(-2),
                LastStockUpdate = DateTime.Now.AddHours(-3)
            };
        }

        private async Task LoadSupplierPricings()
        {
            // Simulation de chargement des tarifs fournisseurs
            await Task.Delay(100);

            SupplierPricings = new List<SupplierPricingDto>
            {
                new()
                {
                    SupplierId = 1,
                    SupplierCode = "FOUR001",
                    SupplierName = "TECH SUPPLIES SARL",
                    WholesalePrice = 699.99m,
                    RetailPrice = 899.99m,
                    PromotionalPrice = 749.99m,
                    RetailMargin = 28.57m,
                    RetailMaxDiscount = 10,
                    LastPurchaseDate = DateTime.Now.AddDays(-5)
                },
                new()
                {
                    SupplierId = 2,
                    SupplierCode = "FOUR002",
                    SupplierName = "FOURNITURES BUREAU PLUS",
                    WholesalePrice = 695.00m,
                    RetailPrice = 899.99m,
                    PromotionalPrice = 0,
                    RetailMargin = 29.41m,
                    RetailMaxDiscount = 8,
                    LastPurchaseDate = DateTime.Now.AddDays(-15)
                }
            };
        }

        private async Task LoadStockHistory()
        {
            // Simulation de chargement de l'historique de stock
            await Task.Delay(100);

            StockHistory = new StockHistoryDto
            {
                TotalMovements = 25,
                LastMovements = new List<StockMovementDto>
                {
                    new()
                    {
                        Date = DateTime.Now.AddHours(-3),
                        Type = "Réception",
                        Quantity = 10,
                        Reference = "BL-2025-001",
                        Comment = "Réception fournisseur TECH SUPPLIES"
                    },
                    new()
                    {
                        Date = DateTime.Now.AddDays(-1),
                        Type = "Vente",
                        Quantity = -2,
                        Reference = "VN-2025-145",
                        Comment = "Commande client #1245"
                    },
                    new()
                    {
                        Date = DateTime.Now.AddDays(-3),
                        Type = "Réservation",
                        Quantity = -2,
                        Reference = "RES-2025-089",
                        Comment = "Réservation pour commande #1240"
                    },
                    new()
                    {
                        Date = DateTime.Now.AddDays(-5),
                        Type = "Réception",
                        Quantity = 25,
                        Reference = "BL-2025-002",
                        Comment = "Réception fournisseur FOURNITURES BUREAU PLUS"
                    }
                }
            };
        }

        #endregion
    }
}