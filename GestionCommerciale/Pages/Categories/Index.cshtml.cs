using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.Models;

namespace GestionCommerciale.Pages.Categories
{
    public class IndexModel : PageModel
    {
        public List<Categorie> Categories { get; set; } = new List<Categorie>();

        [BindProperty(SupportsGet = true)]
        public string? Filtre { get; set; }

        public void OnGet()
        {
            var toutesLesCategories = new List<Categorie>
            {
                new Categorie { ID = 1, Libelle="Electroniques" },
                new Categorie { ID = 2, Libelle="Vidéos" },
                new Categorie { ID = 3, Libelle="Télésurveillance" },
                new Categorie { ID = 4, Libelle="Caméras embarquée" },
                new Categorie { ID = 5, Libelle="Luminaires" }
            }; 

            // Filtrage si un filtre est appliqué
            if (!string.IsNullOrEmpty(Filtre))
            {
                Categories = toutesLesCategories.Where(a =>
                    a.Libelle.Contains(Filtre, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                Categories = toutesLesCategories;
            }
        }
    }
}