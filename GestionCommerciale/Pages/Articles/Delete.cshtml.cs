using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.Models;

namespace GestionCommerciale.Pages.Articles
{
    public class DeleteModel : PageModel
    {
        [BindProperty]
        public Article Article { get; set; } = new Article();

        public IActionResult OnGet(int id)
        {
            // Données d'exemple - en production, vous utiliseriez une base de données
            var articles = GetArticles();
            Article = articles.FirstOrDefault(a => a.ID == id);

            if (Article == null)
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

        private List<Article> GetArticles()
        {
            return new List<Article>
            {
                new Article { ID = 1, Reference = "ART001", Libelle = "Ordinateur portable", CodeBarre = "123456789", Quantite = 10, PrixVenteHT = 800.00m, PrixVenteTTC = 960.00m },
                new Article { ID = 2, Reference = "ART002", Libelle = "Souris sans fil", CodeBarre = "987654321", Quantite = 25, PrixVenteHT = 25.00m, PrixVenteTTC = 30.00m },
                new Article { ID = 3, Reference = "ART003", Libelle = "Clavier mécanique", CodeBarre = "456789123", Quantite = 15, PrixVenteHT = 120.00m, PrixVenteTTC = 144.00m },
                new Article { ID = 4, Reference = "ART004", Libelle = "Écran 24 pouces", CodeBarre = "789123456", Quantite = 8, PrixVenteHT = 200.00m, PrixVenteTTC = 240.00m },
                new Article { ID = 5, Reference = "ART005", Libelle = "Imprimante laser", CodeBarre = "321654987", Quantite = 5, PrixVenteHT = 300.00m, PrixVenteTTC = 360.00m }
            };
        }
    }
}