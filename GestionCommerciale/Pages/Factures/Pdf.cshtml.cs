using GestionCommerciale.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestionCommerciale.Pages.Factures
{
    public class PdfModel : PageModel
    {
        private readonly IFacturePdfService _pdf;

        public PdfModel(IFacturePdfService pdf)
        {
            _pdf = pdf;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var bytes = await _pdf.GenererAsync(id);
                // Affichage dans le navigateur (pas de nom de fichier = inline)
                return File(bytes, "application/pdf");
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }
    }
}