// Pages/GoodsReceipt/Index.cshtml.cs
using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.GoodsReceipt
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public const int PageSizeDefault = 10;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)] public string? Status { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DateFrom { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? DateTo { get; set; }
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = PageSizeDefault;
        public int TotalPages { get; private set; }
        public int TotalRecords { get; private set; }

        public List<GoodsReceiptViewModel> GoodsReceipts { get; set; } = new();
        public List<string> StatutsDisponibles { get; } =
            Enum.GetNames<StatutBonReception>().ToList();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var query = _context.BonsReception
                    .AsNoTracking()
                    .Include(b => b.Fournisseur)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var term = SearchTerm.Trim();
                    query = query.Where(b =>
                        b.NumeroInterne.Contains(term) ||
                        (b.ReferenceFournisseur != null && b.ReferenceFournisseur.Contains(term)) ||
                        b.Fournisseur.Nom.Contains(term));
                }

                if (!string.IsNullOrWhiteSpace(Status) &&
                    Enum.TryParse<StatutBonReception>(Status, out var statut))
                {
                    query = query.Where(b => b.Statut == statut);
                }

                if (DateFrom.HasValue)
                    query = query.Where(b => b.DateReception >= DateFrom.Value.Date);

                if (DateTo.HasValue)
                    query = query.Where(b => b.DateReception <= DateTo.Value.Date);

                TotalRecords = await query.CountAsync();
                TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)PageSize));
                PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

                GoodsReceipts = await query
                    .OrderByDescending(b => b.DateReception)
                    .ThenByDescending(b => b.Id)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .Select(b => new GoodsReceiptViewModel
                    {
                        Id = b.Id,
                        InternalNumber = b.NumeroInterne,
                        SupplierCode = b.Fournisseur.MatriculeFiscale,
                        SupplierName = b.Fournisseur.Nom,
                        SupplierReference = b.ReferenceFournisseur ?? string.Empty,
                        ReceiptDate = b.DateReception,
                        ReceiptTime = b.HeureReception,
                        AmountExcludingTax = b.MontantHT,
                        AmountIncludingTax = b.MontantTTC,
                        Status = b.Statut.ToString(),
                        CreatedBy = b.CreatedBy,
                        CreatedAt = b.DateCreation
                    })
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des bons de réception");
                TempData["ErrorMessage"] = "Erreur lors du chargement des données.";
                return Page();
            }
        }
    }
}