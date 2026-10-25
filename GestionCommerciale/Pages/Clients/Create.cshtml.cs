using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Clients
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
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
            // Le prénom n'a de sens que pour un particulier
            if (Client.TypeClient == TypeClient.Societe)
            {
                Client.Prenom = null;
                ModelState.Remove("Client.Prenom");
            }

            // Collection de navigation : ne doit pas être validée
            ModelState.Remove("Client.CommandesClient");

            if (!string.IsNullOrWhiteSpace(Client.Email))
            {
                var emailExiste = await _context.Clients
                    .AnyAsync(c => c.Email == Client.Email, cancellationToken);
                if (emailExiste)
                    ModelState.AddModelError("Client.Email", "Un client avec cet email existe déjà.");
            }

            if (!ModelState.IsValid)
                return Page();

            try
            {
                Client.DateCreation = DateTime.Now;
                Client.DateModification = null;
                Client.EstActif = true;

                _context.Clients.Add(Client);
                await _context.SaveChangesAsync(cancellationToken);

                TempData["SuccessMessage"] = $"Client « {Client.NomComplet} » créé avec succès.";
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