using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Commande_Fournisseur
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CommandeFournisseur CommandeFournisseur { get; set; } = default!;

        [BindProperty]
        public List<LigneCommandeFournisseurViewModel> LignesCommande { get; set; } = new();

        public SelectList Fournisseurs { get; set; } = default!;
        public List<Article> Articles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            CommandeFournisseur = new CommandeFournisseur
            {
                NumeroCommande = await GenerateNumeroCommandeAsync(),
                DateCommande = DateTime.Now,
                Statut = StatutCommande.EnAttente
            };

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Les propriétés de navigation ne viennent pas du formulaire
            ModelState.Remove("CommandeFournisseur.Fournisseur");
            ModelState.Remove("CommandeFournisseur.LignesCommande");
            ModelState.Remove("CommandeFournisseur.BonsReception");

            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            if (LignesCommande == null || !LignesCommande.Any())
            {
                ModelState.AddModelError("", "Veuillez ajouter au moins un article à la commande.");
                await LoadDataAsync();
                return Page();
            }

            CommandeFournisseur.MontantHT = LignesCommande.Sum(l => l.MontantLigne);
            CommandeFournisseur.MontantTVA = CommandeFournisseur.MontantHT * 0.20m;
            CommandeFournisseur.MontantTTC = CommandeFournisseur.MontantHT + CommandeFournisseur.MontantTVA;
            CommandeFournisseur.DateCreation = DateTime.Now;

            CommandeFournisseur.LignesCommande = LignesCommande.Select(l => new LigneCommandeFournisseur
            {
                ArticleId = l.ArticleId,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                MontantLigne = l.MontantLigne
            }).ToList();

            _context.CommandesFournisseur.Add(CommandeFournisseur);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Commande fournisseur créée avec succès !";
            return RedirectToPage("./Index");
        }

        private async Task LoadDataAsync()
        {
            var fournisseurs = await _context.Fournisseurs
                .OrderBy(f => f.Nom)
                .ToListAsync();

            Fournisseurs = new SelectList(fournisseurs, "Id", "Nom");

            Articles = await _context.Articles
                .Where(a => a.EstActif)
                .OrderBy(a => a.Libelle)
                .ToListAsync();
        }

        private async Task<string> GenerateNumeroCommandeAsync()
        {
            var last = await _context.CommandesFournisseur
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            int next = (last?.Id ?? 0) + 1;
            return $"CF{DateTime.Now.Year}{next:D6}";
        }
    }
}