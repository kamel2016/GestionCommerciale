//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using GestionCommerciale.Models;
//using GestionCommerciale.ViewModel.DTO;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.EntityFrameworkCore;
//using GestionCommerciale.Data;

//namespace GestionCommerciale.Pages.Articles
//{
//    public class EditModel : PageModel
//    {
//        private readonly ApplicationDbContext _context;

//        public EditModel(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        [BindProperty]
//        public CommandeClient CommandeClient { get; set; } = default!;

//        [BindProperty]
//        public List<LigneCommandeClientDto> LignesCommande { get; set; } = new List<LigneCommandeClientDto>();

//        public SelectList ClientsSelectList { get; set; } = default!;
//        public List<Article> Articles { get; set; } = default!;

//        public async Task<IActionResult> OnGetAsync(int? id)
//        {
//            if (id == null || _context.CommandesClient == null)
//            {
//                return NotFound();
//            }

//            var commandeclient = await _context.CommandesClient
//                .Include(c => c.LignesCommande)
//                .ThenInclude(l => l.Article)
//                .FirstOrDefaultAsync(m => m.Id == id);

//            if (commandeclient == null)
//            {
//                return NotFound();
//            }

//            CommandeClient = commandeclient;
//            LignesCommande = commandeclient.LignesCommande.Select(l => new LigneCommandeClientDto
//            {
//                ArticleId = l.ArticleId,
//                ArticleNom = l.Article.Libelle,
//                Quantite = l.Quantite,
//                PrixUnitaire = l.PrixUnitaire,
//                MontantLigne = l.MontantLigne
//            }).ToList();

//            await LoadSelectLists();
//            return Page();
//        }

//        public async Task<IActionResult> OnPostAsync()
//        {
//            if (!ModelState.IsValid)
//            {
//                await LoadSelectLists();
//                return Page();
//            }

//            CommandeClient.DateModification = DateTime.Now;

//            // Calculer les montants
//            CommandeClient.MontantHT = LignesCommande.Sum(l => l.MontantLigne);
//            CommandeClient.MontantTVA = CommandeClient.MontantHT * 0.20m;
//            CommandeClient.MontantTTC = CommandeClient.MontantHT + CommandeClient.MontantTVA;

//            _context.Attach(CommandeClient).State = EntityState.Modified;

//            // Supprimer les anciennes lignes
//            var anciennesLignes = await _context.LignesCommandeClient
//                .Where(l => l.CommandeClientId == CommandeClient.Id)
//                .ToListAsync();
//            _context.LignesCommandeClient.RemoveRange(anciennesLignes);

//            // Ajouter les nouvelles lignes
//            foreach (var ligneDto in LignesCommande)
//            {
//                var ligne = new LigneCommandeClient
//                {
//                    CommandeClientId = CommandeClient.Id,
//                    ArticleId = ligneDto.ArticleId,
//                    Quantite = ligneDto.Quantite,
//                    PrixUnitaire = ligneDto.PrixUnitaire,
//                    MontantLigne = ligneDto.MontantLigne
//                };
//                _context.LignesCommandeClient.Add(ligne);
//            }

//            try
//            using Microsoft.AspNetCore.Mvc;
using GestionCommerciale.ViewModel;
using GestionCommerciale.ViewModel.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Pages.Articles
{
    public class EditModel : PageModel
    {
        private readonly ILogger<EditModel> _logger;

        public EditModel(ILogger<EditModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public ArticleViewModel Article { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        // Listes pour les dropdowns
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Units { get; set; } = new();

        // Données de tarification
        public List<SupplierPricingDto> SupplierPricings { get; set; } = new();
        public List<SupplierOptionDto> AvailableSuppliers { get; set; } = new();

        public string PageTitle => Article.Id > 0 ? "Modifier l'article" : "Nouvel article";

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await InitializeSelectLists();

                if (Id.HasValue && Id.Value > 0)
                {
                    await LoadArticle(Id.Value);
                }
                else
                {
                    Article = new ArticleViewModel
                    {
                        IsActive = true,
                        VatRate = 20,
                        Unit = "pcs",
                        CreatedAt = DateTime.Now
                    };
                }

                await LoadSupplierPricings();
                await LoadAvailableSuppliers();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la page article");
                TempData["ErrorMessage"] = "Erreur lors du chargement des données.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await InitializeSelectLists();
                    await LoadSupplierPricings();
                    await LoadAvailableSuppliers();
                    return Page();
                }

                // Valider les données
                //await ValidateArticleData();

                if (!ModelState.IsValid)
                {
                    await InitializeSelectLists();
                    await LoadSupplierPricings();
                    await LoadAvailableSuppliers();
                    return Page();
                }

                // Sauvegarder l'article
                //var articleId = await SaveArticle();

                //_logger.LogInformation($"Article sauvegardé - ID: {articleId}, Ref: {Article.Reference}");
                //TempData["SuccessMessage"] = $"L'article {Article.Name} a été sauvegardé avec succès.";

                return RedirectToPage("./Edit", new { id = 1 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde de l'article");
                ModelState.AddModelError("", "Erreur lors de la sauvegarde: " + ex.Message);
                await InitializeSelectLists();
                await LoadSupplierPricings();
                await LoadAvailableSuppliers();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (Article.Id==null || Article.Id == 0)
                return NotFound();

            try
            {
                _logger.LogInformation($"Article supprimé - ID: {Article.Id}");
                TempData["SuccessMessage"] = $"L'article a été supprimé avec succès.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'article");
                TempData["ErrorMessage"] = "Erreur lors de la suppression.";
                return RedirectToPage("./Edit", new { id = Article.Id });
            }
        }

        public async Task<IActionResult> OnPostRemoveSupplierAsync(int supplierId)
        {
            if (Article.Id == null || Article.Id == 0)
                return BadRequest();

            try
            {
                // Simulation de suppression du fournisseur
                _logger.LogInformation($"Fournisseur {supplierId} retiré de l'article {Article.Id}");
                TempData["SuccessMessage"] = "Le fournisseur a été retiré de cet article.";
                return RedirectToPage("./Edit", new { id = Article.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du retrait du fournisseur");
                return BadRequest(new { error = ex.Message });
            }
        }

        #region Méthodes privées

        private async Task InitializeSelectLists()
        {
            Categories = new List<SelectListItem>
            {
                new() { Text = "-- Sélectionner une catégorie --", Value = "" },
                new() { Text = "Informatique", Value = "Informatique" },
                new() { Text = "Bureautique", Value = "Bureautique" },
                new() { Text = "Fournitures", Value = "Fournitures" },
                new() { Text = "Mobilier", Value = "Mobilier" },
                new() { Text = "Outils", Value = "Outils" },
                new() { Text = "Services", Value = "Services" }
            };

            Units = new List<SelectListItem>
            {
                new() { Text = "-- Sélectionner une unité --", Value = "" },
                new() { Text = "Pièce(s)", Value = "pcs" },
                new() { Text = "Kilogramme(s)", Value = "kg" },
                new() { Text = "Litre(s)", Value = "l" },
                new() { Text = "Mètre(s)", Value = "m" },
                new() { Text = "Boîte(s)", Value = "box" },
                new() { Text = "Paquet(s)", Value = "pack" }
            };

            await Task.CompletedTask;
        }

        private async Task LoadArticle(int articleId)
        {
            // Simulation de chargement
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
                Description = "Ordinateur portable professionnel haute performance...",
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

        private async Task LoadAvailableSuppliers()
        {
            // Simulation de chargement des fournisseurs disponibles
            await Task.Delay(100);

            var AvailableSuppliers = new List<SupplierOptionDto>
                {
                    new() { Id = 1, Name = "TECH SUPPLIES SARL", Code = "FOUR001" },
                    new() { Id = 2, Name = "FOURNITURES INDUSTRIELLES", Code = "FOUR002" },
                    new() { Id = 3, Name = "GLOBAL TECH DISTRIBUTION", Code = "FOUR003" },
                    new() { Id = 4, Name = "EURO MATÉRIAUX", Code = "FOUR004" },
                    new() { Id = 5, Name = "LOGISTIQUE & CO", Code = "FOUR005" }
                };
        }
        #endregion
    }
}