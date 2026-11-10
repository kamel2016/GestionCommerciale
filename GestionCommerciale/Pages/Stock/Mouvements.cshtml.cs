using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Stock
{
    public class MouvementsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MouvementsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Article? Article { get; set; }
        public List<MouvementStock> Mouvements { get; set; } = new();
            
        [BindProperty(SupportsGet = true)] public int ArticleId { get; set; }
        [BindProperty(SupportsGet = true)] public TypeMouvementStock? Type { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Du { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Au { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Article = await _context.Articles.FindAsync(ArticleId);
            if (Article == null) return NotFound();

            var query = _context.MouvementsStock
                .AsNoTracking()
                .Where(m => m.ArticleId == ArticleId);

            if (Type.HasValue) query = query.Where(m => m.Type == Type.Value);
            if (Du.HasValue) query = query.Where(m => m.Date >= Du.Value);
            if (Au.HasValue) query = query.Where(m => m.Date < Au.Value.AddDays(1));

            Mouvements = await query.OrderByDescending(m => m.Date).ToListAsync();
            return Page();
        }
    }
}