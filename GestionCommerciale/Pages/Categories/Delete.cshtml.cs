using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.Models;

namespace GestionCommerciale.Pages.Categories
{
    public class DeleteModel : PageModel
    {
        [BindProperty]
        public Categorie Categorie { get; set; } = new Categorie();

        public IActionResult OnGet(int id)
        {
            // Données d'exemple - en production, vous utiliseriez une base de données
            var articles = GetCategories();
            Categorie = articles.FirstOrDefault(a => a.ID == id);

            if (Categorie == null)
            {
                return NotFound();
            }

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            // Ici vous feriez la suppression en base de données
            // Pour l'exemple, on simule juste une suppression réussie

            // Redirection vers la liste après suppression
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