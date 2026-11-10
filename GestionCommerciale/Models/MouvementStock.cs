using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestionCommerciale.Enums;

namespace GestionCommerciale.Models
{
    /// <summary>
    /// Historique d'un mouvement de stock (entrée, sortie, ajustement, retour...).
    /// Chaque ligne conserve la quantité et le coût unitaire au moment du mouvement
    /// afin de pouvoir recalculer le CMUP de l'article a posteriori.
    /// </summary>
    public class MouvementStock
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int ArticleId { get; set; }

        [ForeignKey("ArticleId")]
        public virtual Article Article { get; set; } = null!;

        public DateTime Date { get; set; } = DateTime.Now;

        /// <summary>
        /// Type de mouvement (entrée réception, sortie livraison, ajustement inventaire,
        /// retour client, retour fournisseur, transfert interne).
        /// Remplace l'ancien champ <c>Type</c> qui était une chaîne libre.
        /// </summary>
        [Required]
        [Display(Name = "Type de mouvement")]
        public TypeMouvementStock Type { get; set; } = TypeMouvementStock.EntreeReception;

        /// <summary>
        /// Quantité du mouvement (signe géré selon le type :
        /// valeurs positives pour les entrées, négatives pour les sorties).
        /// </summary>
        public int Quantite { get; set; }

        /// <summary>Quantité de l'article AVANT le mouvement (utile pour l'audit et le recalcul CMUP).</summary>
        public int QuantiteAvant { get; set; }

        /// <summary>Quantité de l'article APRÈS le mouvement (utile pour l'historique).</summary>
        public int QuantiteApres { get; set; }

        /// <summary>
        /// Coût unitaire au moment du mouvement (en TND).
        /// Pour les sorties : égal au CMUP au moment de la sortie.
        /// Pour les entrées : prix d'achat (converti en TND si besoin) mis à jour du CMUP.
        /// </summary>
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "Coût unitaire (TND)")]
        public decimal CoutUnitaire { get; set; }

        // ============================================
        // TRAÇABILITÉ DE L'ORIGINE DU MOUVEMENT — Lot 1
        // ============================================
        // Permet de retrouver le document source (bon de réception, commande client,
        // ajustement manuel...) à l'origine du mouvement.

        [StringLength(50)]
        [Display(Name = "Type de source")]
        public string? SourceType { get; set; }
        // Valeurs attendues : "BonReception", "CommandeClient", "Inventaire", "RetourClient", ...

        [Display(Name = "Identifiant source")]
        public int? SourceId { get; set; }

        [StringLength(250)]
        public string? Commentaire { get; set; }

        [StringLength(50)]
        public string? Reference { get; set; }   // ex: "BR-2026-0012", "BL-2026-0034"

        // ============================================
        // CONCURRENCE OPTIMISTE — Lot 1
        // ============================================

        [Timestamp]
        [Display(Name = "Version concurrente")]
        public byte[]? RowVersion { get; set; }
    }
}
