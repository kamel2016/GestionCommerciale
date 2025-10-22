using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using GestionCommerciale.ViewModel;
using GestionCommerciale.ViewModel.DTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestionCommerciale.Pages.Articles
{
    public class CreateModel : PageModel
    {
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ILogger<CreateModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public ArticleViewModel Article { get; set; } = new();

        // Listes pour les dropdowns
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Units { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await InitializeSelectLists();

                // Initialiser un nouvel article avec les valeurs par défaut
                Article = new ArticleViewModel
                {
                    IsActive = true,
                    VatRate = 20,
                    Unit = "pcs",
                    CreatedAt = DateTime.Now
                };

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la page de création");
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
                    return Page();
                }

                // TODO: Implémenter la validation des données
                // await ValidateArticleData();

                if (!ModelState.IsValid)
                {
                    await InitializeSelectLists();
                    return Page();
                }

                // TODO: Implémenter la sauvegarde de l'article en base de données
                // var articleId = await SaveArticle();

                _logger.LogInformation($"Article créé - Ref: {Article.Reference}");
                TempData["SuccessMessage"] = $"L'article {Article.Name} a été créé avec succès.";

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'article");
                ModelState.AddModelError("", "Erreur lors de la création: " + ex.Message);
                await InitializeSelectLists();
                return Page();
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

        #endregion
    }
}