using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.Models;

namespace GestionCommerciale.Pages.Categories
{
    public class CreateModel : PageModel
    {
        [BindProperty]
        public Categorie Categorie { get; set; } = new Categorie();

        public IActionResult OnGet()
        {
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Ici vous feriez l'insertion en base de données
            // Pour l'exemple, on simule juste une création réussie

            // Redirection vers la liste après création
            return RedirectToPage("./Index");
        }
    }
}