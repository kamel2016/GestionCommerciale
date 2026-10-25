using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCommerciale.Models
{
    public class Article
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "La référence est obligatoire")]
        [StringLength(50)]
        [Display(Name = "Référence")]
        public string Reference { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le libellé est obligatoire")]
        [StringLength(200)]
        [Display(Name = "Libellé")]
        public string Libelle { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Code Barre")]
        public string? CodeBarre { get; set; }

        // ============================================
        // CATÉGORIE
        // ============================================

        [Display(Name = "Catégorie")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // ============================================
        // FOURNISSEUR PRINCIPAL
        // ============================================

        [Display(Name = "Fournisseur")]
        public int? FournisseurId { get; set; }

        [ForeignKey("FournisseurId")]
        public virtual Fournisseur? Fournisseur { get; set; }

        // ============================================
        // PRIX ET TVA
        // ============================================

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Prix Achat HT")]
        public decimal PrixAchatHT { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Prix Vente HT")]
        public decimal PrixVenteHT { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "Prix Vente TTC")]
        public decimal PrixVenteTTC { get; set; }

        [Display(Name = "Taux TVA (%)")]
        public decimal TauxTVA { get; set; } = 19; // 19% par défaut en Tunisie

        [Display(Name = "Fodec")]
        public bool Fodec { get; set; } = false; // 1% Fodec Tunisie

        // ============================================
        // STOCK
        // ============================================

        [Required]
        [Range(0, int.MaxValue)]
        [Display(Name = "Quantité en stock")]
        public int Quantite { get; set; }

        [Display(Name = "Stock minimum")]
        public int StockMin { get; set; } = 0;

        [Display(Name = "Stock maximum")]
        public int? StockMax { get; set; }

        [StringLength(20)]
        [Display(Name = "Unité")]
        public string Unite { get; set; } = "Pièce"; // Pièce, Kg, L, M...

        // ============================================
        // STATUT ET AUDIT
        // ============================================

        [Display(Name = "Actif")]
        public bool EstActif { get; set; } = true;

        [Display(Name = "Date de création")]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [Display(Name = "Date de modification")]
        public DateTime? DateModification { get; set; }

        // ============================================
        // PROPRIÉTÉS CALCULÉES
        // ============================================

        [NotMapped]
        public bool EnRuptureDeStock => Quantite <= StockMin;

        [NotMapped]
        public decimal MargeHT => PrixVenteHT - PrixAchatHT;

        [NotMapped]
        public decimal MargePourcentage => PrixAchatHT > 0
            ? Math.Round((MargeHT / PrixAchatHT) * 100, 2)
            : 0;

        // Détails (onglet Description)
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(100)] public string? Marque { get; set; }
        [StringLength(100)] public string? Modele { get; set; }
        [StringLength(50)] public string? Couleur { get; set; }
        [StringLength(50)] public string? Taille { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Poids { get; set; }

        public virtual ICollection<ArticleFournisseur> TarifsFournisseurs { get; set; } = new List<ArticleFournisseur>();
        public virtual ICollection<MouvementStock> Mouvements { get; set; } = new List<MouvementStock>();
    }
}