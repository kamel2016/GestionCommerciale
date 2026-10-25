//// Pages/CommandeClient/Index.cshtml.cs

//using GestionCommerciale.Data;
//using GestionCommerciale.Enums;
//using GestionCommerciale.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.EntityFrameworkCore;

//namespace GestionCommerciale.Pages.CommandeClient
//{
//    public class IndexModel : PageModel
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly ILogger<IndexModel> _logger;

//        [BindProperty(SupportsGet = true)]
//        public string? SearchString { get; set; }

//        [BindProperty(SupportsGet = true)]
//        public StatutCommande? StatutFilter { get; set; }

//        [BindProperty(SupportsGet = true)]
//        public int Page { get; set; } = 1;

//        [BindProperty(SupportsGet = true)]
//        public int PageSize { get; set; } = 10;

//        public IList<Models.CommandeClient> CommandeClients { get; set; } = new List<Models.CommandeClient>();
//        public int TotalCount { get; set; }
//        public int TotalPages { get; set; }

//        // Statistiques
//        public int TotalCommandes { get; set; }
//        public int EnAttenteCount { get; set; }
//        public int EnPreparationCount { get; set; }
//        public int LivreeCount { get; set; }

//        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
//        {
//            _context = context;
//            _logger = logger;
//        }

//        public async Task OnGetAsync()
//        {
//            try
//            {
//                // Valider la pagination
//                if (Page < 1) Page = 1;
//                if (PageSize < 1 || PageSize > 100) PageSize = 10;

//                // Requête de base
//                var query = _context.CommandesClient
//                    .Include(c => c.Client)
//                    .Include(c => c.LignesCommande)
//                    .AsQueryable();

//                // Recherche
//                if (!string.IsNullOrEmpty(SearchString))
//                {
//                    query = query.Where(c =>
//                        c.NumeroCommande.Contains(SearchString) ||
//                        c.Client.Nom.Contains(SearchString) ||
//                        c.Client.Prenom.Contains(SearchString) ||
//                        c.Client.Email.Contains(SearchString)
//                    );
//                }

//                // Filtre statut
//                if (StatutFilter.HasValue)
//                {
//                    query = query.Where(c => c.Statut == StatutFilter.Value);
//                }

//                // Compter total
//                TotalCount = await query.CountAsync();
//                TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);

//                // Pagination
//                CommandeClients = await query
//                    .OrderByDescending(c => c.DateCommande)
//                    .Skip((Page - 1) * PageSize)
//                    .Take(PageSize)
//                    .ToListAsync();

//                // Statistiques globales (sans filtres)
//                var allCommandes = _context.CommandesClient.AsQueryable();
//                TotalCommandes = await allCommandes.CountAsync();
//                EnAttenteCount = await allCommandes.CountAsync(c => c.Statut == StatutCommande.EnAttente);
//                EnPreparationCount = await allCommandes.CountAsync(c => c.Statut == StatutCommande.EnPreparation);
//                LivreeCount = await allCommandes.CountAsync(c => c.Statut == StatutCommande.Livree);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erreur lors du chargement des commandes clients");
//                TempData["Error"] = "Une erreur s'est produite lors du chargement des commandes.";
//            }
//        }

//        public async Task<IActionResult> OnPostDeleteAsync(int id)
//        {
//            try
//            {
//                var commande = await _context.CommandesClient.FindAsync(id);
//                if (commande == null)
//                {
//                    TempData["Error"] = "Commande non trouvée.";
//                    return RedirectToPage();
//                }

//                // Vérifier si la commande peut être supprimée
//                if (commande.Statut == StatutCommande.Livree)
//                {
//                    TempData["Error"] = "Impossible de supprimer une commande livrée.";
//                    return RedirectToPage();
//                }

//                // Supprimer les lignes de commande
//                var lignes = _context.LignesCommandeClient.Where(l => l.CommandeClientId == id);
//                _context.LignesCommandeClient.RemoveRange(lignes);

//                // Supprimer la commande
//                _context.CommandesClient.Remove(commande);
//                await _context.SaveChangesAsync();

//                TempData["Success"] = "Commande supprimée avec succès.";
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Erreur lors de la suppression de la commande {Id}", id);
//                TempData["Error"] = "Erreur lors de la suppression.";
//            }

//            return RedirectToPage();
//        }
//    }
//}

// Pages/CommandeClient/Index.cshtml.cs

using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.CommandeClient
{
    public class IndexModel : PageModel
    {
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 100;

        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ---------- Paramètres de requête ----------

        [BindProperty(SupportsGet = true)]
        public string? SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public StatutCommande? StatutFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Page { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = DefaultPageSize;

        // ---------- Données affichées ----------

        public IList<Models.CommandeClient> CommandeClients { get; private set; } = new List<Models.CommandeClient>();
        public int TotalCount { get; private set; }
        public int TotalPages { get; private set; }

        // Statistiques globales (indépendantes des filtres)
        public int TotalCommandes { get; private set; }
        public int EnAttenteCount { get; private set; }
        public int EnPreparationCount { get; private set; }
        public int LivreeCount { get; private set; }
        public int AnnuleeCount { get; private set; }

        // ---------- Handlers ----------

        public async Task OnGetAsync()
        {
            try
            {
                NormalizePaging();

                var query = BuildFilteredQuery();

                TotalCount = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

                CommandeClients = await query
                    .OrderByDescending(c => c.DateCommande)
                    .Skip((Page - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                await LoadStatsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des commandes clients");
                TempData["Error"] = "Une erreur s'est produite lors du chargement des commandes.";
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var commande = await _context.CommandesClient.FindAsync(id);
                if (commande == null)
                {
                    return RedirectWithError("Commande non trouvée.");
                }

                if (commande.Statut == StatutCommande.Livree)
                {
                    return RedirectWithError("Impossible de supprimer une commande livrée.");
                }

                var lignes = _context.LignesCommandeClient.Where(l => l.CommandeClientId == id);
                _context.LignesCommandeClient.RemoveRange(lignes);
                _context.CommandesClient.Remove(commande);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Commande supprimée avec succès.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la commande {Id}", id);
                TempData["Error"] = "Erreur lors de la suppression.";
            }

            return RedirectToPage();
        }

        // ---------- Présentation ----------

        /// <summary>Classe CSS et libellé affichés pour un statut (utilisé par la vue).</summary>
        public (string CssClass, string Label) GetStatutBadge(StatutCommande statut) => statut switch
        {
            StatutCommande.EnAttente => ("cmd-badge--attente", "En attente"),
            StatutCommande.EnPreparation => ("cmd-badge--cours", "En cours"),
            StatutCommande.Livree => ("cmd-badge--livree", "Livrée"),
            StatutCommande.Annulee => ("cmd-badge--annulee", "Annulée"),
            _ => ("cmd-badge--neutre", "Inconnu")
        };

        // ---------- Méthodes privées ----------

        private void NormalizePaging()
        {
            if (Page < 1) Page = 1;
            if (PageSize < 1 || PageSize > MaxPageSize) PageSize = DefaultPageSize;
        }

        private IQueryable<Models.CommandeClient> BuildFilteredQuery()
        {
            // AsSplitQuery évite l'explosion cartésienne due au Include de la collection.
            IQueryable<Models.CommandeClient> query = _context.CommandesClient
                .AsNoTracking()
                .Include(c => c.Client)
                .Include(c => c.LignesCommande)
                .AsSplitQuery();

            if (!string.IsNullOrEmpty(SearchString))
            {
                query = query.Where(c =>
                    c.NumeroCommande.Contains(SearchString) ||
                    c.Client.Nom.Contains(SearchString) ||
                    c.Client.Prenom.Contains(SearchString) ||
                    c.Client.Email.Contains(SearchString));
            }

            if (StatutFilter.HasValue)
            {
                query = query.Where(c => c.Statut == StatutFilter.Value);
            }

            return query;
        }

        /// <summary>Une seule requête SQL (GROUP BY) au lieu de quatre COUNT séparés.</summary>
        private async Task LoadStatsAsync()
        {
            var counts = await _context.CommandesClient
                .AsNoTracking()
                .GroupBy(c => c.Statut)
                .Select(g => new { Statut = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Statut, x => x.Count);

            EnAttenteCount = counts.GetValueOrDefault(StatutCommande.EnAttente);
            EnPreparationCount = counts.GetValueOrDefault(StatutCommande.EnPreparation);
            LivreeCount = counts.GetValueOrDefault(StatutCommande.Livree);
            AnnuleeCount = counts.GetValueOrDefault(StatutCommande.Annulee);
            TotalCommandes = counts.Values.Sum();
        }

        private IActionResult RedirectWithError(string message)
        {
            TempData["Error"] = message;
            return RedirectToPage();
        }
    }
}