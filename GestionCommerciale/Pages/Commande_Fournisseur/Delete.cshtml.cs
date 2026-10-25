using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Commande_Fournisseur
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CommandeFournisseur CommandeFournisseur { get; set; } = default!;

        public int NombreBonsReception { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var commande = await _context.CommandesFournisseur
                .Include(c => c.Fournisseur)
                .Include(c => c.LignesCommande)
                    .ThenInclude(l => l.Article)
                .Include(c => c.BonsReception)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande == null)
                return NotFound();

            CommandeFournisseur = commande;
            NombreBonsReception = commande.BonsReception.Count;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var commande = await _context.CommandesFournisseur
                .Include(c => c.LignesCommande)
                .Include(c => c.BonsReception)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande == null)
                return NotFound();

            // Une commande ayant déjà des bons de réception ne peut pas être supprimée
            if (commande.BonsReception.Any())
            {
                TempData["Error"] = "Impossible de supprimer cette commande : elle possède des bons de réception.";
                return RedirectToPage("./Index");
            }

            _context.LignesCommandeFournisseur.RemoveRange(commande.LignesCommande);
            _context.CommandesFournisseur.Remove(commande);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"La commande {commande.NumeroCommande} a été supprimée.";
            return RedirectToPage("./Index");
        }
    }
}