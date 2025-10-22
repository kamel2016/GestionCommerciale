using GestionCommerciale.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestionCommerciale.Pages.Clients
{
    public class CreateModel : PageModel
    {
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ILogger<CreateModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public ClientCreateViewModel Client { get; set; } = new();

        // Listes pour les dropdowns
        public List<SelectListItem> Zones { get; set; } = new();
        public List<SelectListItem> TypesCategorie { get; set; } = new();
        public List<SelectListItem> Representants { get; set; } = new();

        public string PageTitle => "Nouveau Client";

        public async Task<IActionResult> OnGetAsync(int? duplicateId = null)
        {
            try
            {
                // Initialiser les listes de sélection
                await InitializeSelectLists();

                // Si un ID de duplication est fourni, charger les données du client à dupliquer
                if (duplicateId.HasValue)
                {
                    await LoadClientForDuplication(duplicateId.Value);
                }
                else
                {
                    // Initialiser un nouveau client avec des valeurs par défaut
                    InitializeNewClient();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la page de création de client");
                TempData["ErrorMessage"] = "Erreur lors du chargement de la page.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Réinitialiser les listes de sélection en cas d'erreur
                await InitializeSelectLists();

                // Validation personnalisée
                await ValidateClientData();

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                // Préparer les données pour la création
                PrepareClientForCreation();

                // Vérifier l'unicité du code client (simulation)
                if (await CheckCodeClientExists(Client.CodeClient))
                {
                    ModelState.AddModelError("Client.CodeClient", "Ce code client existe déjà.");
                    return Page();
                }

                // Vérifier l'unicité de l'email (simulation)
                if (!string.IsNullOrEmpty(Client.Email) && await CheckEmailExists(Client.Email))
                {
                    ModelState.AddModelError("Client.Email", "Cette adresse email est déjà utilisée.");
                    return Page();
                }

                // Simuler la création du client
                var clientId = await CreateClientSimulation(Client);

                _logger.LogInformation($"Nouveau client créé avec l'ID {clientId} - Code: {Client.CodeClient}");

                TempData["SuccessMessage"] = $"Le client {Client.Prenom} {Client.Nom} a été créé avec succès (ID: {clientId}).";

                // Rediriger vers la page de détails du nouveau client
                return RedirectToPage("./ClientDetails", new { id = clientId, mode = "view" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du client");
                ModelState.AddModelError("", "Une erreur est survenue lors de la création du client. Veuillez réessayer.");
                return Page();
            }
        }

        // Méthode pour valider les données du client
        private async Task ValidateClientData()
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

        // Initialiser les listes de sélection (données en dur)
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

        // Initialiser un nouveau client
        private void InitializeNewClient()
        {
            Client = new ClientCreateViewModel
            {
                DateCreation = DateTime.Now,
                RemiseParDefaut = 0,
                Solde = 0,
                MontantCredit = 0,
                TypeCategorie = "Standard"
            };

            // Générer un code client temporaire
            Client.CodeClient = GenerateTemporaryClientCode();
        }

        // Charger un client pour duplication (simulation)
        private async Task LoadClientForDuplication(int clientId)
        {
            try
            {
                // Simuler le chargement d'un client existant
                var sourceClient = GetSimulatedClientById(clientId);

                if (sourceClient != null)
                {
                    Client = new ClientCreateViewModel
                    {
                        // Ne pas copier : CodeClient (sera généré), Id, dates
                        Nom = sourceClient.Nom,
                        Prenom = sourceClient.Prenom + " (Copie)",
                        Email = "", // Ne pas copier l'email pour éviter les doublons
                        Telephone = sourceClient.Telephone,
                        Adresse = sourceClient.Adresse,
                        CodePostal = sourceClient.CodePostal,
                        Ville = sourceClient.Ville,
                        AdresseLivraison = sourceClient.AdresseLivraison,
                        Zone = sourceClient.Zone,
                        TypeCategorie = sourceClient.TypeCategorie,
                        RemiseParDefaut = sourceClient.RemiseParDefaut,
                        Representant = sourceClient.Representant,
                        MatriculeFiscal = "",
                        CodeTva = "",
                        Cin = "",
                        Banque = sourceClient.Banque,
                        Compte = "",
                        Rib = "",
                        Criteres = sourceClient.Criteres,
                        MontantCredit = sourceClient.MontantCredit,
                        Solde = 0,
                        DateCreation = DateTime.Now
                    };

                    Client.CodeClient = GenerateTemporaryClientCode();
                    TempData["InfoMessage"] = "Client dupliqué. Veuillez modifier les informations nécessaires avant de sauvegarder.";
                }
                else
                {
                    InitializeNewClient();
                    TempData["WarningMessage"] = "Client introuvable pour la duplication. Un nouveau client vide a été initialisé.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la duplication du client {clientId}");
                InitializeNewClient();
                TempData["WarningMessage"] = "Impossible de dupliquer le client. Un nouveau client vide a été initialisé.";
            }

            await Task.CompletedTask;
        }

        // Simuler la récupération d'un client par ID
        private ClientCreateViewModel? GetSimulatedClientById(int clientId)
        {
            // Quelques clients fictifs pour la simulation
            var simulatedClients = new Dictionary<int, ClientCreateViewModel>
            {
                [1] = new ClientCreateViewModel
                {
                    CodeClient = "CLI001",
                    Nom = "DUPONT",
                    Prenom = "Jean",
                    Email = "jean.dupont@email.com",
                    Telephone = "01.23.45.67.89",
                    Adresse = "123 Rue de la Paix",
                    CodePostal = "75001",
                    Ville = "Paris",
                    Zone = "Nord",
                    TypeCategorie = "VIP",
                    RemiseParDefaut = 5,
                    Representant = "Marie Martin",
                    Banque = "BNP Paribas",
                    Criteres = "Client privilégié - Excellent historique de paiement",
                    MontantCredit = 5000,
                    Solde = 1250
                },
                [2] = new ClientCreateViewModel
                {
                    CodeClient = "CLI002",
                    Nom = "MARTIN",
                    Prenom = "Sophie",
                    Email = "sophie.martin@entreprise.com",
                    Telephone = "02.34.56.78.90",
                    Adresse = "456 Avenue des Champs",
                    CodePostal = "69001",
                    Ville = "Lyon",
                    Zone = "Sud",
                    TypeCategorie = "Grossiste",
                    RemiseParDefaut = 10,
                    Representant = "Pierre Dubois",
                    Banque = "Crédit Agricole",
                    MontantCredit = 10000,
                    Solde = 2500
                },
                [3] = new ClientCreateViewModel
                {
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
                    Banque = "Société Générale",
                    MontantCredit = 2000,
                    Solde = 500
                }
            };

            return simulatedClients.TryGetValue(clientId, out var client) ? client : null;
        }

        // Générer un code client temporaire
        private string GenerateTemporaryClientCode()
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmm");
            return $"CLI{timestamp}";
        }

        // Préparer le client pour la création
        private void PrepareClientForCreation()
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

            // Définir les dates
            Client.DateCreation = DateTime.Now;
        }

        // Simuler la vérification d'unicité du code client
        private async Task<bool> CheckCodeClientExists(string codeClient)
        {
            // Simuler quelques codes clients existants
            var existingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CLI001", "CLI002", "CLI003", "CLIDUP001", "CLITEST", "CLIVIP001"
            };

            // Simuler un délai de base de données
            await Task.Delay(100);

            return existingCodes.Contains(codeClient?.Trim().ToUpper() ?? "");
        }

        // Simuler la vérification d'unicité de l'email
        private async Task<bool> CheckEmailExists(string email)
        {
            // Simuler quelques emails existants
            var existingEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "jean.dupont@email.com",
                "sophie.martin@entreprise.com",
                "luc.bernard@gmail.com",
                "test@test.com",
                "admin@monentreprise.com"
            };

            // Simuler un délai de base de données
            await Task.Delay(100);

            return existingEmails.Contains(email?.Trim().ToLower() ?? "");
        }

        // Simuler la création du client
        private async Task<int> CreateClientSimulation(ClientCreateViewModel client)
        {
            // Simuler un délai de création en base de données
            await Task.Delay(200);

            // Générer un ID aléatoire pour le nouveau client
            var clientId = new Random().Next(1000, 9999);

            // Logger les informations du client créé (simulation)
            _logger.LogInformation($"Client créé - ID: {clientId}, Code: {client.CodeClient}, Nom: {client.Nom}, Prénom: {client.Prenom}");

            return clientId;
        }

        // API pour vérifier l'unicité du code client (appelée en AJAX)
        public async Task<IActionResult> OnGetCheckCodeClientAsync(string codeClient)
        {
            if (string.IsNullOrWhiteSpace(codeClient))
            {
                return new JsonResult(new { exists = false });
            }

            var exists = await CheckCodeClientExists(codeClient);
            return new JsonResult(new { exists });
        }

        // API pour vérifier l'unicité de l'email (appelée en AJAX)
        public async Task<IActionResult> OnGetCheckEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new JsonResult(new { exists = false });
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
                // Simuler un délai de recherche
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

        // Suggestions de villes par code postal (données fictives)
        private string[] GetCitySuggestionsByPostalCode(string postalCode)
        {
            var cityMappings = new Dictionary<string, string[]>
            {
                // Paris
                { "75001", new[] { "Paris 1er Arrondissement" } },
                { "75002", new[] { "Paris 2ème Arrondissement" } },
                { "75003", new[] { "Paris 3ème Arrondissement" } },
                { "75008", new[] { "Paris 8ème Arrondissement" } },
                { "75016", new[] { "Paris 16ème Arrondissement" } },
                
                // Lyon
                { "69001", new[] { "Lyon 1er Arrondissement" } },
                { "69002", new[] { "Lyon 2ème Arrondissement" } },
                { "69003", new[] { "Lyon 3ème Arrondissement" } },
                { "69006", new[] { "Lyon 6ème Arrondissement" } },
                
                // Marseille
                { "13001", new[] { "Marseille 1er Arrondissement" } },
                { "13002", new[] { "Marseille 2ème Arrondissement" } },
                { "13008", new[] { "Marseille 8ème Arrondissement" } },
                
                // Autres grandes villes
                { "33000", new[] { "Bordeaux" } },
                { "33100", new[] { "Bordeaux", "Talence" } },
                { "59000", new[] { "Lille" } },
                { "59800", new[] { "Lille" } },
                { "67000", new[] { "Strasbourg" } },
                { "44000", new[] { "Nantes" } },
                { "44100", new[] { "Nantes" } },
                { "34000", new[] { "Montpellier" } },
                { "31000", new[] { "Toulouse" } },
                { "31100", new[] { "Toulouse" } },
                { "35000", new[] { "Rennes" } },
                { "21000", new[] { "Dijon" } },
                { "76000", new[] { "Rouen" } },
                { "80000", new[] { "Amiens" } },
                { "25000", new[] { "Besançon" } },
                { "87000", new[] { "Limoges" } }
            };

            return cityMappings.TryGetValue(postalCode, out var cities) ? cities : new string[0];
        }
    }
}