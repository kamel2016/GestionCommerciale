// Pages/BonsLivraison/Create.cshtml.cs
using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.BonsLivraison
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly NumerotationService _numerotation;

        public CreateModel(ApplicationDbContext context, NumerotationService numerotation)
        {
            _context = context;
            _numerotation = numerotation;
        }

        [BindProperty] public int? ClientId { get; set; }
        [BindProperty] public int? CommandeClientId { get; set; }
        [BindProperty] public DateTime Date { get; set; } = DateTime.Today;
        [BindProperty] public string? Commentaire { get; set; }
        [BindProperty] public List<LigneInput> Lignes { get; set; } = new();

        public SelectList Clients { get; set; } = default!;
        public string? NumeroCommande { get; set; }

        public class LigneInput
        {
            public int ArticleId { get; set; }
            public int? LigneCommandeClientId { get; set; }
            public string Designation { get; set; } = string.Empty;
            public int QuantiteRestante { get; set; }
            public int Stock { get; set; }
            public int Quantite { get; set; }
            public decimal PrixUnitaire { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? commandeId)
        {
            await ChargerClientsAsync();

            if (!commandeId.HasValue)
            {
                TempData["ErrorMessage"] = "Choisissez d'abord une commande client à livrer.";
                return RedirectToPage("/Commande_Client/Index");
            }
         
            if (commandeId.HasValue)
            {
                var commande = await _context.CommandesClient
                    .Include(c => c.LignesCommande).ThenInclude(l => l.Article)
                    .FirstOrDefaultAsync(c => c.Id == commandeId.Value);

                if (commande == null) return NotFound();

                CommandeClientId = commande.Id;
                ClientId = commande.ClientId;
                NumeroCommande = commande.NumeroCommande;

                Lignes = commande.LignesCommande
                    .Where(l => l.QuantiteRestante > 0)
                    .Select(l => new LigneInput
                    {
                        ArticleId = l.ArticleId,
                        LigneCommandeClientId = l.Id,
                        Designation = l.Article!.Libelle,
                        QuantiteRestante = l.QuantiteRestante,
                        Stock = l.Article.Quantite,
                        Quantite = Math.Min(l.QuantiteRestante, l.Article.Quantite),
                        PrixUnitaire = l.PrixUnitaire
                    }).ToList();

                if (!Lignes.Any())
                {
                    TempData["ErrorMessage"] = "Cette commande est déjà entièrement livrée.";
                    return RedirectToPage("/Commande_Client/Index");
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // On ne garde que les lignes avec une quantité > 0
            Lignes = Lignes.Where(l => l.Quantite > 0).ToList();

            if (!Lignes.Any())
                ModelState.AddModelError(string.Empty, "Saisissez au moins une quantité à livrer.");

            if (ClientId == 0)
                ModelState.AddModelError(nameof(ClientId), "Le client est obligatoire.");

            if (!ModelState.IsValid)
            {
                await ChargerClientsAsync();
                return Page();
            }

            var bl = new BonLivraison
            {
                Numero = await _numerotation.ProchainNumeroBonLivraisonAsync(),
                Date = Date,
                ClientId = ClientId,
                CommandeClientId = CommandeClientId,
                Commentaire = Commentaire,
                Statut = StatutBonLivraison.Brouillon,
                Lignes = Lignes.Select(l => new LigneBonLivraison
                {
                    ArticleId = l.ArticleId,
                    LigneCommandeClientId = l.LigneCommandeClientId,
                    Quantite = l.Quantite,
                    PrixUnitaire = l.PrixUnitaire
                }).ToList()
            };

            _context.BonsLivraison.Add(bl);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Bon de livraison {bl.Numero} créé en brouillon.";
            return RedirectToPage("Details", new { id = bl.ID });
        }

        //private async Task ChargerClientsAsync()
        //{
        //    Clients = new SelectList(await _context.Clients.OrderBy(c => c.Nom).ToListAsync(), "ID", "Nom");
        //}

        private async Task ChargerClientsAsync()
        {
            var clients = await _context.Clients
                .Where(c => c.EstActif)
                .OrderBy(c => c.Nom)
                .ToListAsync();

            Clients = new SelectList(clients, nameof(Client.Id), nameof(Client.NomComplet));
        }
    }
}