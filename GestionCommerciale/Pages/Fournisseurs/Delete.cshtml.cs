using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Fournisseurs
{
    public class DeleteModel : PageModel
    {
        //private readonly ApplicationDbContext _context;

        //public DeleteModel(ApplicationDbContext context)
        //{
        //    _context = context;
        //}

        public DeleteModel()
        {
            
        }

        [BindProperty]
        public Client Client { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }


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

            //Client = await _context.Clients
            //    .Include(c => c.CommandesClient)
            //    .FirstOrDefaultAsync(m => m.Id == id);

            if (Client == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Client = await _context.Clients
            //    .Include(c => c.CommandesClient)
            //    .FirstOrDefaultAsync(m => m.Id == id);

            //if (Client != null)
            //{
            //    if (Client.CommandesClient.Any())
            //    {
            //        ModelState.AddModelError("", "Impossible de supprimer un client qui a des commandes.");
            //        return Page();
            //    }

            //    //_context.Clients.Remove(Client);
            //    //await _context.SaveChangesAsync();
            //}

            return RedirectToPage("./Index");
        }
    }
}