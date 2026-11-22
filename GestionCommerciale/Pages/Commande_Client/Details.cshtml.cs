using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Commande_Client
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly GestionCommerciale.Services.BonLivraisonService _bonLivraisonService;

        public DetailsModel(ApplicationDbContext context, GestionCommerciale.Services.BonLivraisonService bonLivraisonService)
        {
            _context = context;
            _bonLivraisonService = bonLivraisonService;
        }

        public Models.CommandeClient CommandeClient { get; set; } = default!;

        /// <summary>Vrai si un bon de livraison peut être créé pour cette commande : MÊME règle que le serveur (statut autorisé et reste à livrer par cumul).</summary>
        public bool PeutCreerBonLivraison { get; private set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.CommandesClient == null)
            {
                return NotFound();
            }

            var commandeclient = await _context.CommandesClient
                .Include(c => c.Client)
                .Include(c => c.LignesCommande)
                .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (commandeclient == null)
            {
                return NotFound();
            }
            else
            {
                CommandeClient = commandeclient;
                PeutCreerBonLivraison = await _bonLivraisonService.PeutCreerBonAsync(commandeclient.Id);
            }
            return Page();
        }
    }
}