using GestionCommerciale.Data;
using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Factures
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IFactureService _factureService;

        public CreateModel(ApplicationDbContext db, IFactureService factureService)
        {
            _db = db;
            _factureService = factureService;
        }

        // Pré-sélection depuis la page du BL : /Factures/Create?blId=5
        [BindProperty(SupportsGet = true)]
        public int? BlId { get; set; }

        public List<SelectListItem> BonsLivraison { get; set; } = new();

        public string? Erreur { get; set; }

        public async Task OnGetAsync()
        {
            await ChargerBonsLivraison();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (BlId == null)
            {
                Erreur = "Veuillez choisir un bon de livraison.";
                await ChargerBonsLivraison();
                return Page();
            }

            try
            {
                var facture = await _factureService.CreerDepuisBonLivraisonAsync(BlId.Value);
                return RedirectToPage("Details", new { id = facture.Id });
            }
            catch (Exception ex)
            {
                Erreur = ex.Message;
                await ChargerBonsLivraison();
                return Page();
            }
        }

        private async Task ChargerBonsLivraison()
        {
            // BL pas encore facturés (aucune facture non annulée liée)
            var dejaFactures = _db.Factures
                .Where(f => f.BonLivraisonId != null && f.Statut != Enums.StatutFacture.Annulee)
                .Select(f => f.BonLivraisonId!.Value);

            var bls = await _db.BonsLivraison
                .Include(b => b.Client)
                .Where(b => !dejaFactures.Contains(b.ID))
                .OrderByDescending(b => b.ID)
                .AsNoTracking()
                .ToListAsync();

            BonsLivraison = bls.Select(b => new SelectListItem
            {
                Value = b.ID.ToString(),
                // Adapte b.Client.Nom et b.Numero à tes propriétés réelles
                Text = $"BL n° {b.ID} - {b.Client?.Nom}"
            }).ToList();
        }
    }
}