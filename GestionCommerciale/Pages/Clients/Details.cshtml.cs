using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Clients
{
    public class DetailsModel : PageModel
    {
        //private readonly ApplicationDbContext _context;

        //public EditModel(ApplicationDbContext context)
        //{
        //    _context = context;
        //}

        public Client? Client { get; set; }

        public IActionResult OnGet(int id)
        {
            //if (id == null)
            //{
            //    return NotFound();
            //}

            //Client = await _context.Clients.FirstOrDefaultAsync(m => m.Id == id);

            Client = new Client
            {
                Id = 6,
                Nom = "Petit",
                Prenom = "Antoine",
                Email = "antoine.petit@example.com",
                Telephone = "0644556677",
                Adresse = "17 Rue Sainte-Catherine, 33000 Bordeaux",
                DateCreation = DateTime.Now.AddDays(-5)
            };

            if (Client == null)
            {
                return NotFound();
            }

            return Page();
        }

     
        private bool ClientExists(int id)
        {
            return false;// _context.Clients.Any(e => e.Id == id);
        }
    }
}