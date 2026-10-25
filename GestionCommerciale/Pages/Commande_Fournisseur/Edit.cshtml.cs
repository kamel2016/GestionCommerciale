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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CommandeFournisseur CommandeFournisseur { get; set; } = default!;

        [BindProperty]
        public List<LigneCommandeFournisseurViewModel> LignesCommande { get; set; } = new();

        public SelectList Fournisseurs { get; set; } = default!;
        public List<Article> Articles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var commande = await _context.CommandesFournisseur
                .Include(c => c.LignesCommande)
                .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande == null)
                return NotFound();

            CommandeFournisseur = commande;

            LignesCommande = commande.LignesCommande.Select(l => new LigneCommandeFournisseurViewModel
            {
                ArticleId = l.ArticleId,
                NomArticle = l.Article.Libelle,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                MontantLigne = l.MontantLigne
            }).ToList();

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            if (!LignesCommande.Any())
            {
                ModelState.AddModelError("", "Veuillez ajouter au moins un article à la commande.");
                await LoadDataAsync();
                return Page();
            }

            var commandeDb = await _context.CommandesFournisseur
                .Include(c => c.LignesCommande)
                .FirstOrDefaultAsync(c => c.Id == CommandeFournisseur.Id);

            if (commandeDb == null)
                return NotFound();

            // Mise à jour des champs d'en-tête
            commandeDb.DateCommande = CommandeFournisseur.DateCommande;
            commandeDb.DateLivraisonPrevue = CommandeFournisseur.DateLivraisonPrevue;
            commandeDb.FournisseurId = CommandeFournisseur.FournisseurId;
            commandeDb.Statut = CommandeFournisseur.Statut;
            commandeDb.Commentaire = CommandeFournisseur.Commentaire;
            commandeDb.DateModification = DateTime.Now;

            // Remplacement des lignes
            _context.LignesCommandeFournisseur.RemoveRange(commandeDb.LignesCommande);
            commandeDb.LignesCommande = LignesCommande.Select(l => new LigneCommandeFournisseur
            {
                ArticleId = l.ArticleId,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                MontantLigne = l.MontantLigne
            }).ToList();

            // Recalcul des montants
            commandeDb.MontantHT = commandeDb.LignesCommande.Sum(l => l.MontantLigne);
            commandeDb.MontantTVA = commandeDb.MontantHT * 0.20m;
            commandeDb.MontantTTC = commandeDb.MontantHT + commandeDb.MontantTVA;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CommandeExistsAsync(CommandeFournisseur.Id))
                    return NotFound();
                throw;
            }

            TempData["Success"] = "Commande fournisseur modifiée avec succès !";
            return RedirectToPage("./Index");
        }

        private async Task LoadDataAsync()
        {
            var fournisseurs = await _context.Fournisseurs.OrderBy(f => f.Nom).ToListAsync();
            Fournisseurs = new SelectList(fournisseurs, "Id", "Nom", CommandeFournisseur.FournisseurId);

            Articles = await _context.Articles
                .Where(a => a.EstActif)
                .OrderBy(a => a.Libelle)
                .ToListAsync();
        }

        private Task<bool> CommandeExistsAsync(int id)
            => _context.CommandesFournisseur.AnyAsync(c => c.Id == id);
    }
}