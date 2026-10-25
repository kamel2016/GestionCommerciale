using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Pages.Clients;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)] public string? SearchTerm { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterStatus { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterType { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterVille { get; set; }
    [BindProperty(SupportsGet = true)] public string SortBy { get; set; } = "name";
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 25;
    [BindProperty(SupportsGet = true)] public int CurrentPage { get; set; } = 1;

    public List<Client> Clients { get; private set; } = new();
    public List<string> Villes { get; private set; } = new();
    public int TotalClients { get; private set; }
    public int ActiveClients { get; private set; }
    public int InactiveClients { get; private set; }
    public int IndividualClients { get; private set; }
    public int CompanyClients { get; private set; }
    public int TotalPages { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PageSize = Math.Clamp(PageSize, 5, 100);
        CurrentPage = Math.Max(CurrentPage, 1);

        var baseQuery = BuildFilteredQuery();
        TotalClients = await baseQuery.CountAsync(cancellationToken);
        ActiveClients = await baseQuery.CountAsync(c => c.EstActif, cancellationToken);
        InactiveClients = TotalClients - ActiveClients;
        IndividualClients = await baseQuery.CountAsync(c => c.TypeClient == TypeClient.Particulier, cancellationToken);
        CompanyClients = TotalClients - IndividualClients;

        Villes = await _context.Clients.AsNoTracking()
            .Where(c => c.Ville != null && c.Ville != "")
            .Select(c => c.Ville!)
            .Distinct().OrderBy(v => v).ToListAsync(cancellationToken);

        TotalPages = (int)Math.Ceiling(TotalClients / (double)PageSize);
        CurrentPage = Math.Min(CurrentPage, Math.Max(TotalPages, 1));

        Clients = await ApplySort(baseQuery)
            .Skip((CurrentPage - 1) * PageSize).Take(PageSize)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Client> BuildFilteredQuery()
    {
        var query = _context.Clients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var term = SearchTerm.Trim();
            var pattern = $"%{term}%";
            query = query.Where(c =>
                EF.Functions.Like(c.Nom, pattern) ||
                (c.Prenom != null && EF.Functions.Like(c.Prenom, pattern)) ||
                (c.Email != null && EF.Functions.Like(c.Email, pattern)) ||
                (c.Telephone != null && EF.Functions.Like(c.Telephone, pattern)) ||
                (c.Mobile != null && EF.Functions.Like(c.Mobile, pattern)) ||
                (c.MatriculeFiscale != null && EF.Functions.Like(c.MatriculeFiscale, pattern)) ||
                (c.Ville != null && EF.Functions.Like(c.Ville, pattern)) ||
                (c.Gouvernorat != null && EF.Functions.Like(c.Gouvernorat, pattern)));
        }
        if (FilterStatus == "active") query = query.Where(c => c.EstActif);
        else if (FilterStatus == "inactive") query = query.Where(c => !c.EstActif);
        if (FilterType == "particulier") query = query.Where(c => c.TypeClient == TypeClient.Particulier);
        else if (FilterType == "societe") query = query.Where(c => c.TypeClient == TypeClient.Societe);
        if (!string.IsNullOrWhiteSpace(FilterVille)) query = query.Where(c => c.Ville == FilterVille);
        return query;
    }

    private IQueryable<Client> ApplySort(IQueryable<Client> query) => SortBy switch
    {
        "name_desc" => query.OrderByDescending(c => c.Nom).ThenByDescending(c => c.Prenom),
        "type" => query.OrderBy(c => c.TypeClient).ThenBy(c => c.Nom),
        "ville" => query.OrderBy(c => c.Ville).ThenBy(c => c.Nom),
        "date" => query.OrderBy(c => c.DateCreation),
        "date_desc" => query.OrderByDescending(c => c.DateCreation),
        _ => query.OrderBy(c => c.Nom).ThenBy(c => c.Prenom)
    };

    public string GetClientName(Client client) => client.NomComplet;
    public string GetTypeText(TypeClient type) => type == TypeClient.Societe ? "Société" : "Particulier";
    public string GetStatusBadgeClass(bool active) => active ? "client-badge-active" : "client-badge-inactive";
    public string GetStatusIcon(bool active) => active ? "fas fa-check-circle" : "fas fa-pause-circle";
    public string GetStatusText(bool active) => active ? "Actif" : "Inactif";

    public async Task<IActionResult> OnPostToggleStatusAsync(int id, CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client == null) TempData["ErrorMessage"] = "Client introuvable.";
        else
        {
            client.EstActif = !client.EstActif;
            client.DateModification = DateTime.Now;
            await _context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = $"Client {client.NomComplet} {(client.EstActif ? "activé" : "désactivé")} avec succès.";
        }
        return RedirectToPage("./Index", BuildRouteValues());
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (client == null) TempData["ErrorMessage"] = "Client introuvable.";
        else
        {
            try
            {
                _context.Clients.Remove(client);
                await _context.SaveChangesAsync(cancellationToken);
                TempData["SuccessMessage"] = "Client supprimé avec succès.";
            }
            catch (DbUpdateException exception)
            {
                _logger.LogWarning(exception, "Suppression impossible du client {ClientId}", id);
                TempData["ErrorMessage"] = "Suppression impossible : ce client est probablement utilisé par une commande.";
            }
        }
        return RedirectToPage("./Index", BuildRouteValues());
    }

    private object BuildRouteValues() => new { SearchTerm, FilterStatus, FilterType, FilterVille, SortBy, PageSize, CurrentPage = 1 };
}
