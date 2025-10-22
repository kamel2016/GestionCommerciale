using GestionCommerciale.Helpers;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace GestionCommerciale.Pages.Fournisseurs
{
    public class IndexModel : PageModel
    {
        //private readonly ApplicationDbContext _context;

        //public IndexModel(ApplicationDbContext context)
        //{
        //    _context = context;
        //}

        public IndexModel()
        {
        }

        public PaginatedList<Client> Clients { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? CurrentFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PageIndex { get; set; }

        public async Task OnGetAsync()
        {
            if (SearchString != null)
            {
                PageIndex = 1;
            }
            else
            {
                SearchString = CurrentFilter;
            }

            CurrentFilter = SearchString;

            //IQueryable<Client> clientsIQ = _context.Clients.Include(c => c.CommandesClient);

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

            var fournisseurs = new List<Fournisseur>
{
    new Fournisseur
    {
        Id = 1,
        Nom = "TechDistrib SARL",
        Email = "contact@techdistrib.com",
        Telephone = "0123456789",
        Adresse = "12 Rue des Industries, 75010 Paris",
        PersonneContact = "Sophie Morel",
        DateCreation = DateTime.Now
    },
    new Fournisseur
    {
        Id = 2,
        Nom = "Solutions BTP",
        Email = "info@solutionsbtp.fr",
        Telephone = "0987654321",
        Adresse = "8 Avenue des Travaux, 69007 Lyon",
        PersonneContact = "Jean-Luc Martin",
        DateCreation = DateTime.Now.AddDays(-10)
    },
    new Fournisseur
    {
        Id = 3,
        Nom = "Fournipro Services",
        Email = "service@fournipro.com",
        Telephone = "0678452310",
        Adresse = "5 Boulevard Fournisseurs, 31000 Toulouse",
        PersonneContact = "Claire Dubois",
        DateCreation = DateTime.Now.AddMonths(-2)
    },
    new Fournisseur
    {
        Id = 4,
        Nom = "Matériel Express",
        Email = "contact@matexpress.fr",
        Telephone = "0147852369",
        Adresse = "17 Rue du Commerce, 59800 Lille",
        PersonneContact = "Karim El Amrani",
        DateCreation = DateTime.Now.AddMonths(-1).AddDays(-3)
    },
    new Fournisseur
    {
        Id = 5,
        Nom = "InnovaTech Distribution",
        Email = "support@innovatech.fr",
        Telephone = "0612345678",
        Adresse = "20 Chemin des Entrepreneurs, 13008 Marseille",
        PersonneContact = "Nathalie Bernard",
        DateCreation = DateTime.Now.AddDays(-30)
    }
};


            if (!string.IsNullOrEmpty(SearchString))
            {
                clients = clients.Where(c => c.Nom.Contains(SearchString) ||
                                               c.Prenom.Contains(SearchString) ||
                                               c.Email.Contains(SearchString) ||
                                               c.Telephone.Contains(SearchString));
            }

            clients = clients.OrderBy(c => c.Nom).ThenBy(c => c.Prenom);

            int pageSize = 10;
            Clients = await PaginatedList<Client>.CreateAsync(clients, PageIndex ?? 1, pageSize);
        }
    }
}