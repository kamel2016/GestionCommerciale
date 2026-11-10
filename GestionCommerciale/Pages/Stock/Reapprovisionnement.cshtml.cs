using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace TonProjet.Pages.Stock; // adapte au namespace de ton projet

public class ReapprovisionnementModel : PageModel
{
    private readonly ApplicationDbContext _context; // remplace par le nom de ton DbContext

    public ReapprovisionnementModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Article> ArticlesAReapprovisionner { get; set; } = new();

    public async Task OnGetAsync()
    {
        ArticlesAReapprovisionner = await _context.Articles
            .Where(a => a.Quantite <= a.StockMin)
            .OrderBy(a => a.Quantite)
            .ToListAsync();
    }
}