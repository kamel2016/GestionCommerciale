using GestionCommerciale.Enums;
using GestionCommerciale.Helpers;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace GestionCommerciale.Pages.Commande_Client
{
    //public class IndexModel : PageModel
    //{
    //    private readonly ApplicationDbContext _context;

    //    public IndexModel(ApplicationDbContext context)
    //    {
    //        _context = context;
    //    }

    //    public IList<CommandeClient> CommandesClient { get; set; } = default!;

    //    public async Task OnGetAsync()
    //    {
    //        if (_context.CommandesClient != null)
    //        {
    //            CommandesClient = await _context.CommandesClient
    //                .Include(c => c.Client)
    //                .OrderByDescending(c => c.DateCommande)
    //                .ToListAsync();
    //        }
    //    }
    //}

    public class IndexModel : PageModel
    {
        // private readonly ApplicationDbContext _context;

        // public IndexModel(ApplicationDbContext context)
        // {
        //     _context = context;
        // }

        public IndexModel()
        {
        }

        public IList<CommandeClient> CommandeClients { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public StatutCommande? StatutFilter { get; set; }

        public async Task OnGetAsync()
        {
            // Simulation des données
            var commandesClient = new List<CommandeClient>
            {
                new CommandeClient
                {
                    Id = 1,
                    NumeroCommande = "CMD20250101001",
                    DateCommande = DateTime.Now.AddDays(-10),
                    DateLivraison = DateTime.Now.AddDays(-5),
                    ClientId = 1,
                    Statut = StatutCommande.Livree,
                    MontantHT = 1200.00m,
                    MontantTVA = 240.00m,
                    MontantTTC = 1440.00m,
                    DateCreation = DateTime.Now.AddDays(-10),
                    Client = new Client { Id = 1, Nom = "Dupont", Prenom = "Jean", Email = "jean.dupont@example.com" }
                },
                new CommandeClient
                {
                    Id = 2,
                    NumeroCommande = "CMD20250105002",
                    DateCommande = DateTime.Now.AddDays(-5),
                    DateLivraison = DateTime.Now.AddDays(2),
                    ClientId = 2,
                    Statut = StatutCommande.Expediee,
                    MontantHT = 850.00m,
                    MontantTVA = 170.00m,
                    MontantTTC = 1020.00m,
                    DateCreation = DateTime.Now.AddDays(-5),
                    Client = new Client { Id = 2, Nom = "Martin", Prenom = "Marie", Email = "marie.martin@example.com" }
                },
                new CommandeClient
                {
                    Id = 3,
                    NumeroCommande = "CMD20250110003",
                    DateCommande = DateTime.Now.AddDays(-2),
                    DateLivraison = DateTime.Now.AddDays(5),
                    ClientId = 3,
                    Statut = StatutCommande.EnAttente,
                    MontantHT = 450.00m,
                    MontantTVA = 90.00m,
                    MontantTTC = 540.00m,
                    DateCreation = DateTime.Now.AddDays(-2),
                    Client = new Client { Id = 3, Nom = "Durand", Prenom = "Pierre", Email = "pierre.durand@example.com" }
                },
                new CommandeClient
                {
                    Id = 4,
                    NumeroCommande = "CMD20250112004",
                    DateCommande = DateTime.Now.AddDays(-1),
                    DateLivraison = null,
                    ClientId = 1,
                    Statut = StatutCommande.Annulee,
                    MontantHT = 299.99m,
                    MontantTVA = 60.00m,
                    MontantTTC = 359.99m,
                    DateCreation = DateTime.Now.AddDays(-1),
                    Client = new Client { Id = 1, Nom = "Dupont", Prenom = "Jean", Email = "jean.dupont@example.com" }
                }
            };

            // Filtrage par recherche
            if (!string.IsNullOrEmpty(SearchString))
            {
                commandesClient = commandesClient.Where(c =>
                    c.NumeroCommande.Contains(SearchString, StringComparison.OrdinalIgnoreCase) ||
                    c.Client.Nom.Contains(SearchString, StringComparison.OrdinalIgnoreCase) ||
                    c.Client.Prenom.Contains(SearchString, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Filtrage par statut
            if (StatutFilter.HasValue)
            {
                commandesClient = commandesClient.Where(c => c.Statut == StatutFilter.Value).ToList();
            }

            CommandeClients = commandesClient.OrderByDescending(c => c.DateCreation).ToList();
        }
    }
}