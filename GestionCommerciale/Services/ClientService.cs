using GestionCommerciale.Data;
using GestionCommerciale.Helpers;
using GestionCommerciale.Models;
using GestionCommerciale.Services.Interface;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Services
{
    public class ClientService : IClientService
    {
        public const string PrefixeCode = "CLI-";
        public const int NombreMaxEssais = 3;
        public const string NomIndexCode = "IX_Clients_Code";

        private readonly ApplicationDbContext _context;

        public ClientService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ResultatClient> CreerAsync(Client client, CancellationToken cancellationToken = default)
        {
            Normaliser(client);

            var resultat = await ControlerAsync(client, null, cancellationToken);

            if (!resultat.Succes)
                return resultat;

            client.DateCreation = DateTime.Now;
            client.DateModification = null;
            client.EstActif = true;

            // Le code est généré ICI et nulle part ailleurs.
            for (int essai = 1; essai <= NombreMaxEssais; essai++)
            {
                client.Code = await CalculerProchainCodeAsync(essai, cancellationToken);
                _context.Clients.Add(client);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    return resultat; // créé
                }
                catch (DbUpdateException ex) when (EstViolationIndexCode(ex))
                {
                    // Un autre utilisateur a pris ce code entre-temps : on détache l'entité et on recommence.
                    _context.Entry(client).State = EntityState.Detached;
                }
                // Toute autre DbUpdateException n'est PAS interceptée : elle remonte telle quelle.
            }

            resultat.Erreurs.Add((string.Empty,
                "Impossible de générer un code client unique après plusieurs essais. Veuillez réessayer."));
            return resultat;
        }

        public async Task<FicheClient?> ObtenirFicheAsync(int id, CancellationToken cancellationToken = default)
        {
            var client = await ObtenirAsync(id, cancellationToken);
            if (client == null)
                return null;

            var commandes = _context.CommandesClient.AsNoTracking().Where(c => c.ClientId == id);

            int nombre = await commandes.CountAsync(cancellationToken);

            var dernieres = await commandes
                .OrderByDescending(c => c.DateCommande)
                .ThenByDescending(c => c.Id)
                .Take(5)
                .Select(c => new CommandeResume(c.Id, c.NumeroCommande, c.DateCommande, c.Statut, c.MontantTTC))
                .ToListAsync(cancellationToken);

            return new FicheClient
            {
                Client = client,
                NombreCommandes = nombre,
                DernieresCommandes = dernieres,
                ModeFacturationVerrouille = await ADesDocumentsAsync(id, cancellationToken)
            };
        }

        public async Task<Client?> ObtenirAsync(int id, CancellationToken cancellationToken = default)
            => await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public async Task<ResultatClient> ModifierAsync(int id, Client saisie, CancellationToken cancellationToken = default)
        {
            var existant = await _context.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (existant == null)
            {
                var introuvable = new ResultatClient { Introuvable = true };
                introuvable.Erreurs.Add((string.Empty, "Client introuvable."));
                return introuvable;
            }

            Normaliser(saisie);

            var resultat = await ControlerAsync(saisie, id, cancellationToken);

            // Le mode de facturation est verrouillé dès que le client a un document.
            if (saisie.ModeFacturation != existant.ModeFacturation
                && await ADesDocumentsAsync(id, cancellationToken))
            {
                resultat.Erreurs.Add((nameof(Client.ModeFacturation), MessageModeFacturationVerrouille));
            }

            if (!resultat.Succes)
                return resultat;

            // Copie champ par champ : Id, Code et DateCreation ne sont JAMAIS repris de la saisie.
            existant.TypeClient = saisie.TypeClient;
            existant.ModeFacturation = saisie.ModeFacturation;
            existant.Nom = saisie.Nom;
            existant.Prenom = saisie.TypeClient == TypeClient.Societe ? null : saisie.Prenom;
            existant.MatriculeFiscale = saisie.MatriculeFiscale;
            existant.Email = saisie.Email;
            existant.Telephone = saisie.Telephone;
            existant.Mobile = saisie.Mobile;
            existant.Adresse = saisie.Adresse;
            existant.Ville = saisie.Ville;
            existant.CodePostal = saisie.CodePostal;
            existant.Gouvernorat = saisie.Gouvernorat;
            existant.PlafondCredit = saisie.PlafondCredit;
            existant.DelaiPaiement = saisie.DelaiPaiement;
            existant.RemiseDefaut = saisie.RemiseDefaut;
            existant.EstActif = saisie.EstActif;
            existant.SoumisRetenueSource = saisie.SoumisRetenueSource;
            existant.TauxRetenueSource = saisie.TauxRetenueSource;
            existant.DateModification = DateTime.Now;

            await _context.SaveChangesAsync(cancellationToken);
            return resultat;
        }

        public const string MessageModeFacturationVerrouille =
            "Le mode de facturation ne peut plus être modifié : ce client a déjà des documents "
            + "(commandes, devis, bons de livraison ou factures).";

        public async Task<bool> ADesDocumentsAsync(int clientId, CancellationToken cancellationToken = default)
        {
            return await _context.CommandesClient.AsNoTracking().AnyAsync(d => d.ClientId == clientId, cancellationToken)
                || await _context.Devis.AsNoTracking().AnyAsync(d => d.ClientId == clientId, cancellationToken)
                || await _context.BonsLivraison.AsNoTracking().AnyAsync(d => d.ClientId == clientId, cancellationToken)
                || await _context.Factures.AsNoTracking().AnyAsync(d => d.ClientId == clientId, cancellationToken);
        }

        /// <summary>Contrôles communs à la création et à la modification.</summary>
        private async Task<ResultatClient> ControlerAsync(Client client, int? idExclu, CancellationToken cancellationToken)
        {
            var resultat = new ResultatClient();

            if (string.IsNullOrEmpty(client.Nom))
                resultat.Erreurs.Add((nameof(Client.Nom), "Le nom est obligatoire."));

            foreach (var (champ, message) in ClientValidation.Valider(client))
                resultat.Erreurs.Add((champ, message));

            var unicite = await VerifierUniciteAsync(client, idExclu, cancellationToken);
            resultat.Erreurs.AddRange(unicite.Erreurs);

            return resultat;
        }

        public async Task<ResultatClient> VerifierUniciteAsync(
            Client client, int? idExclu = null, CancellationToken cancellationToken = default)
        {
            var resultat = new ResultatClient();
            int exclu = idExclu ?? 0;

            var email = NormaliserCle(client.Email);
            if (email != null)
            {
                bool existe = await _context.Clients.AsNoTracking().AnyAsync(c =>
                    c.Id != exclu
                    && c.Email != null
                    && c.Email.Trim().ToLower() == email, cancellationToken);

                if (existe)
                    resultat.Erreurs.Add((nameof(Client.Email), "Un client avec cet email existe déjà."));
            }

            var matricule = NormaliserCle(client.MatriculeFiscale);
            if (matricule != null)
            {
                bool existe = await _context.Clients.AsNoTracking().AnyAsync(c =>
                    c.Id != exclu
                    && c.MatriculeFiscale != null
                    && c.MatriculeFiscale.Trim().ToLower() == matricule, cancellationToken);

                if (existe)
                    resultat.Erreurs.Add((nameof(Client.MatriculeFiscale), "Un client avec ce matricule fiscal existe déjà."));
            }

            return resultat;
        }

        /// <summary>
        /// Calcule le prochain code (CLI-00001, CLI-00002, ...). Surchargeable pour les tests.
        /// </summary>
        protected virtual async Task<string> CalculerProchainCodeAsync(int essai, CancellationToken cancellationToken)
        {
            // On prend les codes les plus longs puis les plus grands (le tri alphabétique seul
            // se tromperait après CLI-99999), et on garde le plus grand numéro lisible.
            var codes = await _context.Clients.AsNoTracking()
                .Where(c => c.Code.StartsWith(PrefixeCode))
                .OrderByDescending(c => c.Code.Length)
                .ThenByDescending(c => c.Code)
                .Select(c => c.Code)
                .Take(20)
                .ToListAsync(cancellationToken);

            int dernier = codes
                .Select(c => int.TryParse(c.AsSpan(PrefixeCode.Length), out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"{PrefixeCode}{dernier + 1:D5}";
        }

        /// <summary>
        /// Vrai seulement si l'erreur vient de l'index unique du code client
        /// (SQL Server : erreur 2601/2627 sur IX_Clients_Code ; SQLite des tests : UNIQUE sur Clients.Code).
        /// </summary>
        public static bool EstViolationIndexCode(DbUpdateException exception)
        {
            for (Exception? e = exception; e != null; e = e.InnerException)
            {
                if (e is SqlException sql
                    && (sql.Number == 2601 || sql.Number == 2627)
                    && sql.Message.Contains(NomIndexCode, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (e.GetType().Name == "SqliteException"
                    && e.Message.Contains("UNIQUE constraint failed: Clients.Code", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void Normaliser(Client client)
        {
            client.Nom = (client.Nom ?? string.Empty).Trim();
            client.Prenom = NettoyerFacultatif(client.Prenom);
            client.Email = NettoyerFacultatif(client.Email);
            client.MatriculeFiscale = NettoyerFacultatif(client.MatriculeFiscale);
            client.Telephone = NettoyerFacultatif(client.Telephone);
            client.Mobile = NettoyerFacultatif(client.Mobile);
            client.Adresse = NettoyerFacultatif(client.Adresse);
            client.Ville = NettoyerFacultatif(client.Ville);
            client.CodePostal = NettoyerFacultatif(client.CodePostal);
            client.Gouvernorat = NettoyerFacultatif(client.Gouvernorat);
        }

        private static string? NettoyerFacultatif(string? valeur)
        {
            var v = valeur?.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        private static string? NormaliserCle(string? valeur)
        {
            var v = valeur?.Trim().ToLowerInvariant();
            return string.IsNullOrEmpty(v) ? null : v;
        }
    }
}
