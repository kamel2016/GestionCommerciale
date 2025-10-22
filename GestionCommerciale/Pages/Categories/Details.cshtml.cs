using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.Models;

namespace GestionCommerciale.Pages.Categories
{
    public class DetailsModel : PageModel
    {
        public Categorie? Categorie { get; set; }

        public IActionResult OnGet(int id)
        {
            // Données d'exemple - en production, vous utiliseriez une base de données
            var categories = new List<Categorie>
            {
                new Categorie { ID = 1, Libelle="Electroniques" },
                new Categorie { ID = 2, Libelle="Vidéos" },
                new Categorie { ID = 3, Libelle="Télésurveillance" },
                new Categorie { ID = 4, Libelle="Caméras embarquée" },
                new Categorie { ID = 5, Libelle="Luminaires" }            };

            Categorie = categories.FirstOrDefault(a => a.ID == id);

            if (Categorie == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}