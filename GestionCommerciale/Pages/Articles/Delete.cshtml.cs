using GestionCommerciale.Models;
using GestionCommerciale.Services;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Data;

namespace GestionCommerciale.Pages.Articles
{
    /// <summary>Page de confirmation avant la suppression d'un article. Le vrai contrôle est refait par le service.</summary>
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IArticleService _articleService;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(ApplicationDbContext context, IArticleService articleService, ILogger<DeleteModel> logger)
        {
            _context = context;
            _articleService = articleService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public Article Article { get; private set; } = null!;

        public UtilisationArticle Utilisation { get; private set; } = new();

        /// <summary>Indication affichée seulement : le vrai contrôle est refait par le service à la suppression.</summary>
        public bool SuppressionImpossible => Utilisation.SuppressionImpossible;

        /// <summary>Message de refus à afficher (vide si l'article est supprimable).</summary>
        public string MessageRefus => SuppressionImpossible
            ? ArticleService.MessageSuppressionRefusee(Article.Reference, Utilisation)
            : string.Empty;

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            return await ChargerAsync(cancellationToken) ?? Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            try
            {
                var resultat = await _articleService.SupprimerAsync(Id, cancellationToken);

                if (resultat.Introuvable)
                {
                    TempData["ErrorMessage"] = "Article introuvable.";
                    return RedirectToPage("./Index");
                }

                if (!resultat.Succes)
                {
                    // Refus : on reste sur la page, avec le message explicite. Rien n'a été supprimé.
                    foreach (var (_, message) in resultat.Erreurs)
                        ModelState.AddModelError(string.Empty, message);

                    return await ChargerAsync(cancellationToken) ?? Page();
                }

                TempData["SuccessMessage"] = $"Article {resultat.Reference} supprimé.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'article {ArticleId}", Id);
                ModelState.AddModelError(string.Empty, "Une erreur est survenue lors de la suppression.");
                return await ChargerAsync(cancellationToken) ?? Page();
            }
        }

        /// <summary>Charge l'article à afficher. Renvoie une redirection s'il n'existe pas, sinon null.</summary>
        private async Task<IActionResult?> ChargerAsync(CancellationToken cancellationToken)
        {
            var article = await _context.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.ID == Id, cancellationToken);
            if (article == null)
            {
                TempData["ErrorMessage"] = "Article introuvable.";
                return RedirectToPage("./Index");
            }

            Article = article;
            Utilisation = await _articleService.ObtenirUtilisationAsync(Id, cancellationToken);
            return null;
        }
    }
}
