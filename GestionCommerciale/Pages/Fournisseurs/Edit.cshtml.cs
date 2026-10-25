// Pages/Suppliers/Edit.cshtml.cs

using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Fournisseurs
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        [BindProperty]
        public Fournisseur Fournisseur { get; set; } = null!;

        public int ArticlesCount { get; set; }
        public int CommandesCount { get; set; }

        public List<string> Gouvernorats { get; set; } = new();

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                // Charger le fournisseur
                Fournisseur = await _context.Fournisseurs.FirstOrDefaultAsync(f => f.Id == id);

                if (Fournisseur == null)
                {
                    TempData["Error"] = "Fournisseur non trouvé.";
                    return RedirectToPage("Index");
                }

                // Compter les articles
                ArticlesCount = await _context.Articles
                    .Where(a => a.FournisseurId == id)
                    .CountAsync();

                // Compter les commandes
                CommandesCount = await _context.CommandesFournisseur
                    .Where(c => c.FournisseurId == id)
                    .CountAsync();

                // Charger les gouvernorats
                LoadGouvernorats();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du fournisseur {Id}", id);
                TempData["Error"] = "Une erreur s'est produite.";
                return RedirectToPage("Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    LoadGouvernorats();
                    return Page();
                }

                // Vérifier que le fournisseur existe
                var existingFournisseur = await _context.Fournisseurs
                    .FirstOrDefaultAsync(f => f.Id == Fournisseur.Id);

                if (existingFournisseur == null)
                {
                    TempData["Error"] = "Fournisseur non trouvé.";
                    return RedirectToPage("Index");
                }

                // Vérifier l'unicité de l'email (sauf lui-même)
                if (!string.IsNullOrEmpty(Fournisseur.Email))
                {
                    var emailExists = await _context.Fournisseurs
                        .AnyAsync(f => f.Email == Fournisseur.Email
                            && f.Id != Fournisseur.Id);

                    if (emailExists)
                    {
                        ModelState.AddModelError("Fournisseur.Email", "Cet email est déjà utilisé.");
                        LoadGouvernorats();
                        return Page();
                    }
                }

                // Mettre à jour les propriétés
                existingFournisseur.Nom = Fournisseur.Nom;
                existingFournisseur.MatriculeFiscale = Fournisseur.MatriculeFiscale;
                existingFournisseur.Email = Fournisseur.Email;
                existingFournisseur.Telephone = Fournisseur.Telephone;
                existingFournisseur.Fax = Fournisseur.Fax;
                existingFournisseur.PersonneContact = Fournisseur.PersonneContact;
                existingFournisseur.Adresse = Fournisseur.Adresse;
                existingFournisseur.CodePostal = Fournisseur.CodePostal;
                existingFournisseur.Ville = Fournisseur.Ville;
                existingFournisseur.Gouvernorat = Fournisseur.Gouvernorat;
                existingFournisseur.DelaiLivraison = Fournisseur.DelaiLivraison;
                existingFournisseur.DelaiPaiement = Fournisseur.DelaiPaiement;
                existingFournisseur.RemiseAccordee = Fournisseur.RemiseAccordee;
                existingFournisseur.EstActif = Fournisseur.EstActif;
                existingFournisseur.DateModification = DateTime.Now;

                _context.Fournisseurs.Update(existingFournisseur);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Fournisseur modifié avec succès.";
                return RedirectToPage("Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur base de données lors de la modification");
                ModelState.AddModelError("", "Une erreur base de données s'est produite.");
                LoadGouvernorats();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du fournisseur");
                ModelState.AddModelError("", "Une erreur s'est produite.");
                LoadGouvernorats();
                return Page();
            }
        }

        private void LoadGouvernorats()
        {
            Gouvernorats = new List<string>
            {
                "Tunis",
                "Ariana",
                "Ben Arous",
                "Manouba",
                "Nabeul",
                "Zaghouan",
                "Bizerte",
                "Sousse",
                "Monastir",
                "Mahdia",
                "Sfax",
                "Gabes",
                "Medenine",
                "Tataouine",
                "Gafsa",
                "Tozeur",
                "Kebili",
                "Kasserine",
                "Sidi Bouzid",
                "Kairouan",
                "Jendouba",
                "Le Kef",
                "Siliana"
            };
        }
    }
}