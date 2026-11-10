using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Models.Enums;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Factures
{
    public class ReglementsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IReglementService _service;

        public ReglementsModel(ApplicationDbContext db, IReglementService service)
        {
            _db = db;
            _service = service;
        }

        public Facture Facture { get; set; } = null!;
        public List<Reglement> Reglements { get; set; } = new();

        [BindProperty]
        public Reglement Nouveau { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!await ChargerAsync(id)) return NotFound();

            Nouveau.FactureId = id;
            Nouveau.DateReglement = DateTime.Today;
            Nouveau.Montant = Facture.ResteAPayer;   // pré-rempli avec le reste
            return Page();
        }

        public async Task<IActionResult> OnPostAjouterAsync(int id)
        {
            Nouveau.FactureId = id;
            ModelState.Remove("Nouveau.Facture");

            if (!await ChargerAsync(id)) return NotFound();

            if (!ModelState.IsValid) return Page();

            try
            {
                await _service.AjouterAsync(Nouveau);
                TempData["Success"] = "Règlement enregistré.";
                return RedirectToPage(new { id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSupprimerAsync(int id, int reglementId)
        {
            await _service.SupprimerAsync(reglementId);
            TempData["Success"] = "Règlement supprimé.";
            return RedirectToPage(new { id });
        }

        private async Task<bool> ChargerAsync(int id)
        {
            var f = await _db.Factures
                .Include(x => x.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (f == null) return false;

            Facture = f;
            Reglements = await _service.GetByFactureAsync(id);
            return true;
        }
    }
}