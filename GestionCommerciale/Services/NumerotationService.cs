// Services/NumerotationService.cs
using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Data;
using GestionCommerciale.Services.Interface;

namespace GestionCommerciale.Services
{
    public class NumerotationService : INumerotationService
    {
        private readonly ApplicationDbContext _context;

        public NumerotationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> ProchainNumeroBonLivraisonAsync()
        {
            var prefixe = $"BL-{DateTime.Now.Year}-";

            var dernier = await _context.BonsLivraison
                .Where(b => b.Numero.StartsWith(prefixe))
                .OrderByDescending(b => b.Numero)
                .Select(b => b.Numero)
                .FirstOrDefaultAsync();

            var n = 1;
            if (dernier != null && int.TryParse(dernier.Substring(prefixe.Length), out var val))
                n = val + 1;

            return $"{prefixe}{n:D5}";
        }
    }
}