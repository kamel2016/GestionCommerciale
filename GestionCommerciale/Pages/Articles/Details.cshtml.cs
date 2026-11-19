using System.ComponentModel.DataAnnotations;
using System.Reflection;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestionCommerciale.Pages.Articles
{
    /// <summary>
    /// Fiche d'un article : données RÉELLES, lues par ArticleService (article, catégorie, tarifs par fournisseur, derniers
    /// mouvements de stock). Aucune donnée d'exemple.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly IArticleService _articleService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(IArticleService articleService, ILogger<DetailsModel> logger)
        {
            _articleService = articleService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        public FicheArticle Fiche { get; private set; } = null!;

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            if (!Id.HasValue || Id.Value <= 0)
                return NotFound();

            try
            {
                var fiche = await _articleService.ObtenirFicheAsync(Id.Value, cancellationToken);
                if (fiche == null)
                    return NotFound();

                Fiche = fiche;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des détails de l'article {ArticleId}", Id);
                TempData["ErrorMessage"] = "Erreur lors du chargement de l'article.";
                return RedirectToPage("./Index");
            }
        }

        /// <summary>Libellé français d'un type de mouvement de stock (attribut Display de l'énumération).</summary>
        public static string LibelleType(TypeMouvementStock type)
            => type.GetType().GetField(type.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? type.ToString();

        /// <summary>Marge détail en % : (prix détail - prix gros) / prix gros, 0 si le prix gros est nul (même formule que la fiche de modification).</summary>
        public static decimal MargeDetail(decimal prixGrosHT, decimal prixDetailHT)
            => prixGrosHT > 0 ? (prixDetailHT - prixGrosHT) / prixGrosHT * 100 : 0;
    }
}
