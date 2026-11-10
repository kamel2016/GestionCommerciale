using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Models
{
    public class ParametresSociete
    {
        public int ID { get; set; }

        // ============================================
        // IDENTITÉ DE LA SOCIÉTÉ
        // ============================================

        [Required, StringLength(150)]
        [Display(Name = "Raison sociale")]
        public string RaisonSociale { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Matricule fiscale")]
        public string? MatriculeFiscale { get; set; }

        [StringLength(50)]
        [Display(Name = "Registre de commerce")]
        public string? RegistreCommerce { get; set; }

        [StringLength(250)]
        [Display(Name = "Adresse")]
        public string? Adresse { get; set; }

        [StringLength(20)]
        [Display(Name = "Téléphone")]
        public string? Telephone { get; set; }

        [EmailAddress, StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(250)]
        [Display(Name = "Logo (chemin)")]
        public string? LogoPath { get; set; }

        // ============================================
        // PARAMÈTRES FISCAUX TUNISIE
        // ============================================

        [Display(Name = "Taux TVA par défaut (%)")]
        public decimal TauxTVADefaut { get; set; } = 19;

        [Display(Name = "Appliquer Fodec par défaut")]
        public bool FodecDefaut { get; set; } = false;

        [Display(Name = "Taux Fodec (%)")]
        public decimal TauxFodec { get; set; } = 1m;

        [Display(Name = "Taux retenue à la source (%)")]
        public decimal TauxRetenueSource { get; set; } = 1m;

        [Display(Name = "Timbre fiscal (DT)")]
        public decimal TimbreFiscal { get; set; } = 1.000m;

        [Display(Name = "Appliquer la retenue à la source")]
        public bool RetenueActive { get; set; } = false;
    }
}