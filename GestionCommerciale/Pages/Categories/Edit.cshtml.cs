using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.Models;

namespace GestionCommerciale.Pages.Categories
{
    public class EditModel : PageModel
    {
        [BindProperty]
        public Categorie Categorie { get; set; } = new Categorie();

        public IActionResult OnGet(int id)
        {
            // Données d'exemple - en production, vous utiliseriez une base de données
            var categories = GetCategories();
            Categorie = categories.FirstOrDefault(a => a.ID == id);

            if (Categorie == null)
            {
                return NotFound();
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Ici vous feriez la mise à jour en base de données
            // Pour l'exemple, on simule juste une mise à jour réussie

            // Redirection vers la liste après modification
            return RedirectToPage("./Index");
        }

        private List<Categorie> GetCategories()
        {
            return new List<Categorie>
            {
                new Categorie { ID = 1, Libelle="Electroniques" },
                new Categorie { ID = 2, Libelle="Vidéos" },
                new Categorie { ID = 3, Libelle="Télésurveillance" },
                new Categorie { ID = 4, Libelle="Caméras embarquée" },
                new Categorie { ID = 5, Libelle="Luminaires" }
            };
        }
    }
}