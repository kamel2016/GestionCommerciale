using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Factures
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<Facture> Factures { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? Recherche { get; set; }
        [BindProperty(SupportsGet = true)] public StatutFacture? Statut { get; set; }

        [TempData] public string? Message { get; set; }
        [TempData] public string? Erreur { get; set; }

        public async Task OnGetAsync()
        {
            var q = _db.Factures.Include(f => f.Client).AsQueryable();

            if (!string.IsNullOrWhiteSpace(Recherche))
            {
                var r = Recherche.Trim();
                q = q.Where(f => (f.Numero != null && f.Numero.Contains(r))
                              || (f.Client != null && f.Client.Nom.Contains(r)));
            }
            if (Statut.HasValue)
                q = q.Where(f => f.Statut == Statut.Value);

            Factures = await q.OrderByDescending(f => f.Date)
                              .ThenByDescending(f => f.Id)
                              .ToListAsync();
        }
    }
}