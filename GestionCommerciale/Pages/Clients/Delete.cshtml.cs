using GestionCommerciale.Services;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestionCommerciale.Pages.Clients
{
    /// <summary>Page de confirmation avant la suppression d'un client.</summary>
    public class DeleteModel : PageModel
    {
        private readonly IClientService _clientService;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(IClientService clientService, ILogger<DeleteModel> logger)
        {
            _clientService = clientService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public FicheClient Fiche { get; private set; } = null!;

        /// <summary>Indication affichée seulement : le vrai contrôle est refait par le service à la suppression.</summary>
        public bool SuppressionImpossible { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            return await ChargerAsync(cancellationToken) ?? Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            try
            {
                var resultat = await _clientService.SupprimerAsync(Id, cancellationToken);

                if (resultat.Introuvable)
                {
                    TempData["ErrorMessage"] = "Client introuvable.";
                    return RedirectToPage("./Index");
                }

                if (!resultat.Succes)
                {
                    // Refus : on reste sur la page, avec le message explicite. Rien n'a été supprimé.
                    foreach (var (_, message) in resultat.Erreurs)
                        ModelState.AddModelError(string.Empty, message);

                    return await ChargerAsync(cancellationToken) ?? Page();
                }

                TempData["SuccessMessage"] = $"Client {resultat.Code} supprimé.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du client {ClientId}", Id);
                ModelState.AddModelError(string.Empty, "Une erreur est survenue lors de la suppression.");
                return await ChargerAsync(cancellationToken) ?? Page();
            }
        }

        /// <summary>Charge la fiche à afficher. Renvoie une redirection si le client n'existe pas, sinon null.</summary>
        private async Task<IActionResult?> ChargerAsync(CancellationToken cancellationToken)
        {
            var fiche = await _clientService.ObtenirFicheAsync(Id, cancellationToken);
            if (fiche == null)
            {
                TempData["ErrorMessage"] = "Client introuvable.";
                return RedirectToPage("./Index");
            }

            Fiche = fiche;
            SuppressionImpossible = await _clientService.ADesDocumentsAsync(Id, cancellationToken);
            return null;
        }
    }
}
