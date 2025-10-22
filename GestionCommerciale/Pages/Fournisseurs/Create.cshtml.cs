using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestionCommerciale.Pages.Fournisseurs
{
    public class CreateModel : PageModel
    {
        //private readonly ApplicationDbContext _context;

        //public CreateModel(ApplicationDbContext context)
        //{
        //    _context = context;
        //}

        public CreateModel()
        {
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Client Client { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Vérifier si l'email existe déjà
            //if (await _context.Clients.AnyAsync(c => c.Email == Client.Email))
            //{
            //    ModelState.AddModelError("Client.Email", "Un client avec cet email existe déjà.");
            //    return Page();
            //}

            //_context.Clients.Add(Client);
            //await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}