using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Factures
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IFactureService _factureService;

        public DetailsModel(ApplicationDbContext db, IFactureService factureService)
        {
            _db = db;
            _factureService = factureService;
        }

        public Facture Facture { get; set; } = null!;

        [TempData] public string? Message { get; set; }
        [TempData] public string? Erreur { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var f = await Charger(id);
            if (f == null) return NotFound();
            Facture = f;
            return Page();
        }

        public async Task<IActionResult> OnPostValiderAsync(int id)
        {
            var (ok, erreur) = await _factureService.ValiderAsync(id);
            if (ok) Message = "Facture validée.";
            else Erreur = erreur;
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAnnulerAsync(int id)
        {
            var (ok, erreur) = await _factureService.AnnulerAsync(id);
            if (ok) Message = "Facture annulée.";
            else Erreur = erreur;
            return RedirectToPage(new { id });
        }

        private Task<Facture?> Charger(int id)
        {
            return _db.Factures
                .Include(f => f.Client)
                .Include(f => f.Lignes).ThenInclude(l => l.Article)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);
        }
    }
}