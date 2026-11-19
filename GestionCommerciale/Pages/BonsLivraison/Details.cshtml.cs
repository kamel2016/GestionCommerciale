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

        /// <summary>Affiché quand le stock d'un article a été modifié par une autre opération entre la lecture et l'enregistrement.</summary>
        public const string MessageConflitStock = "Le stock d'un article a été modifié par une autre opération. Rien n'a été enregistré : vérifiez le bon de livraison puis recommencez.";

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
            try
            {
                var (succes, erreur) = await _service.ValiderAsync(id);
                if (succes) TempData["SuccessMessage"] = "Bon de livraison validé, stock mis à jour.";
                else TempData["ErrorMessage"] = erreur;
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = MessageConflitStock;
            }
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAnnulerAsync(int id)
        {
            try
            {
                var (succes, erreur) = await _service.AnnulerAsync(id);
                if (succes) TempData["SuccessMessage"] = "Bon de livraison annulé.";
                else TempData["ErrorMessage"] = erreur;
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = MessageConflitStock;
            }
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