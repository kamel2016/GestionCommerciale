// Pages/BonsLivraison/Details.cshtml.cs
using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.BonsLivraison
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly BonLivraisonService _service;

        public DetailsModel(ApplicationDbContext context, BonLivraisonService service)
        {
            _context = context;
            _service = service;
        }

        public BonLivraison BonLivraison { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var bl = await Charger(id);
            if (bl == null) return NotFound();
            BonLivraison = bl;
            return Page();
        }

        public async Task<IActionResult> OnPostValiderAsync(int id)
        {
            var (succes, erreur) = await _service.ValiderAsync(id);
            if (succes) TempData["SuccessMessage"] = "Bon de livraison validé, stock mis à jour.";
            else TempData["ErrorMessage"] = erreur;
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAnnulerAsync(int id)
        {
            var (succes, erreur) = await _service.AnnulerAsync(id);
            if (succes) TempData["SuccessMessage"] = "Bon de livraison annulé.";
            else TempData["ErrorMessage"] = erreur;
            return RedirectToPage(new { id });
        }

        private Task<BonLivraison?> Charger(int id) =>
            _context.BonsLivraison
                .Include(b => b.Client)
                .Include(b => b.CommandeClient)
                .Include(b => b.Lignes).ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(b => b.ID == id);
    }
}