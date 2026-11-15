using GestionCommerciale.Data;
using GestionCommerciale.Enums;
using GestionCommerciale.Models;
using GestionCommerciale.Services.Interface;
using Microsoft.EntityFrameworkCore;

public class DevisService : IDevisService
{
    private const string TYPE_DOC = "DEV";
    private readonly ApplicationDbContext _context;
    private readonly INumerotationService _numerotation;

    public DevisService(ApplicationDbContext context, INumerotationService numerotation)
    {
        _context = context;
        _numerotation = numerotation;
    }

    // ---------- Calculs (arrondi au millime à chaque étape) ----------
    private static decimal R3(decimal v) => Math.Round(v, 3, MidpointRounding.AwayFromZero);

    private static void CalculerLigne(LigneDevis l)
    {
        l.MontantBrutHT = R3(l.Quantite * l.PrixUnitaireHT);
        l.MontantRemise = R3(l.MontantBrutHT * l.TauxRemise / 100m);
        l.MontantHT = l.MontantBrutHT - l.MontantRemise;
        l.MontantTVA = R3(l.MontantHT * l.TauxTVA / 100m);
        l.MontantTTC = l.MontantHT + l.MontantTVA;
    }

    private static void CalculerTotaux(Devis d)
    {
        foreach (var l in d.Lignes) CalculerLigne(l);
        d.MontantBrutHT = d.Lignes.Sum(l => l.MontantBrutHT);
        d.MontantRemise = d.Lignes.Sum(l => l.MontantRemise);
        d.MontantHT = d.Lignes.Sum(l => l.MontantHT);
        d.MontantTVA = d.Lignes.Sum(l => l.MontantTVA);
        d.MontantTTC = d.Lignes.Sum(l => l.MontantTTC);
    }

    // ---------- Création ----------
    public async Task<Devis> CreerAsync(Devis devis, string utilisateur)
    {
        if (!devis.Lignes.Any())
            throw new InvalidOperationException("Le devis doit contenir au moins une ligne.");

        // Si c'est un client, on vide les champs prospect pour éviter les incohérences
        if (devis.ClientId != null)
        {
            devis.NomProspect = devis.TelephoneProspect = devis.EmailProspect = null;
            devis.AdresseProspect = devis.MatriculeFiscalProspect = null;
        }

        await using var tx = await _context.Database.BeginTransactionAsync();

        devis.Numero = await _numerotation.ProchainNumeroAsync(TYPE_DOC, devis.DateDevis);
        devis.Version = 1;
        devis.Statut = StatutDevis.Brouillon;
        devis.CreatedBy = utilisateur;
        devis.DateCreation = DateTime.Now;

        int ordre = 1;
        foreach (var l in devis.Lignes) l.Ordre = ordre++;

        CalculerTotaux(devis);

        _context.Devis.Add(devis);
        await _context.SaveChangesAsync();
        await tx.CommitAsync();
        return devis;
    }

    // ---------- Modification (brouillon uniquement) ----------
    public async Task MettreAJourAsync(Devis saisie)
    {
        var devis = await _context.Devis.Include(d => d.Lignes)
                        .FirstOrDefaultAsync(d => d.Id == saisie.Id)
                    ?? throw new InvalidOperationException("Devis introuvable.");

        if (!devis.EstModifiable)
            throw new InvalidOperationException(
                "Seul un devis en brouillon est modifiable. Créez une nouvelle version.");

        devis.DateDevis = saisie.DateDevis;
        devis.DateValidite = saisie.DateValidite;
        devis.ClientId = saisie.ClientId;
        devis.NomProspect = saisie.NomProspect;
        devis.TelephoneProspect = saisie.TelephoneProspect;
        devis.EmailProspect = saisie.EmailProspect;
        devis.AdresseProspect = saisie.AdresseProspect;
        devis.MatriculeFiscalProspect = saisie.MatriculeFiscalProspect;
        devis.Conditions = saisie.Conditions;
        devis.Notes = saisie.Notes;
        devis.DateModification = DateTime.Now;

        // Remplacement complet des lignes (simple et fiable pour un brouillon)
        _context.LigneDevis.RemoveRange(devis.Lignes);
        devis.Lignes.Clear();
        int ordre = 1;
        foreach (var l in saisie.Lignes)
        {
            l.Id = 0;
            l.Ordre = ordre++;
            devis.Lignes.Add(l);
        }

        CalculerTotaux(devis);
        await _context.SaveChangesAsync();
    }

    // ---------- Changements de statut ----------
    private async Task<Devis> ChargerAsync(int id) =>
        await _context.Devis.Include(d => d.Lignes).FirstOrDefaultAsync(d => d.Id == id)
        ?? throw new InvalidOperationException("Devis introuvable.");

    private static void Exiger(Devis d, params StatutDevis[] autorises)
    {
        if (!autorises.Contains(d.Statut))
            throw new InvalidOperationException(
                $"Action impossible : le devis est au statut « {d.Statut} ».");
    }

    public async Task EnvoyerAsync(int devisId)
    {
        var d = await ChargerAsync(devisId);
        Exiger(d, StatutDevis.Brouillon);
        d.Statut = StatutDevis.Envoye;
        d.DateModification = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task AccepterAsync(int devisId)
    {
        var d = await ChargerAsync(devisId);
        Exiger(d, StatutDevis.Envoye);
        if (d.DateValidite < DateTime.Today)
            throw new InvalidOperationException("Ce devis est expiré. Créez une nouvelle version.");
        d.Statut = StatutDevis.Accepte;
        d.DateModification = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    public async Task RefuserAsync(int devisId)
    {
        var d = await ChargerAsync(devisId);
        Exiger(d, StatutDevis.Envoye);
        d.Statut = StatutDevis.Refuse;
        d.DateModification = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    // ---------- Nouvelle version ----------
    public async Task<Devis> NouvelleVersionAsync(int devisId, string utilisateur)
    {
        var source = await ChargerAsync(devisId);
        Exiger(source, StatutDevis.Envoye, StatutDevis.Refuse, StatutDevis.Expire);

        // Les prix sont recopiés depuis le devis source, PAS depuis l'article.
        // L'utilisateur pourra les modifier tant que la nouvelle version est en brouillon.
        var nouvelle = new Devis
        {
            Numero = source.Numero,      // même numéro, version +1
            Version = await _context.Devis.Where(d => d.Numero == source.Numero)
                                          .MaxAsync(d => d.Version) + 1,
            DevisParentId = source.Id,
            DateDevis = DateTime.Today,
            DateValidite = DateTime.Today.AddDays((source.DateValidite - source.DateDevis).Days),
            ClientId = source.ClientId,
            NomProspect = source.NomProspect,
            TelephoneProspect = source.TelephoneProspect,
            EmailProspect = source.EmailProspect,
            AdresseProspect = source.AdresseProspect,
            MatriculeFiscalProspect = source.MatriculeFiscalProspect,
            Conditions = source.Conditions,
            Notes = source.Notes,
            CreatedBy = utilisateur,
            Lignes = source.Lignes.OrderBy(l => l.Ordre).Select(l => new LigneDevis
            {
                ArticleId = l.ArticleId,
                Designation = l.Designation,
                Quantite = l.Quantite,
                PrixUnitaireHT = l.PrixUnitaireHT,
                TauxRemise = l.TauxRemise,
                TauxTVA = l.TauxTVA,
                Ordre = l.Ordre
            }).ToList()
        };

        CalculerTotaux(nouvelle);
        source.Statut = StatutDevis.Remplace;
        source.DateModification = DateTime.Now;

        _context.Devis.Add(nouvelle);
        await _context.SaveChangesAsync();
        return nouvelle;
    }

    // ---------- Transformation en commande ----------
    /// <param name="clientIdExistant">
    /// Pour un prospect : id d'un client existant auquel le rattacher,
    /// ou null pour créer automatiquement la fiche client.
    /// </param>
    public async Task<CommandeClient> TransformerEnCommandeAsync(
        int devisId, int? clientIdExistant, string utilisateur)
    {
        var devis = await ChargerAsync(devisId);
        Exiger(devis, StatutDevis.Accepte);

        await using var tx = await _context.Database.BeginTransactionAsync();

        // 1. Résoudre le client
        int clientId;
        if (devis.ClientId != null)
        {
            clientId = devis.ClientId.Value;
        }
        else if (clientIdExistant != null)
        {
            clientId = clientIdExistant.Value;
        }
        else
        {
            var client = new Client
            {
                Nom = devis.NomProspect!,
                Telephone = devis.TelephoneProspect,
                Email = devis.EmailProspect,
                Adresse = devis.AdresseProspect,
                MatriculeFiscale = devis.MatriculeFiscalProspect
            };
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            clientId = client.Id;
        }
        devis.ClientId = clientId;   // le devis est désormais rattaché au client

        // 2. Créer la commande
        var commande = new CommandeClient
        {
            NumeroCommande = await _numerotation.ProchainNumeroAsync("CC", DateTime.Today),
            DateCommande = DateTime.Today,
            ClientId = clientId,
            Statut = StatutCommande.EnAttente,
            Commentaire = $"Issue du devis {devis.NumeroComplet}",
            MontantHT = devis.MontantHT,
            MontantTVA = devis.MontantTVA,
            MontantTTC = devis.MontantTTC,
            LignesCommande = devis.Lignes.OrderBy(l => l.Ordre).Select(l => new LigneCommandeClient
            {
                ArticleId = l.ArticleId,
                Quantite = l.Quantite,
                // La ligne de commande n'a pas encore de remise : on reporte le prix net
                PrixUnitaire = R3(l.MontantHT / l.Quantite),
                MontantLigne = l.MontantHT,
                QuantiteLivree = 0
            }).ToList()
        };
        _context.CommandesClient.Add(commande);
        await _context.SaveChangesAsync();

        // 3. Clôturer le devis
        devis.CommandeClientId = commande.Id;   
        devis.Statut = StatutDevis.Transforme;
        devis.DateModification = DateTime.Now;

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        // La réservation de stock se fera à la VALIDATION de la commande, pas ici.
        return commande;
    }

    // ---------- Expiration automatique ----------
    public async Task<int> MarquerExpiresAsync() =>
        await _context.Devis
            .Where(d => d.Statut == StatutDevis.Envoye && d.DateValidite < DateTime.Today)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.Statut, StatutDevis.Expire)
                .SetProperty(d => d.DateModification, DateTime.Now));
}