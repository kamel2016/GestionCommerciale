using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Models
{
    public class ParametresSociete
    {
        [Key]
        public int Id { get; set; }

        // ============================================
        // IDENTITÉ SOCIÉTÉ
        // ============================================

        [Required]
        [StringLength(200)]
        [Display(Name = "Raison sociale")]
        public string RaisonSociale { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Matricule Fiscale")]
        public string? MatriculeFiscale { get; set; }

        [StringLength(50)]
        [Display(Name = "Registre de commerce")]
        public string? RegistreCommerce { get; set; }

        // ============================================
        // CONTACT
        // ============================================

        [StringLength(500)]
        [Display(Name = "Adresse")]
        public string? Adresse { get; set; }

        [StringLength(100)]
        [Display(Name = "Ville")]
        public string? Ville { get; set; }

        [StringLength(10)]
        [Display(Name = "Code Postal")]
        public string? CodePostal { get; set; }

        [StringLength(50)]
        [Display(Name = "Gouvernorat")]
        public string? Gouvernorat { get; set; }

        [StringLength(20)]
        [Display(Name = "Téléphone")]
        public string? Telephone { get; set; }

        [StringLength(20)]
        [Display(Name = "Fax")]
        public string? Fax { get; set; }

        [EmailAddress]
        [StringLength(255)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(100)]
        [Display(Name = "Site web")]
        public string? SiteWeb { get; set; }

        // ============================================
        // PARAMÈTRES FISCAUX TUNISIE
        // ============================================

        [Display(Name = "Taux TVA par défaut (%)")]
        public decimal TauxTVADefaut { get; set; } = 19;

        [Display(Name = "Appliquer Fodec par défaut")]
        public bool FodecDefaut { get; set; } = false;

        [Display(Name = "Timbre fiscal (DT)")]
        public decimal TimbreFiscal { get; set; } = 1.000m;

        // ============================================
        // PARAMÈTRES DOCUMENTS
        // ============================================

        [StringLength(20)]
        [Display(Name = "Préfixe Devis")]
        public string PrefixeDevis { get; set; } = "DEV";

        [StringLength(20)]
        [Display(Name = "Préfixe Bon de commande")]
        public string PrefixeBonCommande { get; set; } = "BC";

        [StringLength(20)]
        [Display(Name = "Préfixe Bon de livraison")]
        public string PrefixeBonLivraison { get; set; } = "BL";

        [StringLength(20)]
        [Display(Name = "Préfixe Facture")]
        public string PrefixeFacture { get; set; } = "FAC";

        [Display(Name = "Prochain numéro devis")]
        public int ProchainNumeroDevis { get; set; } = 1;

        [Display(Name = "Prochain numéro BC")]
        public int ProchainNumeroBonCommande { get; set; } = 1;

        [Display(Name = "Prochain numéro BL")]
        public int ProchainNumeroBonLivraison { get; set; } = 1;

        [Display(Name = "Prochain numéro facture")]
        public int ProchainNumeroFacture { get; set; } = 1;

        // ============================================
        // LOGO
        // ============================================

        [StringLength(500)]
        [Display(Name = "Chemin du logo")]
        public string? LogoPath { get; set; }
    }
}