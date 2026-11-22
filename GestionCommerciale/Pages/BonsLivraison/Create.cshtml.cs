// Pages/BonsLivraison/Create.cshtml.cs
using GestionCommerciale.Data;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.BonsLivraison
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly NumerotationService _numerotation;
        private readonly BonLivraisonService _bonLivraisonService;

        public CreateModel(ApplicationDbContext context, NumerotationService numerotation, BonLivraisonService bonLivraisonService)
        {
            _context = context;
            _numerotation = numerotation;
            _bonLivraisonService = bonLivraisonService;
        }

        // Le client n'est PLUS un champ du formulaire : il est lu côté serveur dans la commande (S10b). Aucune valeur postée n'est liée.
        [BindProperty] public int? CommandeClientId { get; set; }
        [BindProperty] public DateTime Date { get; set; } = DateTime.Today;
        [BindProperty] public string? Commentaire { get; set; }
        [BindProperty] public List<LigneInput> Lignes { get; set; } = new();

        /// <summary>Numéro de la commande et nom de son client : affichage seulement.</summary>
        public string? NumeroCommande { get; set; }
        public string? NomClient { get; set; }

        /// <summary>Commandes proposées quand aucune commande n'est choisie (page de sélection).</summary>
        public List<BonLivraisonService.CommandeEligible> CommandesEligibles { get; set; } = new();

        public class LigneInput
        {
            public int ArticleId { get; set; }
            public int? LigneCommandeClientId { get; set; }
            public string Designation { get; set; } = string.Empty;
            public int QuantiteRestante { get; set; }
            public int Stock { get; set; }
            public int Quantite { get; set; }
            public decimal PrixUnitaire { get; set; }   // affichage seulement : jamais relu au POST
        }

        public async Task<IActionResult> OnGetAsync(int? commandeId)
        {
            if (!commandeId.HasValue)
            {
                // Page de sélection : les commandes éligibles (même règle que le serveur), au lieu d'une redirection silencieuse.
                CommandesEligibles = await _bonLivraisonService.CommandesEligiblesAsync();
                return Page();
            }

            var commande = await _context.CommandesClient
                .Include(c => c.Client)
                .Include(c => c.LignesCommande).ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(c => c.Id == commandeId.Value);

            if (commande == null) return NotFound();

            if (!BonLivraisonService.StatutsCommandeAutorisantUnBl.Contains(commande.Statut))
            {
                // Clé « Error » : c'est celle que la liste des commandes (Commande_Client/Index) affiche.
                TempData["Error"] = $"La commande {commande.NumeroCommande} est au statut « {BonLivraisonService.LibelleStatutCommande(commande.Statut)} » : " +
                                    "un bon de livraison n'est possible que pour une commande Confirmée, En préparation ou Expédiée.";
                return RedirectToPage("/Commande_Client/Index");
            }

            CommandeClientId = commande.Id;
            NumeroCommande = commande.NumeroCommande;
            NomClient = commande.Client?.NomComplet;

            // Reste à livrer = quantité commandée moins le cumul des lignes de BL non annulés (même règle que le contrôle du serveur)
            var restes = await _bonLivraisonService.ResteParLigneAsync(commande.Id);
            Lignes = commande.LignesCommande
                .Where(l => restes.GetValueOrDefault(l.Id) > 0)
                .Select(l => new LigneInput
                {
                    ArticleId = l.ArticleId,
                    LigneCommandeClientId = l.Id,
                    Designation = l.Article!.Libelle,
                    QuantiteRestante = restes[l.Id],
                    Stock = l.Article.Quantite,
                    Quantite = Math.Min(restes[l.Id], l.Article.Quantite),
                    PrixUnitaire = l.PrixUnitaire
                }).ToList();

            if (!Lignes.Any())
            {
                TempData["Error"] = "Cette commande est déjà entièrement couverte par des bons de livraison.";
                return RedirectToPage("/Commande_Client/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // On ne garde que les lignes avec une quantité > 0
            Lignes = Lignes.Where(l => l.Quantite > 0).ToList();

            if (!Lignes.Any())
                ModelState.AddModelError(string.Empty, "Saisissez au moins une quantité à livrer.");

            if (!ModelState.IsValid)
            {
                await ChargerEnTeteAsync();
                return Page();
            }

            // Tout est contrôlé et construit côté serveur, dans une transaction, après verrou de la commande : client de la commande,
            // statut autorisé, prix et article de la ligne de commande liée, une ligne de commande une seule fois, reste à livrer.
            // Le numéro n'est demandé qu'après tous les contrôles.
            var (bl, erreur) = await _bonLivraisonService.CreerBrouillonAsync(
                CommandeClientId, Date, Commentaire,
                Lignes.Select(l => (l.LigneCommandeClientId, l.Quantite)),
                () => _numerotation.ProchainNumeroAsync("BL", DateTime.Now));

            if (bl == null)
            {
                ModelState.AddModelError(string.Empty, erreur ?? "Création impossible.");
                await ChargerEnTeteAsync();
                return Page();
            }

            TempData["SuccessMessage"] = $"Bon de livraison {bl.Numero} créé en brouillon.";
            return RedirectToPage("Details", new { id = bl.ID });
        }

        private async Task ChargerEnTeteAsync()
        {
            if (CommandeClientId == null) return;
            var commande = await _context.CommandesClient.AsNoTracking()
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.Id == CommandeClientId.Value);
            NumeroCommande = commande?.NumeroCommande;
            NomClient = commande?.Client?.NomComplet;
        }
    }
}
