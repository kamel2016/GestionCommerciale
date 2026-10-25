using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Commande_Fournisseur
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<CommandeFournisseur> CommandesFournisseur { get; set; } = new List<CommandeFournisseur>();

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public StatutCommande? StatutFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.CommandesFournisseur
                .Include(c => c.Fournisseur)
                .AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(c =>
                    c.NumeroCommande.Contains(SearchString) ||
                    c.Fournisseur.Nom.Contains(SearchString));
            }

            if (StatutFilter.HasValue)
            {
                query = query.Where(c => c.Statut == StatutFilter.Value);
            }

            CommandesFournisseur = await query
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }
    }
}