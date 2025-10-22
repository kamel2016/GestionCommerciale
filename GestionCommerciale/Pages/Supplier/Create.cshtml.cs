using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Pages.Suppliers
{
    public class CreateModel : PageModel
    {
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ILogger<CreateModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public CreateSupplierViewModel Supplier { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? DuplicateId { get; set; }

        // Listes pour les dropdowns
        public List<SelectListItem> Zones { get; set; } = new();
        public List<SelectListItem> TypesCategorie { get; set; } = new();
        public List<SelectListItem> TypesFournisseur { get; set; } = new();
        public List<SelectListItem> ConditionsPaiement { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await InitializeSelectLists();
                await InitializeNewSupplier();

                // Si on duplique un fournisseur existant
                if (DuplicateId.HasValue)
                {
                    await LoadSupplierForDuplication(DuplicateId.Value);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la page de création");
                TempData["ErrorMessage"] = "Erreur lors du chargement de la page.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                await InitializeSelectLists();
                await ValidateSupplierData();

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                // Vérifier l'unicité du code fournisseur
                if (await CheckCodeSupplierExists(Supplier.CodeFournisseur))
                {
                    ModelState.AddModelError("Supplier.CodeFournisseur", "Ce code fournisseur existe déjà.");
                    return Page();
                }

                // Vérifier l'unicité de l'email si renseigné
                if (!string.IsNullOrEmpty(Supplier.Email) && await CheckEmailExists(Supplier.Email))
                {
                    ModelState.AddModelError("Supplier.Email", "Cette adresse email est déjà utilisée.");
                    return Page();
                }

                // Préparer les données pour la création
                PrepareSupplierForCreation();

                // Créer le fournisseur (simulation)
                var supplierId = await CreateSupplierSimulation(Supplier);

                _logger.LogInformation($"Nouveau fournisseur créé avec l'ID {supplierId} - Code: {Supplier.CodeFournisseur}");
                TempData["SuccessMessage"] = $"Le fournisseur {Supplier.RaisonSociale} a été créé avec succès.";

                // Nettoyer le localStorage pour éviter les conflits
                TempData["ClearLocalStorage"] = true;

                return RedirectToPage("./Details", new { id = supplierId, mode = "view" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du fournisseur");
                ModelState.AddModelError("", "Erreur lors de la création du fournisseur: " + ex.Message);
                return Page();
            }
        }

        #region Méthodes privées

        private async Task InitializeSelectLists()
        {
            Zones = new List<SelectListItem>
            {
                new() { Text = "Sélectionner une zone...", Value = "" },
                new() { Text = "Île-de-France", Value = "Île-de-France" },
                new() { Text = "Rhône-Alpes", Value = "Rhône-Alpes" },
                new() { Text = "PACA", Value = "PACA" },
                new() { Text = "Nord-Pas-de-Calais", Value = "Nord-Pas-de-Calais" },
                new() { Text = "Aquitaine", Value = "Aquitaine" },
                new() { Text = "Bretagne", Value = "Bretagne" },
                new() { Text = "Centre", Value = "Centre" },
                new() { Text = "International", Value = "International" }
            };

            TypesCategorie = new List<SelectListItem>
            {
                new() { Text = "Sélectionner une catégorie...", Value = "" },
                new() { Text = "Matériel Informatique", Value = "Matériel Informatique" },
                new() { Text = "Fournitures Bureau", Value = "Fournitures Bureau" },
                new() { Text = "Maintenance", Value = "Maintenance" },
                new() { Text = "Consommables", Value = "Consommables" },
                new() { Text = "Mobilier", Value = "Mobilier" },
                new() { Text = "Services", Value = "Services" },
                new() { Text = "Télécommunications", Value = "Télécommunications" },
                new() { Text = "Logiciels", Value = "Logiciels" },
                new() { Text = "Autres", Value = "Autres" }
            };

            TypesFournisseur = new List<SelectListItem>
            {
                new() { Text = "Sélectionner un type...", Value = "" },
                new() { Text = "Fabricant", Value = "Fabricant" },
                new() { Text = "Distributeur", Value = "Distributeur" },
                new() { Text = "Grossiste", Value = "Grossiste" },
                new() { Text = "Détaillant", Value = "Détaillant" },
                new() { Text = "Prestataire de services", Value = "Prestataire de services" },
                new() { Text = "Sous-traitant", Value = "Sous-traitant" },
                new() { Text = "Importateur", Value = "Importateur" },
                new() { Text = "Revendeur", Value = "Revendeur" }
            };

            ConditionsPaiement = new List<SelectListItem>
            {
                new() { Text = "Sélectionner une condition...", Value = "" },
                new() { Text = "Comptant", Value = "Comptant" },
                new() { Text = "15 jours net", Value = "15 jours net" },
                new() { Text = "30 jours net", Value = "30 jours net" },
                new() { Text = "30 jours fin de mois", Value = "30 jours fin de mois" },
                new() { Text = "45 jours net", Value = "45 jours net" },
                new() { Text = "45 jours fin de mois", Value = "45 jours fin de mois" },
                new() { Text = "60 jours net", Value = "60 jours net" },
                new() { Text = "60 jours fin de mois", Value = "60 jours fin de mois" },
                new() { Text = "90 jours", Value = "90 jours" }
            };

            await Task.CompletedTask;
        }

        private async Task InitializeNewSupplier()
        {
            Supplier = new CreateSupplierViewModel
            {
                CodeFournisseur = await GenerateSupplierCode(),
                DateCreation = DateTime.Now,
                DelaiLivraison = 7, // Délai par défaut
                RemiseNegociee = 0,
                PlafondCredit = 0,
                SoldeActuel = 0,
                Pays = "France", // Pays par défaut
                IsActive = true
            };
        }

        private async Task LoadSupplierForDuplication(int supplierId)
        {
            var sourceSupplier = await LoadExistingSupplierAsync(supplierId);
            if (sourceSupplier != null)
            {
                Supplier = new CreateSupplierViewModel
                {
                    // Informations générales (modifiées pour éviter les doublons)
                    CodeFournisseur = await GenerateSupplierCode(),
                    RaisonSociale = sourceSupplier.RaisonSociale + " (Copie)",
                    ContactPrincipal = sourceSupplier.ContactPrincipal,
                    TypeFournisseur = sourceSupplier.TypeFournisseur,
                    TypeCategorie = sourceSupplier.TypeCategorie,
                    Zone = sourceSupplier.Zone,

                    // Contact (email effacé pour éviter les doublons)
                    Email = "",
                    Telephone = sourceSupplier.Telephone,
                    Fax = sourceSupplier.Fax,
                    SiteWeb = sourceSupplier.SiteWeb,

                    // Adresse
                    Adresse = sourceSupplier.Adresse,
                    CodePostal = sourceSupplier.CodePostal,
                    Ville = sourceSupplier.Ville,
                    Pays = sourceSupplier.Pays,

                    // Commercial
                    ConditionPaiement = sourceSupplier.ConditionPaiement,
                    DelaiLivraison = sourceSupplier.DelaiLivraison,
                    RemiseNegociee = sourceSupplier.RemiseNegociee,
                    PlafondCredit = sourceSupplier.PlafondCredit,

                    // Financier (coordonnées bancaires conservées)
                    Banque = sourceSupplier.Banque,

                    // Fiscal (identifiants effacés car uniques)
                    MatriculeFiscal = "",
                    NumeroTva = "",
                    Siret = "",
                    CodeNaf = sourceSupplier.CodeNaf, // Code NAF peut être identique

                    // Système
                    SoldeActuel = 0, // Nouveau solde à zéro
                    DateCreation = DateTime.Now,
                    IsActive = true
                };

                TempData["InfoMessage"] = $"Fournisseur dupliqué depuis '{sourceSupplier.RaisonSociale}'. Veuillez vérifier et modifier les informations nécessaires.";
            }
            else
            {
                await InitializeNewSupplier();
                TempData["WarningMessage"] = "Fournisseur source introuvable pour la duplication. Un nouveau fournisseur vierge a été initialisé.";
            }
        }

        private async Task<string> GenerateSupplierCode()
        {
            // Générer un code unique basé sur la date/heure
            var timestamp = DateTime.Now.ToString("yyMMddHHmm");
            var baseCode = $"FOUR{timestamp}";

            // Vérifier l'unicité et ajouter un suffixe si nécessaire
            var code = baseCode;
            var counter = 1;

            while (await CheckCodeSupplierExists(code))
            {
                code = $"{baseCode}_{counter:D2}";
                counter++;

                // Sécurité pour éviter une boucle infinie
                if (counter > 99)
                {
                    code = $"FOUR{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
                    break;
                }
            }

            return code;
        }

        private void PrepareSupplierForCreation()
        {
            // Normaliser les données avant création
            Supplier.CodeFournisseur = Supplier.CodeFournisseur?.Trim().ToUpper();
            Supplier.RaisonSociale = Supplier.RaisonSociale?.Trim().ToUpper();
            Supplier.ContactPrincipal = Supplier.ContactPrincipal?.Trim();
            Supplier.Email = Supplier.Email?.Trim().ToLower();
            Supplier.Telephone = Supplier.Telephone?.Trim();
            Supplier.Fax = Supplier.Fax?.Trim();
            Supplier.SiteWeb = NormalizeSiteWeb(Supplier.SiteWeb);
            Supplier.Adresse = Supplier.Adresse?.Trim();
            Supplier.Ville = Supplier.Ville?.Trim();
            Supplier.Pays = Supplier.Pays?.Trim();
            Supplier.MatriculeFiscal = Supplier.MatriculeFiscal?.Trim().ToUpper();
            Supplier.NumeroTva = Supplier.NumeroTva?.Trim().ToUpper();
            Supplier.Siret = Supplier.Siret?.Trim();
            Supplier.CodeNaf = Supplier.CodeNaf?.Trim().ToUpper();
            Supplier.Iban = Supplier.Iban?.Trim().ToUpper();
            Supplier.Bic = Supplier.Bic?.Trim().ToUpper();

            // Dates système
            Supplier.DateCreation = DateTime.Now;
        }

        private string? NormalizeSiteWeb(string? siteWeb)
        {
            if (string.IsNullOrWhiteSpace(siteWeb))
                return null;

            var normalized = siteWeb.Trim().ToLower();

            // Ajouter http:// si aucun protocole n'est spécifié
            if (!normalized.StartsWith("http://") && !normalized.StartsWith("https://"))
            {
                normalized = "http://" + normalized;
            }

            return normalized;
        }

        private async Task ValidateSupplierData()
        {
            // Validation du code fournisseur
            if (string.IsNullOrWhiteSpace(Supplier.CodeFournisseur))
            {
                ModelState.AddModelError("Supplier.CodeFournisseur", "Le code fournisseur est obligatoire.");
            }
            else if (Supplier.CodeFournisseur.Length < 3)
            {
                ModelState.AddModelError("Supplier.CodeFournisseur", "Le code fournisseur doit contenir au moins 3 caractères.");
            }
            else if (Supplier.CodeFournisseur.Length > 20)
            {
                ModelState.AddModelError("Supplier.CodeFournisseur", "Le code fournisseur ne peut pas dépasser 20 caractères.");
            }

            // Validation de la raison sociale
            if (string.IsNullOrWhiteSpace(Supplier.RaisonSociale))
            {
                ModelState.AddModelError("Supplier.RaisonSociale", "La raison sociale est obligatoire.");
            }
            else if (Supplier.RaisonSociale.Length < 2)
            {
                ModelState.AddModelError("Supplier.RaisonSociale", "La raison sociale doit contenir au moins 2 caractères.");
            }

            // Validation de l'email
            if (!string.IsNullOrWhiteSpace(Supplier.Email))
            {
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(Supplier.Email))
                {
                    ModelState.AddModelError("Supplier.Email", "Le format de l'email n'est pas valide.");
                }
            }

            // Validation du code postal
            if (!string.IsNullOrWhiteSpace(Supplier.CodePostal))
            {
                var postalRegex = new System.Text.RegularExpressions.Regex(@"^\d{5}$");
                if (!postalRegex.IsMatch(Supplier.CodePostal))
                {
                    ModelState.AddModelError("Supplier.CodePostal", "Le code postal doit contenir exactement 5 chiffres.");
                }
            }

            // Validation du SIRET
            if (!string.IsNullOrWhiteSpace(Supplier.Siret))
            {
                var siretRegex = new System.Text.RegularExpressions.Regex(@"^\d{14}$");
                if (!siretRegex.IsMatch(Supplier.Siret))
                {
                    ModelState.AddModelError("Supplier.Siret", "Le numéro SIRET doit contenir exactement 14 chiffres.");
                }
            }

            // Validation des montants
            if (Supplier.RemiseNegociee < 0 || Supplier.RemiseNegociee > 100)
            {
                ModelState.AddModelError("Supplier.RemiseNegociee", "La remise négociée doit être comprise entre 0 et 100%.");
            }

            if (Supplier.PlafondCredit < 0)
            {
                ModelState.AddModelError("Supplier.PlafondCredit", "Le plafond de crédit ne peut pas être négatif.");
            }

            if (Supplier.DelaiLivraison < 0 || Supplier.DelaiLivraison > 365)
            {
                ModelState.AddModelError("Supplier.DelaiLivraison", "Le délai de livraison doit être compris entre 0 et 365 jours.");
            }

            await Task.CompletedTask;
        }

        private async Task<bool> CheckCodeSupplierExists(string codeFournisseur)
        {
            // Simulation - dans un vrai projet, ceci ferait appel à la base de données
            var existingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FOUR001", "FOUR002", "FOUR003", "FOURTEST", "FOURVIP001", "FOURDEV001"
            };

            await Task.Delay(100); // Simulation délai réseau
            return existingCodes.Contains(codeFournisseur?.Trim().ToUpper() ?? "");
        }

        private async Task<bool> CheckEmailExists(string email)
        {
            // Simulation - dans un vrai projet, ceci ferait appel à la base de données
            var existingEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "contact@techsupplies.fr",
                "commandes@bureauplus.com",
                "support@servicesmaintenance.fr",
                "info@exemple.com"
            };

            await Task.Delay(100); // Simulation délai réseau
            return existingEmails.Contains(email?.Trim().ToLower() ?? "");
        }

        private async Task<ExistingSupplierDto?> LoadExistingSupplierAsync(int supplierId)
        {
            // Simulation de chargement d'un fournisseur existant
            await Task.Delay(150);

            var simulatedSuppliers = new Dictionary<int, ExistingSupplierDto>
            {
                [1] = new ExistingSupplierDto
                {
                    Id = 1,
                    CodeFournisseur = "FOUR001",
                    RaisonSociale = "TECH SUPPLIES SARL",
                    ContactPrincipal = "Marie TECHNOLOGIE",
                    Email = "contact@techsupplies.fr",
                    Telephone = "01.23.45.67.89",
                    Fax = "01.23.45.67.90",
                    SiteWeb = "www.techsupplies.fr",
                    Adresse = "123 Avenue de l'Innovation",
                    CodePostal = "75001",
                    Ville = "Paris",
                    Pays = "France",
                    Zone = "Île-de-France",
                    TypeFournisseur = "Distributeur",
                    TypeCategorie = "Matériel Informatique",
                    ConditionPaiement = "30 jours fin de mois",
                    DelaiLivraison = 3,
                    RemiseNegociee = 12.5m,
                    PlafondCredit = 50000,
                    Banque = "BNP Paribas Entreprises",
                    CodeNaf = "4651Z"
                },
                [2] = new ExistingSupplierDto
                {
                    Id = 2,
                    CodeFournisseur = "FOUR002",
                    RaisonSociale = "FOURNITURES BUREAU PLUS",
                    ContactPrincipal = "Jean BUREAU",
                    Email = "commandes@bureauplus.com",
                    Telephone = "02.34.56.78.90",
                    Adresse = "456 Rue du Commerce",
                    CodePostal = "69001",
                    Ville = "Lyon",
                    Pays = "France",
                    Zone = "Rhône-Alpes",
                    TypeFournisseur = "Grossiste",
                    TypeCategorie = "Fournitures Bureau",
                    ConditionPaiement = "45 jours net",
                    DelaiLivraison = 2,
                    RemiseNegociee = 8.0m,
                    PlafondCredit = 25000
                }
            };

            return simulatedSuppliers.TryGetValue(supplierId, out var supplier) ? supplier : null;
        }

        private async Task<int> CreateSupplierSimulation(CreateSupplierViewModel supplier)
        {
            // Simulation de création en base de données
            await Task.Delay(300); // Simulation du temps de traitement

            var supplierId = new Random().Next(1000, 9999);

            _logger.LogInformation($"Création du fournisseur simulée:");
            _logger.LogInformation($"  - ID généré: {supplierId}");
            _logger.LogInformation($"  - Code: {supplier.CodeFournisseur}");
            _logger.LogInformation($"  - Raison sociale: {supplier.RaisonSociale}");
            _logger.LogInformation($"  - Email: {supplier.Email}");
            _logger.LogInformation($"  - Type: {supplier.TypeFournisseur}");
            _logger.LogInformation($"  - Catégorie: {supplier.TypeCategorie}");

            return supplierId;
        }

        #endregion

        #region API AJAX pour validation en temps réel

        public async Task<IActionResult> OnGetCheckCodeSupplierAsync(string codeSupplier)
        {
            if (string.IsNullOrWhiteSpace(codeSupplier))
                return new JsonResult(new { exists = false });

            var exists = await CheckCodeSupplierExists(codeSupplier);
            return new JsonResult(new { exists });
        }

        public async Task<IActionResult> OnGetCheckEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return new JsonResult(new { exists = false });

            // Validation format email côté serveur
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(email))
                return new JsonResult(new { exists = false, invalidFormat = true });

            var exists = await CheckEmailExists(email);
            return new JsonResult(new { exists });
        }

        public async Task<IActionResult> OnGetGetCitySuggestionsAsync(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode) || postalCode.Length != 5)
                return new JsonResult(new { suggestions = new string[0] });

            await Task.Delay(50); // Simulation appel API
            var suggestions = GetCitySuggestionsByPostalCode(postalCode);
            return new JsonResult(new { suggestions });
        }

        private string[] GetCitySuggestionsByPostalCode(string postalCode)
        {
            // Simulation d'un service de géolocalisation
            var cityMappings = new Dictionary<string, string[]>
            {
                // Paris et région parisienne
                { "75001", new[] { "Paris 1er Arrondissement" } },
                { "75002", new[] { "Paris 2ème Arrondissement" } },
                { "75003", new[] { "Paris 3ème Arrondissement" } },
                { "92100", new[] { "Boulogne-Billancourt" } },
                { "94300", new[] { "Vincennes" } },
                
                // Grandes villes
                { "69001", new[] { "Lyon 1er Arrondissement" } },
                { "69002", new[] { "Lyon 2ème Arrondissement" } },
                { "13001", new[] { "Marseille 1er Arrondissement" } },
                { "13002", new[] { "Marseille 2ème Arrondissement" } },
                { "33000", new[] { "Bordeaux" } },
                { "59000", new[] { "Lille" } },
                { "67000", new[] { "Strasbourg" } },
                { "31000", new[] { "Toulouse" } },
                { "06000", new[] { "Nice" } },
                
                // Autres villes
                { "44000", new[] { "Nantes" } },
                { "35000", new[] { "Rennes" } },
                { "21000", new[] { "Dijon" } },
                { "25000", new[] { "Besançon" } }
            };

            return cityMappings.TryGetValue(postalCode, out var cities) ? cities : new string[0];
        }

        #endregion
    }

    #region ViewModels et DTOs

    public class CreateSupplierViewModel
    {
        [Required(ErrorMessage = "Le code fournisseur est obligatoire")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Le code fournisseur doit contenir entre 3 et 20 caractères")]
        public string CodeFournisseur { get; set; } = string.Empty;

        [Required(ErrorMessage = "La raison sociale est obligatoire")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "La raison sociale doit contenir entre 2 et 200 caractères")]
        public string RaisonSociale { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Le nom du contact ne peut pas dépasser 100 caractères")]
        public string? ContactPrincipal { get; set; }

        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(255, ErrorMessage = "L'email ne peut pas dépasser 255 caractères")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Format de téléphone invalide")]
        [StringLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
        public string? Telephone { get; set; }

        [StringLength(20, ErrorMessage = "Le numéro de fax ne peut pas dépasser 20 caractères")]
        public string? Fax { get; set; }

        [Url(ErrorMessage = "Format d'URL invalide")]
        [StringLength(255, ErrorMessage = "L'URL ne peut pas dépasser 255 caractères")]
        public string? SiteWeb { get; set; }

        [StringLength(500, ErrorMessage = "L'adresse ne peut pas dépasser 500 caractères")]
        public string? Adresse { get; set; }

        [RegularExpression(@"^\d{5}$", ErrorMessage = "Le code postal doit contenir exactement 5 chiffres")]
        public string? CodePostal { get; set; }

        [StringLength(100, ErrorMessage = "Le nom de la ville ne peut pas dépasser 100 caractères")]
        public string? Ville { get; set; }

        [StringLength(100, ErrorMessage = "Le nom du pays ne peut pas dépasser 100 caractères")]
        public string? Pays { get; set; }

        [StringLength(50, ErrorMessage = "La zone ne peut pas dépasser 50 caractères")]
        public string? Zone { get; set; }

        [StringLength(50, ErrorMessage = "Le type de fournisseur ne peut pas dépasser 50 caractères")]
        public string? TypeFournisseur { get; set; }

        [StringLength(50, ErrorMessage = "La catégorie ne peut pas dépasser 50 caractères")]
        public string? TypeCategorie { get; set; }

        [StringLength(50, ErrorMessage = "La condition de paiement ne peut pas dépasser 50 caractères")]
        public string? ConditionPaiement { get; set; }

        [Range(0, 365, ErrorMessage = "Le délai de livraison doit être entre 0 et 365 jours")]
        public int? DelaiLivraison { get; set; }

        [Range(0, 100, ErrorMessage = "La remise négociée doit être entre 0 et 100%")]
        public decimal? RemiseNegociee { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Le plafond de crédit ne peut pas être négatif")]
        public decimal? PlafondCredit { get; set; }

        public decimal? SoldeActuel { get; set; }

        [StringLength(50, ErrorMessage = "Le matricule fiscal ne peut pas dépasser 50 caractères")]
        public string? MatriculeFiscal { get; set; }

        [StringLength(50, ErrorMessage = "Le numéro TVA ne peut pas dépasser 50 caractères")]
        public string? NumeroTva { get; set; }

        [RegularExpression(@"^\d{14}$", ErrorMessage = "Le numéro SIRET doit contenir exactement 14 chiffres")]
        public string? Siret { get; set; }

        [StringLength(10, ErrorMessage = "Le code NAF ne peut pas dépasser 10 caractères")]
        public string? CodeNaf { get; set; }

        [StringLength(100, ErrorMessage = "Le nom de la banque ne peut pas dépasser 100 caractères")]
        public string? Banque { get; set; }

        [StringLength(50, ErrorMessage = "L'IBAN ne peut pas dépasser 50 caractères")]
        public string? Iban { get; set; }

        [StringLength(20, ErrorMessage = "Le BIC ne peut pas dépasser 20 caractères")]
        public string? Bic { get; set; }

        [StringLength(2000, ErrorMessage = "Les commentaires ne peuvent pas dépasser 2000 caractères")]
        public string? Commentaires { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }

    public class ExistingSupplierDto
    {
        public int Id { get; set; }
        public string CodeFournisseur { get; set; } = string.Empty;
        public string RaisonSociale { get; set; } = string.Empty;
        public string? ContactPrincipal { get; set; }
        public string? Email { get; set; }
        public string? Telephone { get; set; }
        public string? Fax { get; set; }
        public string? SiteWeb { get; set; }
        public string? Adresse { get; set; }
        public string? CodePostal { get; set; }
        public string? Ville { get; set; }
        public string? Pays { get; set; }
        public string? Zone { get; set; }
        public string? TypeFournisseur { get; set; }
        public string? TypeCategorie { get; set; }
        public string? ConditionPaiement { get; set; }
        public int? DelaiLivraison { get; set; }
        public decimal? RemiseNegociee { get; set; }
        public decimal? PlafondCredit { get; set; }
        public string? MatriculeFiscal { get; set; }
        public string? NumeroTva { get; set; }
        public string? Siret { get; set; }
        public string? CodeNaf { get; set; }
        public string? Banque { get; set; }
        public string? Iban { get; set; }
        public string? Bic { get; set; }
    }

    #endregion
}