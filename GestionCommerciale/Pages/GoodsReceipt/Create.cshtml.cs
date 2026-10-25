using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.GoodsReceipt
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty] public InputModel Input { get; set; } = new();

        public List<SelectListItem> Fournisseurs { get; private set; } = new();
        public List<SelectListItem> CommandesFournisseur { get; private set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Le fournisseur est obligatoire")]
            public int FournisseurId { get; set; }

            [Required(ErrorMessage = "La commande fournisseur est obligatoire")]
            public int CommandeFournisseurId { get; set; }

            [StringLength(50)]
            [Display(Name = "Référence fournisseur")]
            public string? ReferenceFournisseur { get; set; }

            [Required]
            [DataType(DataType.Date)]
            public DateTime DateReception { get; set; } = DateTime.Today;

            [DataType(DataType.Time)]
            public TimeSpan HeureReception { get; set; } = DateTime.Now.TimeOfDay;

            [StringLength(1000)]
            public string? Commentaire { get; set; }

            /// <summary>JSON envoyé par le JS : [{ArticleId, Quantite, PrixUnitaire, TauxTVA}]</summary>
            public string LignesJson { get; set; } = "[]";
        }

        public record LigneDto(int ArticleId, int Quantite, decimal PrixUnitaire, decimal TauxTVA);

        public async Task OnGetAsync()
        {
            await ChargerFournisseursAsync();
            await ChargerCommandesAsync(Input.FournisseurId);
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            await ChargerFournisseursAsync();
            await ChargerCommandesAsync(Input.FournisseurId);

            // 1. Lignes envoyées par le client
            var lignes = JsonSerializer.Deserialize<List<LigneDto>>(Input.LignesJson,
                             new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                         ?? new List<LigneDto>();

            if (lignes.Count == 0)
                ModelState.AddModelError(string.Empty, "Ajoutez au moins un article.");

            if (!ModelState.IsValid)
                return Page();

            // 2. Vérifier la commande : existe, appartient au fournisseur, statut ouvert
            var commande = await _context.CommandesFournisseur
                .Include(c => c.LignesCommande)
                .FirstOrDefaultAsync(c => c.Id == Input.CommandeFournisseurId);

            if (commande is null)
            {
                ModelState.AddModelError("Input.CommandeFournisseurId", "La commande sélectionnée est introuvable.");
                return Page();
            }

            if (commande.FournisseurId != Input.FournisseurId)
            {
                ModelState.AddModelError("Input.CommandeFournisseurId",
                    "Cette commande n'appartient pas au fournisseur sélectionné.");
                return Page();
            }

            // 3. Vérifier que chaque article reçu figure dans la commande
            var articleIds = lignes.Select(l => l.ArticleId).Distinct().ToList();
            var articlesCommande = commande.LignesCommande.Select(l => l.ArticleId).ToHashSet();

            if (articleIds.Any(id => !articlesCommande.Contains(id)))
            {
                ModelState.AddModelError(string.Empty,
                    "Un ou plusieurs articles ne figurent pas dans la commande sélectionnée.");
                return Page();
            }

            // 4. Valider les articles en base
            var articles = await _context.Articles
                .AsNoTracking()
                .Where(a => articleIds.Contains(a.ID) && a.EstActif)
                .ToDictionaryAsync(a => a.ID);

            if (articles.Count != articleIds.Count)
            {
                ModelState.AddModelError(string.Empty, "Un ou plusieurs articles sont introuvables ou inactifs.");
                return Page();
            }

            if (lignes.Any(l => l.Quantite <= 0))
            {
                ModelState.AddModelError(string.Empty, "Les quantités doivent être supérieures à zéro.");
                return Page();
            }

            // 5. Vérifier le fournisseur
            var fournisseurExiste = await _context.Fournisseurs
                .AnyAsync(f => f.Id == Input.FournisseurId && f.EstActif);
            if (!fournisseurExiste)
            {
                ModelState.AddModelError("Input.FournisseurId", "Fournisseur invalide.");
                return Page();
            }

            // 6. Construire le bon avec les montants recalculés côté serveur
            var bon = new BonReception
            {
                NumeroInterne = await GenererNumeroAsync(),
                FournisseurId = Input.FournisseurId,
                CommandeFournisseurId = Input.CommandeFournisseurId,
                ReferenceFournisseur = Input.ReferenceFournisseur?.Trim(),
                DateReception = Input.DateReception.Date,
                HeureReception = Input.HeureReception,
                Commentaire = Input.Commentaire?.Trim(),
                Statut = StatutBonReception.Brouillon,
                CreatedBy = User.Identity?.Name ?? "Système",
                DateCreation = DateTime.Now
            };

            decimal totalHT = 0, totalTVA = 0;

            foreach (var l in lignes)
            {
                var article = articles[l.ArticleId];
                var prix = article.PrixAchatHT;          // prix serveur, pas celui du client
                var taux = article.TauxTVA;
                var montantHT = Math.Round(l.Quantite * prix, 2);
                var montantTVA = Math.Round(montantHT * taux / 100m, 2);

                totalHT += montantHT;
                totalTVA += montantTVA;

                bon.Lignes.Add(new LigneBonReception
                {
                    ArticleId = l.ArticleId,
                    Quantite = l.Quantite,
                    PrixUnitaireHT = prix,
                    TauxTVA = taux
                });
            }

            bon.MontantHT = totalHT;
            bon.MontantTVA = totalTVA;
            bon.MontantTTC = totalHT + totalTVA;

            // 7. Enregistrement
            try
            {
                _context.BonsReception.Add(bon);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Bon de réception {bon.NumeroInterne} créé avec succès.";
                _logger.LogInformation("Bon de réception {Numero} créé par {User}", bon.NumeroInterne, bon.CreatedBy);

                return action == "saveAndNew"
                    ? RedirectToPage("Create")
                    : RedirectToPage("Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Erreur d'enregistrement du bon de réception");
                ModelState.AddModelError(string.Empty, "Erreur lors de l'enregistrement. Réessayez.");
                return Page();
            }
        }

        public async Task<IActionResult> OnGetArticlesAsync(int fournisseurId)
        {
            var articles = await _context.Articles
                .Where(a => a.FournisseurId == fournisseurId)
                .Select(a => new
                {
                    id = a.ID,
                    reference = a.Reference,
                    libelle = a.Libelle,
                    qteCommandee = a.Quantite,
                    prixUnitaireHt = a.PrixAchatHT,
                    tva = a.TauxTVA
                })
                .ToListAsync();

            return new JsonResult(articles);
        }

        /// <summary>
        /// Endpoint JSON : commandes ouvertes d'un fournisseur (pour le select dynamique).
        /// Appelé par le JS quand le fournisseur change.
        /// </summary>
        public async Task<JsonResult> OnGetCommandesAsync(int fournisseurId)
        {
            var commandes = await _context.CommandesFournisseur
                .AsNoTracking()
                .Where(c => c.FournisseurId == fournisseurId && c.Statut == StatutCommande.EnAttente)
                .OrderByDescending(c => c.DateCommande)
                .Select(c => new
                {
                    c.Id,
                    Libelle = $"CMD-{c.Id} du {c.DateCommande:dd/MM/yyyy}"
                })
                .ToListAsync();

            return new JsonResult(commandes);
        }

        /// <summary>
        /// Endpoint JSON : articles d'une commande avec quantités et prix, pour pré-remplir le tableau.
        /// </summary>
        public async Task<JsonResult> OnGetLignesCommandeAsync(int commandeId)
        {
            var lignes = await _context.LignesCommandeFournisseur
                .AsNoTracking()
                .Where(l => l.CommandeFournisseurId == commandeId)
                .Select(l => new
                {
                    l.ArticleId,
                    Reference = l.Article.Reference,
                    Libelle = l.Article.Libelle,
                    QuantiteCommandee = l.Quantite,
                    PrixUnitaire = l.Article.PrixAchatHT,
                    TauxTVA = l.Article.TauxTVA
                })
                .ToListAsync();

            return new JsonResult(lignes);
        }

        private async Task ChargerFournisseursAsync()
        {
            Fournisseurs = await _context.Fournisseurs
                .AsNoTracking()
                .Where(f => f.EstActif)
                .OrderBy(f => f.Nom)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Nom
                })
                .ToListAsync();
        }

        private async Task ChargerCommandesAsync(int fournisseurId)
        {
            CommandesFournisseur = fournisseurId <= 0
                ? new List<SelectListItem>()
                : await _context.CommandesFournisseur
                    .AsNoTracking()
                    .Where(c => c.FournisseurId == fournisseurId && c.Statut == StatutCommande.EnAttente)
                    .OrderByDescending(c => c.DateCommande)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = $"CMD-{c.Id} du {c.DateCommande:dd/MM/yyyy}"
                    })
                    .ToListAsync();
        }

        /// <summary>Format : BR-AAAA-0001, incrémenté par année.</summary>
        private async Task<string> GenererNumeroAsync()
        {
            var annee = DateTime.Now.Year;
            var prefixe = $"BR-{annee}-";

            var dernier = await _context.BonsReception
                .Where(b => b.NumeroInterne.StartsWith(prefixe))
                .OrderByDescending(b => b.NumeroInterne)
                .Select(b => b.NumeroInterne)
                .FirstOrDefaultAsync();

            var suivant = 1;
            if (dernier != null && int.TryParse(dernier[prefixe.Length..], out var n))
                suivant = n + 1;

            return $"{prefixe}{suivant:D4}";
        }
    }
}