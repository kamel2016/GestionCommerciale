using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Commande_Client
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CommandeClient CommandeClient { get; set; }
        public List<Article> Articles { get; set; }

        public List<LigneCommandeClient> LignesCommande { get; set; }

        // Liste déroulante des clients
        public List<SelectListItem> ClientsSelectList { get; set; }

        public IActionResult OnGet(int id)
        {
            IQueryable<Client> clients = new List<Client>
            {
                new Client
                {
                    Id = 1,
                    Nom = "Dupont",
                    Prenom = "Alice",
                    Email = "alice.dupont@example.com",
                    Telephone = "0601020304",
                    Adresse = "123 Rue de Paris, 75001 Paris",
                    DateCreation = DateTime.Now.AddDays(-30)
                },
                new Client
                {
                    Id = 2,
                    Nom = "Martin",
                    Prenom = "Jean",
                    Email = "jean.martin@example.com",
                    Telephone = "0605060708",
                    Adresse = "45 Avenue des Champs-Élysées, 75008 Paris",
                    DateCreation = DateTime.Now.AddDays(-25)
                },
                new Client
                {
                    Id = 3,
                    Nom = "Leroy",
                    Prenom = "Claire",
                    Email = "claire.leroy@example.com",
                    Telephone = "0611223344",
                    Adresse = "88 Boulevard Haussmann, 75009 Paris",
                    DateCreation = DateTime.Now.AddDays(-20)
                },
                new Client
                {
                    Id = 4,
                    Nom = "Nguyen",
                    Prenom = "Marc",
                    Email = "marc.nguyen@example.com",
                    Telephone = "0622334455",
                    Adresse = "12 Rue du Faubourg Saint-Honoré, 75008 Paris",
                    DateCreation = DateTime.Now.AddDays(-15)
                },
                new Client
                {
                    Id = 5,
                    Nom = "Bernard",
                    Prenom = "Lucie",
                    Email = "lucie.bernard@example.com",
                    Telephone = "0633445566",
                    Adresse = "5 Place Bellecour, 69002 Lyon",
                    DateCreation = DateTime.Now.AddDays(-10)
                },
                new Client
                {
                    Id = 6,
                    Nom = "Petit",
                    Prenom = "Antoine",
                    Email = "antoine.petit@example.com",
                    Telephone = "0644556677",
                    Adresse = "17 Rue Sainte-Catherine, 33000 Bordeaux",
                    DateCreation = DateTime.Now.AddDays(-5)
                }
            }.AsQueryable();

            ClientsSelectList = clients.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.NomComplet // ou c.Nom si tu n’as pas un champ NomComplet
            }).ToList();

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

            LignesCommande = new List<LigneCommandeClient>();

            if (CommandeClient == null)
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

            // Vérifier si l'email existe déjà (sauf pour le client actuel)
            //if (await _context.Clients.AnyAsync(c => c.Email == Client.Email && c.Id != Client.Id))
            //{
            //    ModelState.AddModelError("Client.Email", "Un client avec cet email existe déjà.");
            //    return Page();
            //}

            CommandeClient.DateModification = DateTime.Now;
            //_context.Attach(Client).State = EntityState.Modified;

            try
            {
                //await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommandeClientExists(CommandeClient.Id))
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

        private bool CommandeClientExists(int id)
        {
            return false;// _context.Clients.Any(e => e.Id == id);
        }
    }
}