using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GestionCommerciale.ViewModel;

namespace GestionCommerciale.Pages.Clients
{
    public class ClientDetailsModel : PageModel
    {
        [BindProperty]
        public ClientDetailsViewModel Client { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Mode { get; set; } = "view"; // view, edit, create

        public string PageTitle => Mode switch
        {
            "create" => "Nouveau Client",
            "edit" => "Modifier le Client",
            _ => "Détails du Client"
        };

        public string FormAction => Mode switch
        {
            "create" => "OnPostCreateAsync",
            "edit" => "OnPostUpdateAsync",
            _ => ""
        };

        public async Task<IActionResult> OnGetAsync()
        {
            // Déterminer le mode basé sur les paramètres
            if (Id == null && Mode == "create")
            {
                Client = new ClientDetailsViewModel
                {
                    IsEditMode = true,
                    DateCreation = DateTime.Now
                };
                return Page();
            }

            if (Id == null && Mode != "create")
            {
                // Si aucun ID et mode n'est pas "create", rediriger vers la liste
                return RedirectToPage("./Index");
            }

            if (Id.HasValue)
            {
                // Charger le client existant
                Client = await LoadClientAsync(Id.Value);

                if (Client == null)
                {
                    return NotFound();
                }

                Client.IsEditMode = Mode == "edit";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                Client.IsEditMode = true;
                return Page();
            }

            try
            {
                // Logique de création en base de données
                await CreateClientAsync(Client);

                TempData["SuccessMessage"] = "Le client a été créé avec succès.";
                return RedirectToPage("./ClientDetails", new { id = Client.Id, mode = "view" });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Erreur lors de la création du client: " + ex.Message);
                Client.IsEditMode = true;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                Client.IsEditMode = true;
                return Page();
            }

            try
            {
                // Logique de mise à jour en base de données
                await UpdateClientAsync(Client);

                TempData["SuccessMessage"] = "Le client a été modifié avec succès.";
                return RedirectToPage("./ClientDetails", new { id = Client.Id, mode = "view" });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Erreur lors de la modification du client: " + ex.Message);
                Client.IsEditMode = true;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            if (Id == null)
            {
                return NotFound();
            }

            try
            {
                // Logique de suppression en base de données
                await DeleteClientAsync(Id.Value);

                TempData["SuccessMessage"] = "Le client a été supprimé avec succès.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Erreur lors de la suppression du client: " + ex.Message;
                return RedirectToPage("./ClientDetails", new { id = Id, mode = "view" });
            }
        }

        // Méthodes de simulation - À remplacer par votre logique de données réelle
        private async Task<ClientDetailsViewModel?> LoadClientAsync(int id)
        {
            // Simulation - remplacez par votre logique de chargement depuis la base
            await Task.Delay(1);

            if (id == 1) // Client d'exemple
            {
                return new ClientDetailsViewModel
                {
                    Id = 1,
                    CodeClient = "CLI001",
                    Nom = "DUPONT",
                    Prenom = "Jean",
                    Adresse = "123 Rue de la Paix",
                    CodePostal = "75001",
                    Ville = "Paris",
                    Telephone = "01.23.45.67.89",
                    Email = "jean.dupont@email.com",
                    Zone = "Nord",
                    RemiseParDefaut = 5.0m,
                    TypeCategorie = "VIP",
                    Representant = "Marie Martin",
                    DateCreation = DateTime.Now.AddMonths(-6),
                    DateModification = DateTime.Now.AddDays(-10)
                };
            }

            return null;
        }

        private async Task CreateClientAsync(ClientDetailsViewModel client)
        {
            // Simulation - remplacez par votre logique de création en base
            await Task.Delay(100);
            client.Id = new Random().Next(1000, 9999);
            client.DateCreation = DateTime.Now;
        }

        private async Task UpdateClientAsync(ClientDetailsViewModel client)
        {
            // Simulation - remplacez par votre logique de mise à jour en base
            await Task.Delay(100);
            client.DateModification = DateTime.Now;
        }

        private async Task DeleteClientAsync(int id)
        {
            // Simulation - remplacez par votre logique de suppression en base
            await Task.Delay(100);
        }
    }
}