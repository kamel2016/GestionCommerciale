using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Fournisseurs
{
    public class DetailsModel : PageModel
    {
        //private readonly ApplicationDbContext _context;

        //public EditModel(ApplicationDbContext context)
        //{
        //    _context = context;
        //}

        public DetailsModel()
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Vérifier si l'email existe déjà (sauf pour le client actuel)
            //if (await _context.Clients.AnyAsync(c => c.Email == Client.Email && c.Id != Client.Id))
            //{
            //    ModelState.AddModelError("Client.Email", "Un client avec cet email existe déjà.");
            //    return Page();
            //}

            Client.DateModification = DateTime.Now;
            //_context.Attach(Client).State = EntityState.Modified;

            try
            {
                //await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientExists(Client.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ClientExists(int id)
        {
            return false;// _context.Clients.Any(e => e.Id == id);
        }
    }
}