using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestionCommerciale.Pages.Clients
{
    /// <summary>Fiche client en lecture seule (données réelles de la base).</summary>
    public class ClientDetailsModel : PageModel
    {
        private readonly IClientService _clientService;

        public ClientDetailsModel(IClientService clientService)
        {
            _clientService = clientService;
        }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        public FicheClient Fiche { get; private set; } = null!;

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            if (Id == null || Id <= 0)
            {
                TempData["ErrorMessage"] = "Client introuvable.";
                return RedirectToPage("./Index");
            }

            var fiche = await _clientService.ObtenirFicheAsync(Id.Value, cancellationToken);
            if (fiche == null)
            {
                TempData["ErrorMessage"] = "Client introuvable.";
                return RedirectToPage("./Index");
            }

            Fiche = fiche;
            return Page();
        }
    }
}
