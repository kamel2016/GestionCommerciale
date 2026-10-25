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

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Models.CommandeClient CommandeClient { get; set; } = default!;

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
            }
            return Page();
        }
    }
}