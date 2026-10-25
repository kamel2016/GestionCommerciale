using GestionCommerciale.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;

namespace GestionCommerciale.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalSuppliers { get; set; }
        public int OrdersInProgress { get; set; }
        public int GoodsReceiptsToday { get; set; }

        public async Task OnGetAsync()
        {
            TotalSuppliers = await _context.Fournisseurs.CountAsync();
            // Adapte les noms de DbSet et de statut à ton modèle
            OrdersInProgress = await _context.CommandesFournisseur
                .CountAsync(c => c.Statut == Enums.StatutCommande.EnAttente);
            GoodsReceiptsToday = await _context.BonsReception
                .CountAsync(r => r.DateReception.Date == DateTime.Today);
        }
    }
}