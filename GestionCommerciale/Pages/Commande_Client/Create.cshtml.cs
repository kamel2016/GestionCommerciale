// Pages/Commande_Client/Create.cshtml.cs

using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GestionCommerciale.Pages.Commande_Client
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

        [BindProperty]
        public Models.CommandeClient CommandeClient { get; set; } = new();

        // Lignes envoyées en JSON par le JS (évite les soucis de binding indexé)
        [BindProperty]
        public string LignesJson { get; set; } = "[]";

        public SelectList Clients { get; set; } = null!;
        public List<ArticleOption> Articles { get; set; } = new();

        public class ArticleOption
        {
            public int Id { get; set; }
            public string Nom { get; set; } = string.Empty;
            public decimal Prix { get; set; }
            public decimal TauxTVA { get; set; }
            public int Stock { get; set; }
        }

        public class LigneDto
        {
            public int ArticleId { get; set; }
            public int Quantite { get; set; }
            public decimal PrixUnitaire { get; set; }
        }

        public async Task OnGetAsync()
        {
            CommandeClient.DateCommande = DateTime.Today;
            CommandeClient.NumeroCommande = await GenererNumeroCommandeAsync();
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var lignes = ParseLignes(LignesJson);

            if (!lignes.Any())
                ModelState.AddModelError(string.Empty, "Ajoutez au moins un article à la commande.");

            if (CommandeClient.ClientId == null)
                ModelState.AddModelError("CommandeClient.ClientId", "Le client est obligatoire.");

            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                CommandeClient.NumeroCommande = await GenererNumeroCommandeAsync();
                return Page();
            }

            try
            {
                // Charger les articles réels pour recalculer les montants côté serveur
                var articleIds = lignes.Select(l => l.ArticleId).Distinct().ToList();
                var articles = await _context.Articles
                    .Where(a => articleIds.Contains(a.ID))
                    .ToDictionaryAsync(a => a.ID);

                // Vérifications
                foreach (var ligne in lignes)
                {
                    if (!articles.TryGetValue(ligne.ArticleId, out var article))
                    {
                        ModelState.AddModelError(string.Empty, "Un des articles sélectionnés est introuvable.");
                        await LoadDataAsync();
                        CommandeClient.NumeroCommande = await GenererNumeroCommandeAsync();
                        return Page();
                    }

                    if (ligne.Quantite <= 0)
                    {
                        ModelState.AddModelError(string.Empty, $"Quantité invalide pour « {article.Libelle} ».");
                        await LoadDataAsync();
                        CommandeClient.NumeroCommande = await GenererNumeroCommandeAsync();
                        return Page();
                    }

                    if (article.Quantite < ligne.Quantite)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Stock insuffisant pour « {article.Libelle} » (disponible : {article.Quantite}).");
                        await LoadDataAsync();
                        CommandeClient.NumeroCommande = await GenererNumeroCommandeAsync();
                        return Page();
                    }
                }

                // Construction de la commande
                var commande = new Models.CommandeClient
                {
                    NumeroCommande = await GenererNumeroCommandeAsync(),
                    ClientId = CommandeClient.ClientId,
                    DateCommande = CommandeClient.DateCommande,
                    DateLivraison = CommandeClient.DateLivraison,
                    Commentaire = CommandeClient.Commentaire ?? string.Empty,
                    Statut = StatutCommande.EnAttente,
                    DateCreation = DateTime.Now
                };

                decimal totalHT = 0;
                decimal totalTVA = 0;

                foreach (var ligne in lignes)
                {
                    var article = articles[ligne.ArticleId];
                    var prix = article.PrixVenteHT;
                    var montantLigneHT = Math.Round(prix * ligne.Quantite, 2);
                    var montantLigneTVA = Math.Round(montantLigneHT * (article.TauxTVA / 100m), 2);

                    totalHT += montantLigneHT;
                    totalTVA += montantLigneTVA;

                    commande.LignesCommande.Add(new LigneCommandeClient
                    {
                        ArticleId = article.ID,
                        Quantite = ligne.Quantite,
                        PrixUnitaire = prix
                    });

                    // Décrémentation du stock
                    article.Quantite -= ligne.Quantite;
                }

                commande.MontantHT = Math.Round(totalHT, 2);
                commande.MontantTVA = Math.Round(totalTVA, 2);
                commande.MontantTTC = Math.Round(totalHT + totalTVA, 2);

                _context.CommandesClient.Add(commande);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Commande {commande.NumeroCommande} créée avec succès.";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la commande client");
                ModelState.AddModelError(string.Empty, "Une erreur est survenue lors de l'enregistrement.");
                await LoadDataAsync();
                CommandeClient.NumeroCommande = await GenererNumeroCommandeAsync();
                return Page();
            }
        }

        private static List<LigneDto> ParseLignes(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<LigneDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
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
                .Select(c => new { c.Id, c.Nom })
                .ToListAsync();

            Clients = new SelectList(clients, "Id", "Nom");

            Articles = await _context.Articles
                .Where(a => a.EstActif)
                .OrderBy(a => a.Libelle)
                .Select(a => new ArticleOption
                {
                    Id = a.ID,
                    Nom = a.Libelle,
                    Prix = a.PrixVenteHT,
                    TauxTVA = a.TauxTVA,
                    Stock = a.Quantite
                })
                .ToListAsync();
        }

        private async Task<string> GenererNumeroCommandeAsync()
        {
            var prefix = $"CC-{DateTime.Now:yyyyMM}-";
            var dernier = await _context.CommandesClient
                .Where(c => c.NumeroCommande.StartsWith(prefix))
                .OrderByDescending(c => c.NumeroCommande)
                .Select(c => c.NumeroCommande)
                .FirstOrDefaultAsync();

            var suivant = 1;
            if (dernier != null && int.TryParse(dernier.Substring(prefix.Length), out var n))
                suivant = n + 1;

            return $"{prefix}{suivant:D4}";
        }
    }
}