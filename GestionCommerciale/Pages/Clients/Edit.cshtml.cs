using GestionCommerciale.Models;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestionCommerciale.Pages.Clients
{
    public class EditModel : PageModel
    {
        private readonly IClientService _clientService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(IClientService clientService, ILogger<EditModel> logger)
        {
            _clientService = clientService;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public Client Client { get; set; } = new Client();

        /// <summary>Code affiché en lecture seule (il ne vient jamais du formulaire).</summary>
        public string CodeClient { get; private set; } = string.Empty;

        /// <summary>Vrai si le client a déjà des documents : son mode de facturation n'est plus modifiable.</summary>
        public bool ModeFacturationVerrouille { get; private set; }

        /// <summary>Mode de facturation actuellement enregistré (celui qui reste affiché si c'est verrouillé).</summary>
        public Enums.ModeFacturation ModeFacturationActuel { get; private set; }

        public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        {
            var client = await _clientService.ObtenirAsync(Id, cancellationToken);
            if (client == null)
            {
                TempData["ErrorMessage"] = "Client introuvable.";
                return RedirectToPage("./Index");
            }

            Client = client;
            CodeClient = client.Code;
            ModeFacturationActuel = client.ModeFacturation;
            ModeFacturationVerrouille = await _clientService.ADesDocumentsAsync(Id, cancellationToken);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            var existant = await _clientService.ObtenirAsync(Id, cancellationToken);
            if (existant == null)
            {
                TempData["ErrorMessage"] = "Client introuvable.";
                return RedirectToPage("./Index");
            }
            CodeClient = existant.Code;
            ModeFacturationActuel = existant.ModeFacturation;
            ModeFacturationVerrouille = await _clientService.ADesDocumentsAsync(Id, cancellationToken);

            // Le code, l'Id et les collections ne viennent pas du formulaire.
            ModelState.Remove("Client.Code");
            ModelState.Remove("Client.Id");
            ModelState.Remove("Client.CommandesClient");

            // Le prénom n'a de sens que pour un particulier.
            if (Client.TypeClient == TypeClient.Societe)
            {
                Client.Prenom = null;
                ModelState.Remove("Client.Prenom");
            }

            if (!ModelState.IsValid)
                return Page();

            try
            {
                var resultat = await _clientService.ModifierAsync(Id, Client, cancellationToken);

                if (resultat.Introuvable)
                {
                    TempData["ErrorMessage"] = "Client introuvable.";
                    return RedirectToPage("./Index");
                }

                if (!resultat.Succes)
                {
                    foreach (var (champ, message) in resultat.Erreurs)
                    {
                        ModelState.AddModelError(
                            string.IsNullOrEmpty(champ) ? string.Empty : $"Client.{champ}", message);
                    }
                    return Page();
                }

                TempData["SuccessMessage"] = $"Client « {Client.NomComplet} » modifié avec succès.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du client {ClientId}", Id);
                ModelState.AddModelError(string.Empty, "Une erreur est survenue lors de l'enregistrement.");
                return Page();
            }
        }
    }
}
