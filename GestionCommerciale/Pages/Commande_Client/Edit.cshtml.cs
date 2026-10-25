// Pages/Commande_Client/Edit.cshtml.cs

using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace GestionCommerciale.Pages.Commande_Client
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public CommandeInput Input { get; set; } = new();

        // Lignes envoyées en JSON par le JS
        [BindProperty]
        public string LignesJson { get; set; } = "[]";

        public SelectList ClientsList { get; set; } = null!;
        public List<ArticleOption> ArticlesDisponibles { get; set; } = new();
        public List<LigneDto> LignesExistantes { get; set; } = new();
        public List<string> ModesPaiement { get; set; } = new()
        {
            "Espèces", "Chèque", "Virement", "Carte bancaire", "Prélèvement"
        };

        public class CommandeInput
        {
            public int Id { get; set; }

            [Required]
            public string NumeroCommande { get; set; } = string.Empty;

            [Required(ErrorMessage = "Le client est obligatoire")]
            public int? ClientId { get; set; }

            [Required(ErrorMessage = "La date de commande est obligatoire")]
            [DataType(DataType.Date)]
            public DateTime DateCommande { get; set; } = DateTime.Today;

            [DataType(DataType.Date)]
            public DateTime? DateLivraison { get; set; }

            [Required]
            public StatutCommande Statut { get; set; } = StatutCommande.EnAttente;

            [StringLength(1000)]
            public string? Commentaire { get; set; }

            public DateTime DateCreation { get; set; }
        }

        public class ArticleOption
        {
            public int Id { get; set; }
            public string Reference { get; set; } = string.Empty;
            public string Libelle { get; set; } = string.Empty;
            public decimal PrixUnitaireHT { get; set; }
            public decimal TauxTVA { get; set; }
            public int StockDisponible { get; set; }
        }

        public class LigneDto
        {
            public int ArticleId { get; set; }
            public string ArticleNom { get; set; } = string.Empty;
            public int Quantite { get; set; }
            public decimal PrixUnitaire { get; set; }
            public decimal TauxTVA { get; set; }
            public decimal MontantLigne { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var commande = await _context.CommandesClient
                .Include(c => c.LignesCommande)
                    .ThenInclude(l => l.Article)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (commande == null)
            {
                return NotFound();
            }

            Input = new CommandeInput
            {
                Id = commande.Id,
                NumeroCommande = commande.NumeroCommande,
                ClientId = commande.ClientId,
                DateCommande = commande.DateCommande,
                DateLivraison = commande.DateLivraison,
                Statut = commande.Statut,
                Commentaire = commande.Commentaire,
                DateCreation = commande.DateCreation
            };

            LignesExistantes = commande.LignesCommande.Select(l => new LigneDto
            {
                ArticleId = l.ArticleId,
                ArticleNom = l.Article?.Libelle ?? string.Empty,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                TauxTVA = l.Article?.TauxTVA ?? 0,
                MontantLigne = l.PrixUnitaire * l.Quantite
            }).ToList();

            LignesJson = JsonSerializer.Serialize(LignesExistantes);

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var lignes = ParseLignes(LignesJson);

            if (!lignes.Any())
            {
                ModelState.AddModelError(string.Empty, "Ajoutez au moins un article à la commande.");
            }

            if (!ModelState.IsValid)
            {
                LignesExistantes = lignes;
                await LoadDataAsync();
                return Page();
            }

            try
            {
                var commande = await _context.CommandesClient
                    .Include(c => c.LignesCommande)
                    .FirstOrDefaultAsync(c => c.Id == Input.Id);

                if (commande == null)
                {
                    return NotFound();
                }

                var client = await _context.Clients.FindAsync(Input.ClientId!.Value);
                if (client == null)
                {
                    ModelState.AddModelError("Input.ClientId", "Client introuvable.");
                    LignesExistantes = lignes;
                    await LoadDataAsync();
                    return Page();
                }

                // 1. Restituer le stock des anciennes lignes
                var anciennesLignes = commande.LignesCommande.ToList();
                var anciennesArticleIds = anciennesLignes.Select(l => l.ArticleId).Distinct().ToList();
                var articlesAnciens = await _context.Articles
                    .Where(a => anciennesArticleIds.Contains(a.ID))
                    .ToDictionaryAsync(a => a.ID);

                foreach (var ancienne in anciennesLignes)
                {
                    if (articlesAnciens.TryGetValue(ancienne.ArticleId, out var art))
                    {
                        art.Quantite += ancienne.Quantite;
                    }
                }

                // 2. Charger les nouveaux articles et vérifier le stock
                var nouvellesIds = lignes.Select(l => l.ArticleId).Distinct().ToList();
                var articles = await _context.Articles
                    .Where(a => nouvellesIds.Contains(a.ID))
                    .ToDictionaryAsync(a => a.ID);

                foreach (var l in lignes)
                {
                    if (!articles.TryGetValue(l.ArticleId, out var article))
                    {
                        ModelState.AddModelError(string.Empty, "Un article sélectionné est introuvable.");
                        LignesExistantes = lignes;
                        await LoadDataAsync();
                        return Page();
                    }

                    if (article.Quantite < l.Quantite)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Stock insuffisant pour « {article.Libelle} » (disponible : {article.Quantite}).");
                        LignesExistantes = lignes;
                        await LoadDataAsync();
                        return Page();
                    }
                }

                // 3. Remplacer les lignes et recalculer les montants
                _context.LignesCommandeClient.RemoveRange(anciennesLignes);

                decimal totalHT = 0;
                decimal totalTVA = 0;

                foreach (var l in lignes)
                {
                    var article = articles[l.ArticleId];
                    var montantLigneHT = article.PrixAchatHT * l.Quantite;
                    totalHT += montantLigneHT;
                    totalTVA += montantLigneHT * (article.TauxTVA / 100m);

                    commande.LignesCommande.Add(new LigneCommandeClient
                    {
                        ArticleId = article.ID,
                        Quantite = l.Quantite,
                        PrixUnitaire = article.PrixVenteHT
                    });

                    article.Quantite -= l.Quantite;
                }

                // 4. Mettre à jour l'entête
                commande.ClientId = client.Id;
                commande.DateCommande = Input.DateCommande;
                commande.DateLivraison = Input.DateLivraison;
                commande.Statut = Input.Statut;
                commande.Commentaire = Input.Commentaire ?? string.Empty;
                commande.MontantHT = Math.Round(totalHT, 2);
                commande.MontantTVA = Math.Round(totalTVA, 2);
                commande.MontantTTC = Math.Round(totalHT + totalTVA, 2);
                commande.DateModification = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Commande {commande.NumeroCommande} modifiée avec succès.";
                return RedirectToPage("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "La commande a été modifiée par un autre utilisateur. Rechargez la page.");
                LignesExistantes = lignes;
                await LoadDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de la commande {Id}", Input.Id);
                ModelState.AddModelError(string.Empty, "Une erreur est survenue lors de l'enregistrement.");
                LignesExistantes = lignes;
                await LoadDataAsync();
                return Page();
            }
        }

        private List<LigneDto> ParseLignes(string json)
        {
            try
            {
                var lignes = JsonSerializer.Deserialize<List<LigneDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                return lignes
                    .Where(l => l.ArticleId > 0 && l.Quantite > 0)
                    .ToList();
            }
            catch
            {
                return new();
            }
        }

        private async Task LoadDataAsync()
        {
            var clients = await _context.Clients
                .Where(c => c.EstActif)
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .Select(c => new { c.Id, Nom = c.Nom + " " + c.Prenom })
                .ToListAsync();

            ClientsList = new SelectList(clients, "Id", "Nom", Input.ClientId);

            ArticlesDisponibles = await _context.Articles
                .Where(a => a.EstActif)
                .OrderBy(a => a.Libelle)
                .Select(a => new ArticleOption
                {
                    Id = a.ID,
                    Reference = a.Reference,
                    Libelle = a.Libelle,
                    PrixUnitaireHT = a.PrixVenteHT,
                    TauxTVA = a.TauxTVA,
                    StockDisponible = a.Quantite
                })
                .ToListAsync();
        }
    }
}