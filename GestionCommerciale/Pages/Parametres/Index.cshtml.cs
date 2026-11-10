using GestionCommerciale.Data;
using GestionCommerciale.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Pages.Parametres
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public IndexModel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [BindProperty]
        public ParametresSociete Parametres { get; set; } = new();

        [BindProperty]
        public IFormFile? LogoFile { get; set; }

        [BindProperty]
        public bool SupprimerLogo { get; set; }

        [TempData]
        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            Parametres = await _db.ParametresSociete.AsNoTracking().FirstOrDefaultAsync()
                         ?? new ParametresSociete();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // LogoPath n'est pas saisi dans le formulaire : on l'ignore à la validation
            ModelState.Remove("Parametres.LogoPath");

            if (!ModelState.IsValid) return Page();

            var existant = await _db.ParametresSociete.FirstOrDefaultAsync();
            if (existant == null)
            {
                existant = new ParametresSociete();
                _db.ParametresSociete.Add(existant);
            }

            existant.RaisonSociale = Parametres.RaisonSociale;
            existant.MatriculeFiscale = Parametres.MatriculeFiscale;
            existant.RegistreCommerce = Parametres.RegistreCommerce;
            existant.Adresse = Parametres.Adresse;
            existant.Telephone = Parametres.Telephone;
            existant.Email = Parametres.Email;
            existant.TauxTVADefaut = Parametres.TauxTVADefaut;
            existant.FodecDefaut = Parametres.FodecDefaut;
            existant.TauxFodec = Parametres.TauxFodec;
            existant.TauxRetenueSource = Parametres.TauxRetenueSource;
            existant.TimbreFiscal = Parametres.TimbreFiscal;
            existant.RetenueActive = Parametres.RetenueActive;

            // Gestion du logo
            if (SupprimerLogo && !string.IsNullOrEmpty(existant.LogoPath))
            {
                SupprimerFichierLogo(existant.LogoPath);
                existant.LogoPath = null;
            }

            if (LogoFile != null && LogoFile.Length > 0)
            {
                var ext = Path.GetExtension(LogoFile.FileName).ToLowerInvariant();
                var autorises = new[] { ".png", ".jpg", ".jpeg" };

                if (!autorises.Contains(ext))
                {
                    ModelState.AddModelError("LogoFile", "Format non supporté (PNG ou JPG uniquement).");
                    return Page();
                }
                if (LogoFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("LogoFile", "Le logo ne doit pas dépasser 2 Mo.");
                    return Page();
                }

                var dossier = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(dossier);

                // Supprimer l'ancien logo
                if (!string.IsNullOrEmpty(existant.LogoPath))
                    SupprimerFichierLogo(existant.LogoPath);

                var nomFichier = $"logo_{Guid.NewGuid():N}{ext}";
                var chemin = Path.Combine(dossier, nomFichier);
                using (var fs = new FileStream(chemin, FileMode.Create))
                {
                    await LogoFile.CopyToAsync(fs);
                }
                existant.LogoPath = $"/uploads/{nomFichier}";
            }

            await _db.SaveChangesAsync();
            Message = "Paramètres enregistrés avec succès.";
            return RedirectToPage();
        }

        private void SupprimerFichierLogo(string logoPath)
        {
            var physique = Path.Combine(_env.WebRootPath, logoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physique))
                System.IO.File.Delete(physique);
        }
    }
}