// Pages/BonsLivraison/Index.cshtml.cs
using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.BonsLivraison
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<BonLivraison> BonsLivraison { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? Recherche { get; set; }
        [BindProperty(SupportsGet = true)] public StatutBonLivraison? Statut { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.BonsLivraison
                .Include(b => b.Client)
                .Include(b => b.Lignes)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Recherche))
                query = query.Where(b => b.Numero.Contains(Recherche) ||
                                         b.Client!.Nom.Contains(Recherche));

            if (Statut.HasValue)
                query = query.Where(b => b.Statut == Statut.Value);

            BonsLivraison = await query.OrderByDescending(b => b.Date).ToListAsync();
        }
    }
}