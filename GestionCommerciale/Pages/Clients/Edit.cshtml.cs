using GestionCommerciale.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestionCommerciale.Pages.Clients
{
    public class EditClientModel : PageModel
    {
        private readonly ILogger<EditClientModel> _logger;

        public EditClientModel(ILogger<EditClientModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public ClientEditViewModel Client { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        // Listes pour les dropdowns
        public List<SelectListItem> Zones { get; set; } = new();
        public List<SelectListItem> TypesCategorie { get; set; } = new();
        public List<SelectListItem> Representants { get; set; } = new();

        public string PageTitle => "Modifier le Client";

        // Stocker les valeurs originales pour détecter les changements
        public ClientEditViewModel OriginalClient { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                if (Id <= 0)
                {
                    TempData["ErrorMessage"] = "ID client invalide.";
                    return RedirectToPage("./Index");
                }

                // Initialiser les listes de sélection
                await InitializeSelectLists();

                // Charger le client existant (simulation)
                var client = await LoadClientById(Id);
                if (client == null)
                {
                    TempData["ErrorMessage"] = "Client introuvable.";
                    return RedirectToPage("./Index");
                }

                Client = client;

                // Conserver une copie des données originales pour détecter les changements
                OriginalClient = CloneClient(client);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du chargement du client {Id} pour modification");
                TempData["ErrorMessage"] = "Erreur lors du chargement du client.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Réinitialiser les listes de sélection en cas d'erreur
                await InitializeSelectLists();

                // Charger les données originales pour comparaison
                var originalClient = await LoadClientById(Id);
                if (originalClient == null)
                {
                    TempData["ErrorMessage"] = "Client introuvable.";
                    return RedirectToPage("./Index");
                }

                OriginalClient = originalClient;

                // Validation personnalisée
                await ValidateClientData(originalClient);

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                // Préparer les données pour la modification
                PrepareClientForUpdate();

                // Vérifier l'unicité du code client (sauf si pas changé)
                if (Client.CodeClient != originalClient.CodeClient &&
                    await CheckCodeClientExists(Client.CodeClient))
                {
                    ModelState.AddModelError("Client.CodeClient", "Ce code client existe déjà.");
                    return Page();
                }

                // Vérifier l'unicité de l'email (sauf si pas changé)
                if (!string.IsNullOrEmpty(Client.Email) &&
                    Client.Email != originalClient.Email &&
                    await CheckEmailExists(Client.Email))
                {
                    ModelState.AddModelError("Client.Email", "Cette adresse email est déjà utilisée.");
                    return Page();
                }

                // Détecter et logger les changements
                var changes = DetectChanges(originalClient, Client);

                // Simuler la modification du client
                await UpdateClientSimulation(Client, changes);

                _logger.LogInformation($"Client {Id} modifié avec {changes.Count} changement(s) par {User.Identity?.Name ?? "Anonyme"}");

                TempData["SuccessMessage"] = $"Le client {Client.Prenom} {Client.Nom} a été modifié avec succès.";

                // Rediriger vers la page de détails
                return RedirectToPage("./ClientDetails", new { id = Id, mode = "view" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la modification du client {Id}");
                ModelState.AddModelError("", "Une erreur est survenue lors de la modification du client. Veuillez réessayer.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                if (Id <= 0)
                {
                    TempData["ErrorMessage"] = "ID client invalide.";
                    return RedirectToPage("./Index");
                }

                // Vérifier si le client existe
                var client = await LoadClientById(Id);
                if (client == null)
                {
                    TempData["ErrorMessage"] = "Client introuvable.";
                    return RedirectToPage("./Index");
                }

                // Simuler la suppression
                await DeleteClientSimulation(Id);

                _logger.LogInformation($"Client {Id} ({client.Prenom} {client.Nom}) supprimé par {User.Identity?.Name ?? "Anonyme"}");

                TempData["SuccessMessage"] = $"Le client {client.Prenom} {client.Nom} a été supprimé avec succès.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression du client {Id}");
                TempData["ErrorMessage"] = "Une erreur est survenue lors de la suppression du client.";
                return RedirectToPage("./ClientDetails", new { id = Id, mode = "view" });
            }
        }

        // Charger un client par ID (simulation)
        private async Task<ClientEditViewModel?> LoadClientById(int clientId)
        {
            // Simuler un délai de base de données
            await Task.Delay(150);

            // Base de données simulée de clients
            var simulatedClients = new Dictionary<int, ClientEditViewModel>
            {
                [1] = new ClientEditViewModel
                {
                    Id = 1,
                    CodeClient = "CLI001",
                    Nom = "DUPONT",
                    Prenom = "Jean",
                    Email = "jean.dupont@email.com",
                    Telephone = "01.23.45.67.89",
                    Adresse = "123 Rue de la Paix",
                    CodePostal = "75001",
                    Ville = "Paris",
                    AdresseLivraison = "",
                    Zone = "Nord",
                    TypeCategorie = "VIP",
                    RemiseParDefaut = 5,
                    Representant = "Marie Martin",
                    MatriculeFiscal = "MF123456789",
                    CodeTva = "FR12345678901",
                    Cin = "CIN123456",
                    Banque = "BNP Paribas",
                    Compte = "12345678901",
                    Rib = "FR761751200000D123456789",
                    Criteres = "Client privilégié - Excellent historique de paiement\nCommandes régulières mensuelles",
                    MontantCredit = 5000,
                    Solde = 1250.50m,
                    DateCreation = DateTime.Now.AddMonths(-6),
                    DateModification = DateTime.Now.AddDays(-5),
                    IsActive = true
                },
                [2] = new ClientEditViewModel
                {
                    Id = 2,
                    CodeClient = "CLI002",
                    Nom = "MARTIN",
                    Prenom = "Sophie",
                    Email = "sophie.martin@entreprise.com",
                    Telephone = "02.34.56.78.90",
                    Adresse = "456 Avenue des Champs",
                    CodePostal = "69001",
                    Ville = "Lyon",
                    AdresseLivraison = "456 Avenue des Champs - Service Livraison",
                    Zone = "Sud",
                    TypeCategorie = "Grossiste",
                    RemiseParDefaut = 10,
                    Representant = "Pierre Dubois",
                    MatriculeFiscal = "MF987654321",
                    CodeTva = "FR98765432109",
                    Cin = "CIN987654",
                    Banque = "Crédit Agricole",
                    Compte = "98765432109",
                    Rib = "FR141751200000D987654321",
                    Criteres = "Grossiste avec volumes importants\nNégociation tarifaire annuelle",
                    MontantCredit = 10000,
                    Solde = 2500.75m,
                    DateCreation = DateTime.Now.AddMonths(-12),
                    DateModification = DateTime.Now.AddDays(-2),
                    IsActive = true
                },
                [3] = new ClientEditViewModel
                {
                    Id = 3,
                    CodeClient = "CLI003",
                    Nom = "BERNARD",
                    Prenom = "Luc",
                    Email = "luc.bernard@gmail.com",
                    Telephone = "03.45.67.89.01",
                    Adresse = "789 Boulevard de la République",
                    CodePostal = "13001",
                    Ville = "Marseille",
                    Zone = "Sud",
                    TypeCategorie = "Standard",
                    RemiseParDefaut = 2,
                    Representant = "Sophie Moreau",
                    MatriculeFiscal = "MF456789123",
                    CodeTva = "FR45678912345",
                    Cin = "CIN456789",
                    Banque = "Société Générale",
                    Compte = "45678912345",
                    Rib = "FR301751200000D456789123",
                    Criteres = "Client standard avec potentiel de croissance",
                    MontantCredit = 2000,
                    Solde = 500.25m,
                    DateCreation = DateTime.Now.AddMonths(-3),
                    DateModification = DateTime.Now.AddDays(-10),
                    IsActive = true
                },
                [4] = new ClientEditViewModel
                {
                    Id = 4,
                    CodeClient = "CLI004",
                    Nom = "LEROY",
                    Prenom = "Emma",
                    Email = "emma.leroy@startup.fr",
                    Telephone = "04.56.78.90.12",
                    Adresse = "321 Rue de l'Innovation",
                    CodePostal = "33000",
                    Ville = "Bordeaux",
                    Zone = "Ouest",
                    TypeCategorie = "Partenaire",
                    RemiseParDefaut = 15,
                    Representant = "Lucas Bernard",
                    MontantCredit = 7500,
                    Solde = -150.00m,
                    DateCreation = DateTime.Now.AddMonths(-2),
                    DateModification = null,
                    IsActive = true
                },
                [5] = new ClientEditViewModel
                {
                    Id = 5,
                    CodeClient = "CLI005",
                    Nom = "MOREAU",
                    Prenom = "Antoine",
                    Email = "antoine.moreau@traditionnel.com",
                    Telephone = "05.67.89.01.23",
                    Adresse = "654 Place du Marché",
                    CodePostal = "67000",
                    Ville = "Strasbourg",
                    Zone = "Est",
                    TypeCategorie = "Détaillant",
                    RemiseParDefaut = 7,
                    Representant = "Julie Petit",
                    MontantCredit = 3000,
                    Solde = 875.30m,
                    DateCreation = DateTime.Now.AddMonths(-8),
                    DateModification = DateTime.Now.AddDays(-1),
                    IsActive = false
                }
            };

            return simulatedClients.TryGetValue(clientId, out var client) ? client : null;
        }

        // Méthode pour valider les données du client
        private async Task ValidateClientData(ClientEditViewModel originalClient)
        {
            // Validation du code client
            if (string.IsNullOrWhiteSpace(Client.CodeClient))
            {
                ModelState.AddModelError("Client.CodeClient", "Le code client est obligatoire.");
            }
            else if (Client.CodeClient.Length < 3)
            {
                ModelState.AddModelError("Client.CodeClient", "Le code client doit contenir au moins 3 caractères.");
            }

            // Validation de l'email
            if (!string.IsNullOrWhiteSpace(Client.Email))
            {
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(Client.Email))
                {
                    ModelState.AddModelError("Client.Email", "Le format de l'email n'est pas valide.");
                }
            }

            // Validation du téléphone
            if (!string.IsNullOrWhiteSpace(Client.Telephone))
            {
                var phoneRegex = new System.Text.RegularExpressions.Regex(@"^[\d\s\.\-\+\(\)]+$");
                if (!phoneRegex.IsMatch(Client.Telephone))
                {
                    ModelState.AddModelError("Client.Telephone", "Le format du téléphone n'est pas valide.");
                }
            }

            // Validation du code postal
            if (!string.IsNullOrWhiteSpace(Client.CodePostal))
            {
                var postalRegex = new System.Text.RegularExpressions.Regex(@"^\d{5}$");
                if (!postalRegex.IsMatch(Client.CodePostal))
                {
                    ModelState.AddModelError("Client.CodePostal", "Le code postal doit contenir 5 chiffres.");
                }
            }

            // Validation de la remise
            if (Client.RemiseParDefaut.HasValue && (Client.RemiseParDefaut < 0 || Client.RemiseParDefaut > 100))
            {
                ModelState.AddModelError("Client.RemiseParDefaut", "La remise doit être comprise entre 0 et 100%.");
            }

            // Validation des montants
            if (Client.MontantCredit.HasValue && Client.MontantCredit < 0)
            {
                ModelState.AddModelError("Client.MontantCredit", "Le montant de crédit ne peut pas être négatif.");
            }

            await Task.CompletedTask;
        }

        // Initialiser les listes de sélection
        private async Task InitializeSelectLists()
        {
            // Zones
            Zones = new List<SelectListItem>
            {
                new() { Text = "Sélectionner une zone...", Value = "" },
                new() { Text = "Nord", Value = "Nord" },
                new() { Text = "Sud", Value = "Sud" },
                new() { Text = "Est", Value = "Est" },
                new() { Text = "Ouest", Value = "Ouest" },
                new() { Text = "Centre", Value = "Centre" },
                new() { Text = "International", Value = "International" }
            };

            // Types de catégorie
            TypesCategorie = new List<SelectListItem>
            {
                new() { Text = "Sélectionner...", Value = "" },
                new() { Text = "Standard", Value = "Standard" },
                new() { Text = "VIP", Value = "VIP" },
                new() { Text = "Grossiste", Value = "Grossiste" },
                new() { Text = "Détaillant", Value = "Détaillant" },
                new() { Text = "Partenaire", Value = "Partenaire" }
            };

            // Représentants (données fictives)
            Representants = new List<SelectListItem>
            {
                new() { Text = "Sélectionner un représentant...", Value = "" },
                new() { Text = "Marie Martin", Value = "Marie Martin" },
                new() { Text = "Pierre Dubois", Value = "Pierre Dubois" },
                new() { Text = "Sophie Moreau", Value = "Sophie Moreau" },
                new() { Text = "Lucas Bernard", Value = "Lucas Bernard" },
                new() { Text = "Julie Petit", Value = "Julie Petit" },
                new() { Text = "Thomas Durand", Value = "Thomas Durand" },
                new() { Text = "Emma Leroy", Value = "Emma Leroy" }
            };

            await Task.CompletedTask;
        }

        // Préparer le client pour la mise à jour
        private void PrepareClientForUpdate()
        {
            // Nettoyer les données
            Client.CodeClient = Client.CodeClient?.Trim().ToUpper();
            Client.Nom = Client.Nom?.Trim().ToUpper();
            Client.Prenom = Client.Prenom?.Trim();
            Client.Email = Client.Email?.Trim().ToLower();
            Client.Telephone = Client.Telephone?.Trim();
            Client.Ville = Client.Ville?.Trim();
            Client.CodePostal = Client.CodePostal?.Trim();

            // Si l'adresse de livraison n'est pas spécifiée, utiliser l'adresse principale
            if (string.IsNullOrWhiteSpace(Client.AdresseLivraison))
            {
                Client.AdresseLivraison = Client.Adresse;
            }

            // Mettre à jour la date de modification
            Client.DateModification = DateTime.Now;
        }

        // Simuler la vérification d'unicité du code client
        private async Task<bool> CheckCodeClientExists(string codeClient)
        {
            // Codes clients existants (simulation)
            var existingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CLI001", "CLI002", "CLI003", "CLI004", "CLI005", "CLIDUP001", "CLITEST", "CLIVIP001"
            };

            await Task.Delay(100);
            return existingCodes.Contains(codeClient?.Trim().ToUpper() ?? "");
        }

        // Simuler la vérification d'unicité de l'email
        private async Task<bool> CheckEmailExists(string email)
        {
            // Emails existants (simulation)
            var existingEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "jean.dupont@email.com",
                "sophie.martin@entreprise.com",
                "luc.bernard@gmail.com",
                "emma.leroy@startup.fr",
                "antoine.moreau@traditionnel.com",
                "test@test.com",
                "admin@monentreprise.com"
            };

            await Task.Delay(100);
            return existingEmails.Contains(email?.Trim().ToLower() ?? "");
        }

        // Cloner un client pour comparaison
        private ClientEditViewModel CloneClient(ClientEditViewModel original)
        {
            return new ClientEditViewModel
            {
                Id = original.Id,
                CodeClient = original.CodeClient,
                Nom = original.Nom,
                Prenom = original.Prenom,
                Email = original.Email,
                Telephone = original.Telephone,
                Adresse = original.Adresse,
                CodePostal = original.CodePostal,
                Ville = original.Ville,
                AdresseLivraison = original.AdresseLivraison,
                Zone = original.Zone,
                TypeCategorie = original.TypeCategorie,
                RemiseParDefaut = original.RemiseParDefaut,
                Representant = original.Representant,
                MatriculeFiscal = original.MatriculeFiscal,
                CodeTva = original.CodeTva,
                Cin = original.Cin,
                Banque = original.Banque,
                Compte = original.Compte,
                Rib = original.Rib,
                Criteres = original.Criteres,
                MontantCredit = original.MontantCredit,
                Solde = original.Solde,
                DateCreation = original.DateCreation,
                DateModification = original.DateModification,
                IsActive = original.IsActive
            };
        }

        // Détecter les changements entre l'original et la version modifiée
        private Dictionary<string, (object? OldValue, object? NewValue)> DetectChanges(
            ClientEditViewModel original, ClientEditViewModel modified)
        {
            var changes = new Dictionary<string, (object?, object?)>();

            if (original.CodeClient != modified.CodeClient)
                changes["CodeClient"] = (original.CodeClient, modified.CodeClient);

            if (original.Nom != modified.Nom)
                changes["Nom"] = (original.Nom, modified.Nom);

            if (original.Prenom != modified.Prenom)
                changes["Prenom"] = (original.Prenom, modified.Prenom);

            if (original.Email != modified.Email)
                changes["Email"] = (original.Email, modified.Email);

            if (original.Telephone != modified.Telephone)
                changes["Telephone"] = (original.Telephone, modified.Telephone);

            if (original.Adresse != modified.Adresse)
                changes["Adresse"] = (original.Adresse, modified.Adresse);

            if (original.CodePostal != modified.CodePostal)
                changes["CodePostal"] = (original.CodePostal, modified.CodePostal);

            if (original.Ville != modified.Ville)
                changes["Ville"] = (original.Ville, modified.Ville);

            if (original.AdresseLivraison != modified.AdresseLivraison)
                changes["AdresseLivraison"] = (original.AdresseLivraison, modified.AdresseLivraison);

            if (original.Zone != modified.Zone)
                changes["Zone"] = (original.Zone, modified.Zone);

            if (original.TypeCategorie != modified.TypeCategorie)
                changes["TypeCategorie"] = (original.TypeCategorie, modified.TypeCategorie);

            if (original.RemiseParDefaut != modified.RemiseParDefaut)
                changes["RemiseParDefaut"] = (original.RemiseParDefaut, modified.RemiseParDefaut);

            if (original.Representant != modified.Representant)
                changes["Representant"] = (original.Representant, modified.Representant);

            if (original.MatriculeFiscal != modified.MatriculeFiscal)
                changes["MatriculeFiscal"] = (original.MatriculeFiscal, modified.MatriculeFiscal);

            if (original.CodeTva != modified.CodeTva)
                changes["CodeTva"] = (original.CodeTva, modified.CodeTva);

            if (original.Cin != modified.Cin)
                changes["Cin"] = (original.Cin, modified.Cin);

            if (original.Banque != modified.Banque)
                changes["Banque"] = (original.Banque, modified.Banque);

            if (original.Compte != modified.Compte)
                changes["Compte"] = (original.Compte, modified.Compte);

            if (original.Rib != modified.Rib)
                changes["Rib"] = (original.Rib, modified.Rib);

            if (original.Criteres != modified.Criteres)
                changes["Criteres"] = (original.Criteres, modified.Criteres);

            if (original.MontantCredit != modified.MontantCredit)
                changes["MontantCredit"] = (original.MontantCredit, modified.MontantCredit);

            if (original.Solde != modified.Solde)
                changes["Solde"] = (original.Solde, modified.Solde);

            if (original.IsActive != modified.IsActive)
                changes["IsActive"] = (original.IsActive, modified.IsActive);

            return changes;
        }

        // Simuler la mise à jour du client
        private async Task UpdateClientSimulation(ClientEditViewModel client, Dictionary<string, (object?, object?)> changes)
        {
            // Simuler un délai de mise à jour en base de données
            await Task.Delay(200);

            // Logger les changements détaillés
            foreach (var change in changes)
            {
                //_logger.LogInformation($"Champ {change.Key}: '{change.Value.OldValue}' -> '{change.Value.NewValue}'");
            }

            _logger.LogInformation($"Client {client.Id} mis à jour avec succès");
        }

        // Simuler la suppression du client
        private async Task DeleteClientSimulation(int clientId)
        {
            // Simuler un délai de suppression
            await Task.Delay(150);

            _logger.LogInformation($"Client {clientId} marqué comme supprimé");
        }

        // API pour vérifier l'unicité du code client (appelée en AJAX)
        public async Task<IActionResult> OnGetCheckCodeClientAsync(string codeClient, int currentId)
        {
            if (string.IsNullOrWhiteSpace(codeClient))
            {
                return new JsonResult(new { exists = false });
            }

            // Charger le client actuel pour exclure son propre code de la vérification
            var currentClient = await LoadClientById(currentId);
            if (currentClient != null && currentClient.CodeClient.Equals(codeClient, StringComparison.OrdinalIgnoreCase))
            {
                return new JsonResult(new { exists = false }); // Pas de changement
            }

            var exists = await CheckCodeClientExists(codeClient);
            return new JsonResult(new { exists });
        }

        // API pour vérifier l'unicité de l'email (appelée en AJAX)
        public async Task<IActionResult> OnGetCheckEmailAsync(string email, int currentId)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new JsonResult(new { exists = false });
            }

            // Charger le client actuel pour exclure son propre email de la vérification
            var currentClient = await LoadClientById(currentId);
            if (currentClient != null && currentClient.Email?.Equals(email, StringComparison.OrdinalIgnoreCase) == true)
            {
                return new JsonResult(new { exists = false }); // Pas de changement
            }

            var exists = await CheckEmailExists(email);
            return new JsonResult(new { exists });
        }

        // API pour obtenir des suggestions de ville basées sur le code postal
        public async Task<IActionResult> OnGetGetCitySuggestionsAsync(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode) || postalCode.Length != 5)
            {
                return new JsonResult(new { suggestions = new string[0] });
            }

            try
            {
                await Task.Delay(50);
                var suggestions = GetCitySuggestionsByPostalCode(postalCode);
                return new JsonResult(new { suggestions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la recherche de villes pour le code postal {postalCode}");
                return new JsonResult(new { suggestions = new string[0] });
            }
        }

        // Suggestions de villes par code postal
        private string[] GetCitySuggestionsByPostalCode(string postalCode)
        {
            var cityMappings = new Dictionary<string, string[]>
            {
                { "75001", new[] { "Paris 1er Arrondissement" } },
                { "75008", new[] { "Paris 8ème Arrondissement" } },
                { "69001", new[] { "Lyon 1er Arrondissement" } },
                { "13001", new[] { "Marseille 1er Arrondissement" } },
                { "33000", new[] { "Bordeaux" } },
                { "59000", new[] { "Lille" } },
                { "67000", new[] { "Strasbourg" } },
                { "44000", new[] { "Nantes" } },
                { "34000", new[] { "Montpellier" } },
                { "31000", new[] { "Toulouse" } }
            };

            return cityMappings.TryGetValue(postalCode, out var cities) ? cities : new string[0];
        }
    }
}