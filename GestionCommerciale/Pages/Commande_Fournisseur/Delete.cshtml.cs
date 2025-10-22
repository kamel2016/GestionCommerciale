using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Commande_Client
{
    public class DeleteModel : PageModel
    {
        //private readonly ApplicationDbContext _context;

        //public DeleteModel(ApplicationDbContext context)
        //{
        //    _context = context;
        //}


        [BindProperty]
        public CommandeClient CommandeClient { get; set; }

        public IActionResult OnGet(int id)
        {
            CommandeClient = new CommandeClient
            {
                Id = 1,
                NumeroCommande = "CMD20250101001",
                DateCommande = DateTime.Now.AddDays(-10),
                DateLivraison = DateTime.Now.AddDays(-5),
                ClientId = 1,
                Statut = StatutCommande.Livree,
                MontantHT = 1200.00m,
                MontantTVA = 240.00m,
                MontantTTC = 1440.00m,
                DateCreation = DateTime.Now.AddDays(-10),
                Client = new Client { Id = 1, Nom = "Dupont", Prenom = "Jean", Email = "jean.dupont@example.com" }
            };

            //Client = await _context.Clients
            //    .Include(c => c.CommandesClient)
            //    .FirstOrDefaultAsync(m => m.Id == id);

            if (CommandeClient == null)
            {
                return NotFound();
            }

            return Page();
        }

        public IActionResult OnPost(int id)
        {
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