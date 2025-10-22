using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Pages.Suppliers
{
    public class DetailsModel : PageModel
    {
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ILogger<DetailsModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public SupplierDetailsViewModel Supplier { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Mode { get; set; } = "view"; // view, edit, create

        [BindProperty(SupportsGet = true)]
        public int? DuplicateId { get; set; }

        // Listes pour les dropdowns
        public List<SelectListItem> Zones { get; set; } = new();
        public List<SelectListItem> TypesCategorie { get; set; } = new();
        public List<SelectListItem> TypesFournisseur { get; set; } = new();
        public List<SelectListItem> ConditionsPaiement { get; set; } = new();

        public string PageTitle => Mode switch
        {
            "create" => "Nouveau Fournisseur",
            "edit" => "Modifier le Fournisseur",
            _ => "Détails du Fournisseur"
        };

        public string FormAction => Mode switch
        {
            "create" => "OnPostCreateAsync",
            "edit" => "OnPostUpdateAsync",
            _ => ""
        };

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await InitializeSelectLists();

                if (Id == null && Mode == "create")
                {
                    await InitializeNewSupplier();
                    return Page();
                }

                if (Id == null && Mode != "create")
                {
                    return RedirectToPage("./Index");
                }

                if (Id.HasValue)
                {
                    Supplier = await LoadSupplierAsync(Id.Value);
                    if (Supplier == null)
                    {
                        TempData["ErrorMessage"] = "Fournisseur introuvable.";
                        return RedirectToPage("./Index");
                    }
                    Supplier.IsEditMode = Mode == "edit";
                }

                if (DuplicateId.HasValue && Mode == "create")
                {
                    await LoadSupplierForDuplication(DuplicateId.Value);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la page fournisseur");
                TempData["ErrorMessage"] = "Erreur lors du chargement de la page.";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                await InitializeSelectLists();
                await ValidateSupplierData();

                if (!ModelState.IsValid)
                {
                    Supplier.IsEditMode = true;
                    return Page();
                }

                if (await CheckCodeSupplierExists(Supplier.CodeFournisseur))
                {
                    ModelState.AddModelError("Supplier.CodeFournisseur", "Ce code fournisseur existe déjà.");
                    Supplier.IsEditMode = true;
                    return Page();
                }

                if (!string.IsNullOrEmpty(Supplier.Email) && await CheckEmailExists(Supplier.Email))
                {
                    ModelState.AddModelError("Supplier.Email", "Cette adresse email est déjà utilisée.");
                    Supplier.IsEditMode = true;
                    return Page();
                }

                PrepareSupplierForCreation();
                var supplierId = await CreateSupplierSimulation(Supplier);

                _logger.LogInformation($"Nouveau fournisseur créé avec l'ID {supplierId}");
                TempData["SuccessMessage"] = $"Le fournisseur {Supplier.RaisonSociale} a été créé avec succès.";
                return RedirectToPage("./SupplierDetails", new { id = supplierId, mode = "view" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du fournisseur");
                ModelState.AddModelError("", "Erreur lors de la création du fournisseur: " + ex.Message);
                Supplier.IsEditMode = true;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            try
            {
                await InitializeSelectLists();

                var originalSupplier = await LoadSupplierAsync(Id.Value);
                if (originalSupplier == null)
                {
                    TempData["ErrorMessage"] = "Fournisseur introuvable.";
                    return RedirectToPage("./Index");
                }

                await ValidateSupplierData();

                if (!ModelState.IsValid)
                {
                    Supplier.IsEditMode = true;
                    return Page();
                }

                if (Supplier.CodeFournisseur != originalSupplier.CodeFournisseur &&
                    await CheckCodeSupplierExists(Supplier.CodeFournisseur))
                {
                    ModelState.AddModelError("Supplier.CodeFournisseur", "Ce code fournisseur existe déjà.");
                    Supplier.IsEditMode = true;
                    return Page();
                }

                if (!string.IsNullOrEmpty(Supplier.Email) &&
                    Supplier.Email != originalSupplier.Email &&
                    await CheckEmailExists(Supplier.Email))
                {
                    ModelState.AddModelError("Supplier.Email", "Cette adresse email est déjà utilisée.");
                    Supplier.IsEditMode = true;
                    return Page();
                }

                PrepareSupplierForUpdate();
                var changes = DetectChanges(originalSupplier, Supplier);
                await UpdateSupplierSimulation(Supplier, changes);

                _logger.LogInformation($"Fournisseur {Id} modifié avec {changes.Count} changement(s)");
                TempData["SuccessMessage"] = $"Le fournisseur {Supplier.RaisonSociale} a été modifié avec succès.";
                return RedirectToPage("./SupplierDetails", new { id = Id, mode = "view" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du fournisseur");
                ModelState.AddModelError("", "Erreur lors de la modification du fournisseur: " + ex.Message);
                Supplier.IsEditMode = true;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (Id == null) return NotFound();

            try
            {
                var supplier = await LoadSupplierAsync(Id.Value);
                if (supplier == null)
                {
                    TempData["ErrorMessage"] = "Fournisseur introuvable.";
                    return RedirectToPage("./Index");
                }

                await DeleteSupplierSimulation(Id.Value);
                _logger.LogInformation($"Fournisseur {Id} ({supplier.RaisonSociale}) supprimé");
                TempData["SuccessMessage"] = $"Le fournisseur {supplier.RaisonSociale} a été supprimé avec succès.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression du fournisseur {Id}");
                TempData["ErrorMessage"] = "Erreur lors de la suppression du fournisseur: " + ex.Message;
                return RedirectToPage("./SupplierDetails", new { id = Id, mode = "view" });
            }
        }

        // Méthodes de simulation
        private async Task<SupplierDetailsViewModel?> LoadSupplierAsync(int supplierId)
        {
            await Task.Delay(150);

            var simulatedSuppliers = new Dictionary<int, SupplierDetailsViewModel>
            {
                [1] = new SupplierDetailsViewModel
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
                    SoldeActuel = 15000.75m,
                    MatriculeFiscal = "MF123456789",
                    NumeroTva = "FR12345678901",
                    Siret = "12345678901234",
                    CodeNaf = "4651Z",
                    Banque = "BNP Paribas Entreprises",
                    Iban = "FR7630001007941234567890185",
                    Bic = "BNPAFRPPXXX",
                    Commentaires = "Fournisseur principal pour le matériel informatique.\nExcellente réactivité et qualité de service.",
                    DateCreation = DateTime.Now.AddMonths(-12),
                    DateModification = DateTime.Now.AddDays(-3),
                    IsActive = true
                },
                [2] = new SupplierDetailsViewModel
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
                    PlafondCredit = 25000,
                    SoldeActuel = 5500.30m,
                    DateCreation = DateTime.Now.AddMonths(-8),
                    DateModification = DateTime.Now.AddDays(-1),
                    IsActive = true
                },
                [3] = new SupplierDetailsViewModel
                {
                    Id = 3,
                    CodeFournisseur = "FOUR003",
                    RaisonSociale = "SERVICES MAINTENANCE SA",
                    ContactPrincipal = "Sophie MAINTENANCE",
                    Email = "support@servicesmaintenance.fr",
                    Telephone = "03.45.67.89.01",
                    Adresse = "789 Boulevard Industriel",
                    CodePostal = "13001",
                    Ville = "Marseille",
                    Pays = "France",
                    Zone = "PACA",
                    TypeFournisseur = "Prestataire de services",
                    TypeCategorie = "Maintenance",
                    ConditionPaiement = "60 jours fin de mois",
                    DelaiLivraison = 1,
                    RemiseNegociee = 5.0m,
                    PlafondCredit = 15000,
                    SoldeActuel = -2500.00m,
                    DateCreation = DateTime.Now.AddMonths(-6),
                    IsActive = false
                }
            };

            return simulatedSuppliers.TryGetValue(supplierId, out var supplier) ? supplier : null;
        }

        private async Task ValidateSupplierData()
        {
            if (string.IsNullOrWhiteSpace(Supplier.CodeFournisseur))
                ModelState.AddModelError("Supplier.CodeFournisseur", "Le code fournisseur est obligatoire.");
            else if (Supplier.CodeFournisseur.Length < 3)
                ModelState.AddModelError("Supplier.CodeFournisseur", "Le code fournisseur doit contenir au moins 3 caractères.");

            if (string.IsNullOrWhiteSpace(Supplier.RaisonSociale))
                ModelState.AddModelError("Supplier.RaisonSociale", "La raison sociale est obligatoire.");

            if (!string.IsNullOrWhiteSpace(Supplier.Email))
            {
                var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!emailRegex.IsMatch(Supplier.Email))
                    ModelState.AddModelError("Supplier.Email", "Le format de l'email n'est pas valide.");
            }

            if (!string.IsNullOrWhiteSpace(Supplier.CodePostal))
            {
                var postalRegex = new System.Text.RegularExpressions.Regex(@"^\d{5}$");
                if (!postalRegex.IsMatch(Supplier.CodePostal))
                    ModelState.AddModelError("Supplier.CodePostal", "Le code postal doit contenir 5 chiffres.");
            }

            await Task.CompletedTask;
        }

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
                new() { Text = "International", Value = "International" }
            };

            TypesCategorie = new List<SelectListItem>
            {
                new() { Text = "Sélectionner...", Value = "" },
                new() { Text = "Matériel Informatique", Value = "Matériel Informatique" },
                new() { Text = "Fournitures Bureau", Value = "Fournitures Bureau" },
                new() { Text = "Maintenance", Value = "Maintenance" },
                new() { Text = "Consommables", Value = "Consommables" },
                new() { Text = "Mobilier", Value = "Mobilier" },
                new() { Text = "Services", Value = "Services" }
            };

            TypesFournisseur = new List<SelectListItem>
            {
                new() { Text = "Sélectionner...", Value = "" },
                new() { Text = "Fabricant", Value = "Fabricant" },
                new() { Text = "Distributeur", Value = "Distributeur" },
                new() { Text = "Grossiste", Value = "Grossiste" },
                new() { Text = "Détaillant", Value = "Détaillant" },
                new() { Text = "Prestataire de services", Value = "Prestataire de services" },
                new() { Text = "Sous-traitant", Value = "Sous-traitant" }
            };

            ConditionsPaiement = new List<SelectListItem>
            {
                new() { Text = "Sélectionner...", Value = "" },
                new() { Text = "Comptant", Value = "Comptant" },
                new() { Text = "30 jours net", Value = "30 jours net" },
                new() { Text = "30 jours fin de mois", Value = "30 jours fin de mois" },
                new() { Text = "45 jours net", Value = "45 jours net" },
                new() { Text = "60 jours net", Value = "60 jours net" },
                new() { Text = "60 jours fin de mois", Value = "60 jours fin de mois" },
                new() { Text = "90 jours", Value = "90 jours" }
            };

            await Task.CompletedTask;
        }

        private async Task InitializeNewSupplier()
        {
            Supplier = new SupplierDetailsViewModel
            {
                IsEditMode = true,
                DateCreation = DateTime.Now,
                DelaiLivraison = 7,
                RemiseNegociee = 0,
                PlafondCredit = 0,
                SoldeActuel = 0,
                Pays = "France",
                IsActive = true
            };
            Supplier.CodeFournisseur = GenerateSupplierCode();
            await Task.CompletedTask;
        }

        private async Task LoadSupplierForDuplication(int supplierId)
        {
            var sourceSupplier = await LoadSupplierAsync(supplierId);
            if (sourceSupplier != null)
            {
                Supplier = new SupplierDetailsViewModel
                {
                    RaisonSociale = sourceSupplier.RaisonSociale + " (Copie)",
                    ContactPrincipal = sourceSupplier.ContactPrincipal,
                    Email = "",
                    Telephone = sourceSupplier.Telephone,
                    Fax = sourceSupplier.Fax,
                    Adresse = sourceSupplier.Adresse,
                    CodePostal = sourceSupplier.CodePostal,
                    Ville = sourceSupplier.Ville,
                    Pays = sourceSupplier.Pays,
                    Zone = sourceSupplier.Zone,
                    TypeFournisseur = sourceSupplier.TypeFournisseur,
                    TypeCategorie = sourceSupplier.TypeCategorie,
                    ConditionPaiement = sourceSupplier.ConditionPaiement,
                    DelaiLivraison = sourceSupplier.DelaiLivraison,
                    RemiseNegociee = sourceSupplier.RemiseNegociee,
                    PlafondCredit = sourceSupplier.PlafondCredit,
                    SoldeActuel = 0,
                    Banque = sourceSupplier.Banque,
                    DateCreation = DateTime.Now,
                    IsEditMode = true,
                    IsActive = true
                };
                Supplier.CodeFournisseur = GenerateSupplierCode();
                TempData["InfoMessage"] = "Fournisseur dupliqué. Veuillez modifier les informations nécessaires.";
            }
            else
            {
                await InitializeNewSupplier();
                TempData["WarningMessage"] = "Fournisseur introuvable pour la duplication.";
            }
        }

        private string GenerateSupplierCode()
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmm");
            return $"FOUR{timestamp}";
        }

        private void PrepareSupplierForCreation()
        {
            Supplier.CodeFournisseur = Supplier.CodeFournisseur?.Trim().ToUpper();
            Supplier.RaisonSociale = Supplier.RaisonSociale?.Trim().ToUpper();
            Supplier.Email = Supplier.Email?.Trim().ToLower();
            Supplier.DateCreation = DateTime.Now;
        }

        private void PrepareSupplierForUpdate()
        {
            Supplier.CodeFournisseur = Supplier.CodeFournisseur?.Trim().ToUpper();
            Supplier.RaisonSociale = Supplier.RaisonSociale?.Trim().ToUpper();
            Supplier.Email = Supplier.Email?.Trim().ToLower();
            Supplier.DateModification = DateTime.Now;
        }

        private async Task<bool> CheckCodeSupplierExists(string codeFournisseur)
        {
            var existingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "FOUR001", "FOUR002", "FOUR003", "FOURTEST", "FOURVIP001"
            };
            await Task.Delay(100);
            return existingCodes.Contains(codeFournisseur?.Trim().ToUpper() ?? "");
        }

        private async Task<bool> CheckEmailExists(string email)
        {
            var existingEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "contact@techsupplies.fr",
                "commandes@bureauplus.com",
                "support@servicesmaintenance.fr"
            };
            await Task.Delay(100);
            return existingEmails.Contains(email?.Trim().ToLower() ?? "");
        }

        private async Task<int> CreateSupplierSimulation(SupplierDetailsViewModel supplier)
        {
            await Task.Delay(200);
            var supplierId = new Random().Next(1000, 9999);
            _logger.LogInformation($"Fournisseur créé - ID: {supplierId}, Code: {supplier.CodeFournisseur}");
            return supplierId;
        }

        private async Task UpdateSupplierSimulation(SupplierDetailsViewModel supplier, Dictionary<string, (object?, object?)> changes)
        {
            await Task.Delay(200);
            foreach (var change in changes)
            {
                _logger.LogInformation($"Champ {change.Key}: '{change.Value.Item1}' -> '{change.Value.Item2}'");
            }
        }

        private async Task DeleteSupplierSimulation(int supplierId)
        {
            await Task.Delay(150);
            _logger.LogInformation($"Fournisseur {supplierId} marqué comme supprimé");
        }

        private Dictionary<string, (object?, object?)> DetectChanges(SupplierDetailsViewModel original, SupplierDetailsViewModel modified)
        {
            var changes = new Dictionary<string, (object?, object?)>();

            if (original.CodeFournisseur != modified.CodeFournisseur)
                changes["CodeFournisseur"] = (original.CodeFournisseur, modified.CodeFournisseur);
            if (original.RaisonSociale != modified.RaisonSociale)
                changes["RaisonSociale"] = (original.RaisonSociale, modified.RaisonSociale);
            if (original.Email != modified.Email)
                changes["Email"] = (original.Email, modified.Email);

            return changes;
        }

        // APIs AJAX
        public async Task<IActionResult> OnGetCheckCodeSupplierAsync(string codeSupplier, int? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(codeSupplier))
                return new JsonResult(new { exists = false });

            if (currentId.HasValue)
            {
                var currentSupplier = await LoadSupplierAsync(currentId.Value);
                if (currentSupplier != null &&
                    currentSupplier.CodeFournisseur.Equals(codeSupplier, StringComparison.OrdinalIgnoreCase))
                    return new JsonResult(new { exists = false });
            }

            var exists = await CheckCodeSupplierExists(codeSupplier);
            return new JsonResult(new { exists });
        }

        public async Task<IActionResult> OnGetCheckEmailAsync(string email, int? currentId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return new JsonResult(new { exists = false });

            if (currentId.HasValue)
            {
                var currentSupplier = await LoadSupplierAsync(currentId.Value);
                if (currentSupplier != null &&
                    currentSupplier.Email?.Equals(email, StringComparison.OrdinalIgnoreCase) == true)
                    return new JsonResult(new { exists = false });
            }

            var exists = await CheckEmailExists(email);
            return new JsonResult(new { exists });
        }

        public async Task<IActionResult> OnGetGetCitySuggestionsAsync(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode) || postalCode.Length != 5)
                return new JsonResult(new { suggestions = new string[0] });

            await Task.Delay(50);
            var suggestions = GetCitySuggestionsByPostalCode(postalCode);
            return new JsonResult(new { suggestions });
        }

        private string[] GetCitySuggestionsByPostalCode(string postalCode)
        {
            var cityMappings = new Dictionary<string, string[]>
            {
                { "75001", new[] { "Paris 1er Arrondissement" } },
                { "69001", new[] { "Lyon 1er Arrondissement" } },
                { "13001", new[] { "Marseille 1er Arrondissement" } },
                { "33000", new[] { "Bordeaux" } },
                { "59000", new[] { "Lille" } },
                { "67000", new[] { "Strasbourg" } }
            };
            return cityMappings.TryGetValue(postalCode, out var cities) ? cities : new string[0];
        }
    }

    // ViewModel
    public class SupplierDetailsViewModel
    {
        public int Id { get; set; }
        public bool IsEditMode { get; set; }

        [Required(ErrorMessage = "Le code fournisseur est obligatoire")]
        [StringLength(20, MinimumLength = 3)]
        public string CodeFournisseur { get; set; } = string.Empty;

        [Required(ErrorMessage = "La raison sociale est obligatoire")]
        [StringLength(200)]
        public string RaisonSociale { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactPrincipal { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string? Telephone { get; set; }

        [StringLength(20)]
        public string? Fax { get; set; }

        [Url]
        [StringLength(255)]
        public string? SiteWeb { get; set; }

        [StringLength(500)]
        public string? Adresse { get; set; }

        [RegularExpression(@"^\d{5}$")]
        public string? CodePostal { get; set; }

        [StringLength(100)]
        public string? Ville { get; set; }

        [StringLength(100)]
        public string? Pays { get; set; }

        [StringLength(50)]
        public string? Zone { get; set; }

        [StringLength(50)]
        public string? TypeFournisseur { get; set; }

        [StringLength(50)]
        public string? TypeCategorie { get; set; }

        [StringLength(50)]
        public string? ConditionPaiement { get; set; }

        [Range(0, int.MaxValue)]
        public int? DelaiLivraison { get; set; }

        [Range(0, 100)]
        public decimal? RemiseNegociee { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PlafondCredit { get; set; }

        public decimal? SoldeActuel { get; set; }

        [StringLength(50)]
        public string? MatriculeFiscal { get; set; }

        [StringLength(50)]
        public string? NumeroTva { get; set; }

        [StringLength(20)]
        public string? Siret { get; set; }

        [StringLength(10)]
        public string? CodeNaf { get; set; }

        [StringLength(100)]
        public string? Banque { get; set; }

        [StringLength(50)]
        public string? Iban { get; set; }

        [StringLength(20)]
        public string? Bic { get; set; }

        [StringLength(2000)]
        public string? Commentaires { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime? DateModification { get; set; }
        public bool IsActive { get; set; } = true;
    }
}