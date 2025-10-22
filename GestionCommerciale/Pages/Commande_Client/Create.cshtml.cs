using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.ViewModel;
using GestionCommerciale.ViewModel.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestionCommerciale.Pages.Commande_Client
{
    //public class CreateModel : PageModel
    //{
    //    private readonly ApplicationDbContext _context;

    //    public CreateModel(ApplicationDbContext context)
    //    {
    //        _context = context;
    //    }

    //    [BindProperty]
    //    public CommandeClient CommandeClient { get; set; } = default!;

    //    [BindProperty]
    //    public List<LigneCommandeClientDto> LignesCommande { get; set; } = new List<LigneCommandeClientDto>();

    //    public SelectList ClientsSelectList { get; set; } = default!;
    //    public List<Article> Articles { get; set; } = default!;

    //    public async Task<IActionResult> OnGetAsync()
    //    {
    //        await LoadSelectLists();
    //        CommandeClient = new CommandeClient
    //        {
    //            NumeroCommande = await GenerateNumeroCommande(),
    //            DateCommande = DateTime.Now,
    //            Statut = StatutCommande.EnAttente
    //        };
    //        return Page();
    //    }

    //    public async Task<IActionResult> OnPostAsync()
    //    {
    //        if (!ModelState.IsValid)
    //        {
    //            await LoadSelectLists();
    //            return Page();
    //        }

    //        // Calculer les montants
    //        CommandeClient.MontantHT = LignesCommande.Sum(l => l.MontantLigne);
    //        CommandeClient.MontantTVA = CommandeClient.MontantHT * 0.20m; // TVA 20%
    //        CommandeClient.MontantTTC = CommandeClient.MontantHT + CommandeClient.MontantTVA;
    //        CommandeClient.DateCreation = DateTime.Now;

    //        _context.CommandesClient.Add(CommandeClient);
    //        await _context.SaveChangesAsync();

    //        // Ajouter les lignes de commande
    //        foreach (var ligneDto in LignesCommande)
    //        {
    //            var ligne = new LigneCommandeClient
    //            {
    //                CommandeClientId = CommandeClient.Id,
    //                ArticleId = ligneDto.ArticleId,
    //                Quantite = ligneDto.Quantite,
    //                PrixUnitaire = ligneDto.PrixUnitaire,
    //                MontantLigne = ligneDto.MontantLigne
    //            };
    //            _context.LignesCommandeClient.Add(ligne);
    //        }

    //        await _context.SaveChangesAsync();
    //        return RedirectToPage("./Index");
    //    }

    //    private async Task LoadSelectLists()
    //    {
    //        ClientsSelectList = new SelectList(await _context.Clients.ToListAsync(), "Id", "Nom");
    //        Articles = await _context.Articles.Where(a => a.EstActif).ToListAsync();
    //    }

    //    private async Task<string> GenerateNumeroCommande()
    //    {
    //        var lastCommande = await _context.CommandesClient
    //            .OrderByDescending(c => c.Id)
    //            .FirstOrDefaultAsync();

    //        int nextNumber = (lastCommande?.Id ?? 0) + 1;
    //        return $"CMD{DateTime.Now.Year}{nextNumber:D6}";
    //    }
    //}

    public class CreateModel : PageModel
    {
        // private readonly ApplicationDbContext _context;

        // public CreateModel(ApplicationDbContext context)
        // {
        //     _context = context;
        // }

        public CreateModel()
        {
        }

        [BindProperty]
        public CommandeClient CommandeClient { get; set; } = default!;

        [BindProperty]
        public List<LigneCommandeClientViewModel> LignesCommande { get; set; } = new();

        public SelectList Clients { get; set; } = default!;
        public List<ArticleViewModel> Articles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Génération du numéro de commande
            CommandeClient = new CommandeClient
            {
                NumeroCommande = GenerateNumeroCommande(),
                DateCommande = DateTime.Now,
                Statut = StatutCommande.EnAttente
            };

            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadData();
                return Page();
            }

            // Validation des lignes de commande
            if (LignesCommande == null || !LignesCommande.Any())
            {
                ModelState.AddModelError("", "Veuillez ajouter au moins un article à la commande.");
                await LoadData();
                return Page();
            }

            // Calcul des montants
            CalculerMontants();

            // Simulation de sauvegarde
            // _context.CommandesClient.Add(CommandeClient);
            // await _context.SaveChangesAsync();

            TempData["Success"] = "Commande créée avec succès !";
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnGetArticlesAsync()
        {
            var articles = GetArticles();
            return new JsonResult(articles);
        }

        private async Task LoadData()
        {
            // Simulation des données clients
            var clients = new List<Client>
            {
                new Client { Id = 1, Nom = "Dupont", Prenom = "Jean", Email = "jean.dupont@example.com" },
                new Client { Id = 2, Nom = "Martin", Prenom = "Marie", Email = "marie.martin@example.com" },
                new Client { Id = 3, Nom = "Durand", Prenom = "Pierre", Email = "pierre.durand@example.com" }
            };

            Clients = new SelectList(clients, "Id", "Nom");
            Articles = GetArticles();
        }

        private List<ArticleViewModel> GetArticles()
        {
            return new List<ArticleViewModel>
{
    new ArticleViewModel
    {
        Id = 1,
        Reference = "LAPTOP-DELL-XPS13",
        Barcode = "8717285912345",
        Name = "Ordinateur Portable Dell XPS 13",
        Category = "Informatique",
        Unit = "pcs",
        Brand = "Dell",
        Model = "XPS 13",
        Color = "Argent",
        Size = "13 pouces",
        Weight = 1.20m,
        VatRate = 20,
        Description = "Ordinateur portable professionnel haute performance...",
        StockMinimum = 5,
        StockMaximum = 50,
        CurrentStock = 45,
        ReservedStock = 2,
        OnOrderStock = 10,
        IsActive = true,
        CreatedAt = DateTime.Now.AddMonths(-6),
        UpdatedAt = DateTime.Now.AddDays(-2),
        LastStockUpdate = DateTime.Now.AddHours(-3)
    },
    new ArticleViewModel
    {
        Id = 2,
        Reference = "SMARTPHONE-SAMSUNG-S23",
        Barcode = "8717285912346",
        Name = "Smartphone Samsung Galaxy S23",
        Category = "Téléphonie",
        Unit = "pcs",
        Brand = "Samsung",
        Model = "Galaxy S23",
        Color = "Noir",
        Size = "6.1 pouces",
        Weight = 0.168m,
        VatRate = 20,
        Description = "Smartphone haut de gamme avec appareil photo performant...",
        StockMinimum = 10,
        StockMaximum = 100,
        CurrentStock = 80,
        ReservedStock = 5,
        OnOrderStock = 20,
        IsActive = true,
        CreatedAt = DateTime.Now.AddMonths(-3),
        UpdatedAt = DateTime.Now.AddDays(-1),
        LastStockUpdate = DateTime.Now.AddHours(-6)
    },
    new ArticleViewModel
    {
        Id = 3,
        Reference = "TABLET-APPLE-IPAD-AIR",
        Barcode = "8717285912347",
        Name = "Tablette Apple iPad Air",
        Category = "Tablettes",
        Unit = "pcs",
        Brand = "Apple",
        Model = "iPad Air",
        Color = "Gris sidéral",
        Size = "10.9 pouces",
        Weight = 0.458m,
        VatRate = 20,
        Description = "Tablette légère et puissante pour le travail et le divertissement...",
        StockMinimum = 8,
        StockMaximum = 60,
        CurrentStock = 55,
        ReservedStock = 3,
        OnOrderStock = 15,
        IsActive = true,
        CreatedAt = DateTime.Now.AddMonths(-4),
        UpdatedAt = DateTime.Now.AddDays(-3),
        LastStockUpdate = DateTime.Now.AddHours(-12)
    }
};
            ;
        }

        private void CalculerMontants()
        {
            CommandeClient.MontantHT = LignesCommande.Sum(l => l.MontantLigne);
            CommandeClient.MontantTVA = CommandeClient.MontantHT * 0.20m; // TVA 20%
            CommandeClient.MontantTTC = CommandeClient.MontantHT + CommandeClient.MontantTVA;
        }

        private string GenerateNumeroCommande()
        {
            return $"CMD{DateTime.Now:yyyyMMdd}{DateTime.Now.Millisecond:D3}";
        }
    }
}