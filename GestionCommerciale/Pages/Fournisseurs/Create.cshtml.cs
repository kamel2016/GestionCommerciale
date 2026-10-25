using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Pages.Fournisseurs
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext db, ILogger<CreateModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        [BindProperty]
        public FournisseurInput Input { get; set; } = new();

        public List<SelectListItem> GouvernoratOptions { get; set; } = new();

        // Les 24 gouvernorats de Tunisie
        private static readonly string[] Gouvernorats =
        {
            "Ariana", "Béja", "Ben Arous", "Bizerte", "Gabès", "Gafsa", "Jendouba",
            "Kairouan", "Kasserine", "Kébili", "Le Kef", "Mahdia", "La Manouba",
            "Médenine", "Monastir", "Nabeul", "Sfax", "Sidi Bouzid", "Siliana",
            "Sousse", "Tataouine", "Tozeur", "Tunis", "Zaghouan"
        };

        public void OnGet()
        {
            LoadOptions();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LoadOptions();
            Normalize();

            // Doublon sur le nom
            if (await _db.Fournisseurs.AnyAsync(f => f.Nom == Input.Nom))
                ModelState.AddModelError("Input.Nom", "Un fournisseur avec cette raison sociale existe déjà.");

            // Doublon sur le matricule fiscal
            if (!string.IsNullOrEmpty(Input.MatriculeFiscale) &&
                await _db.Fournisseurs.AnyAsync(f => f.MatriculeFiscale == Input.MatriculeFiscale))
                ModelState.AddModelError("Input.MatriculeFiscale", "Ce matricule fiscal est déjà utilisé.");

            if (!ModelState.IsValid)
                return Page();

            try
            {
                var fournisseur = new Fournisseur
                {
                    Nom = Input.Nom,
                    MatriculeFiscale = Input.MatriculeFiscale,
                    Email = Input.Email,
                    Telephone = Input.Telephone,
                    Fax = Input.Fax,
                    PersonneContact = Input.PersonneContact,
                    Adresse = Input.Adresse,
                    Ville = Input.Ville,
                    CodePostal = Input.CodePostal,
                    Gouvernorat = Input.Gouvernorat,
                    DelaiLivraison = Input.DelaiLivraison,
                    DelaiPaiement = Input.DelaiPaiement,
                    RemiseAccordee = Input.RemiseAccordee,
                    EstActif = Input.EstActif,
                    DateCreation = DateTime.Now
                };

                _db.Fournisseurs.Add(fournisseur);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Fournisseur « {fournisseur.Nom} » créé avec succès.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du fournisseur");
                ModelState.AddModelError(string.Empty, "Erreur lors de l'enregistrement. Veuillez réessayer.");
                return Page();
            }
        }

        private void LoadOptions()
        {
            GouvernoratOptions = Gouvernorats
                .Select(g => new SelectListItem(g, g, g == Input.Gouvernorat))
                .ToList();
        }

        // Nettoyage : trim + chaînes vides -> null
        private void Normalize()
        {
            Input.Nom = Input.Nom?.Trim() ?? string.Empty;
            Input.MatriculeFiscale = Clean(Input.MatriculeFiscale);
            Input.Email = Clean(Input.Email);
            Input.Telephone = Clean(Input.Telephone);
            Input.Fax = Clean(Input.Fax);
            Input.PersonneContact = Clean(Input.PersonneContact);
            Input.Adresse = Clean(Input.Adresse);
            Input.Ville = Clean(Input.Ville);
            Input.CodePostal = Clean(Input.CodePostal);
            Input.Gouvernorat = Clean(Input.Gouvernorat);
        }

        private static string? Clean(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // ---------- Modèle de saisie ----------
        public class FournisseurInput
        {
            [Required(ErrorMessage = "La raison sociale est obligatoire")]
            [StringLength(100)]
            [Display(Name = "Raison sociale")]
            public string Nom { get; set; } = string.Empty;

            [StringLength(20)]
            [Display(Name = "Matricule fiscale")]
            public string? MatriculeFiscale { get; set; }

            [EmailAddress(ErrorMessage = "Adresse email invalide")]
            [StringLength(255)]
            [Display(Name = "Email")]
            public string? Email { get; set; }

            [StringLength(20)]
            [Display(Name = "Téléphone")]
            public string? Telephone { get; set; }

            [StringLength(20)]
            [Display(Name = "Fax")]
            public string? Fax { get; set; }

            [StringLength(100)]
            [Display(Name = "Personne de contact")]
            public string? PersonneContact { get; set; }

            [StringLength(500)]
            [Display(Name = "Adresse")]
            public string? Adresse { get; set; }

            [StringLength(100)]
            [Display(Name = "Ville")]
            public string? Ville { get; set; }

            [StringLength(10)]
            [Display(Name = "Code postal")]
            public string? CodePostal { get; set; }

            [StringLength(50)]
            [Display(Name = "Gouvernorat")]
            public string? Gouvernorat { get; set; }

            [Range(0, 365, ErrorMessage = "Entre 0 et 365 jours")]
            [Display(Name = "Délai de livraison (jours)")]
            public int DelaiLivraison { get; set; }

            [Range(0, 365, ErrorMessage = "Entre 0 et 365 jours")]
            [Display(Name = "Délai de paiement (jours)")]
            public int DelaiPaiement { get; set; }

            [Range(0, 100, ErrorMessage = "Entre 0 et 100 %")]
            [Display(Name = "Remise accordée (%)")]
            public decimal RemiseAccordee { get; set; }

            [Display(Name = "Actif")]
            public bool EstActif { get; set; } = true;
        }
    }
}