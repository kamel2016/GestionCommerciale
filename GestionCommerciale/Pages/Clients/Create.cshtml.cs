using GestionCommerciale.Models;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestionCommerciale.Pages.Clients
{
    public class CreateModel : PageModel
    {
        private readonly IClientService _clientService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(IClientService clientService, ILogger<CreateModel> logger)
        {
            _clientService = clientService;
            _logger = logger;
        }

        [BindProperty]
        public Client Client { get; set; } = new Client();

        public static readonly string[] Gouvernorats =
        {
            "Ariana", "Béja", "Ben Arous", "Bizerte", "Gabès", "Gafsa", "Jendouba",
            "Kairouan", "Kasserine", "Kébili", "Le Kef", "Mahdia", "La Manouba",
            "Médenine", "Monastir", "Nabeul", "Sfax", "Sidi Bouzid", "Siliana",
            "Sousse", "Tataouine", "Tozeur", "Tunis", "Zaghouan"
        };

        public IActionResult OnGet() => Page();

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
        {
            // Le code est généré par le service : il ne vient pas du formulaire.
            ModelState.Remove("Client.Code");
            // Collection de navigation : ne doit pas être validée.
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
                var resultat = await _clientService.CreerAsync(Client, cancellationToken);

                if (!resultat.Succes)
                {
                    foreach (var (champ, message) in resultat.Erreurs)
                    {
                        ModelState.AddModelError(
                            string.IsNullOrEmpty(champ) ? string.Empty : $"Client.{champ}", message);
                    }
                    return Page();
                }

                TempData["SuccessMessage"] = $"Client « {Client.NomComplet} » créé avec succès (code {Client.Code}).";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du client");
                ModelState.AddModelError(string.Empty, "Une erreur est survenue lors de l'enregistrement.");
                return Page();
            }
        }
    }
}
