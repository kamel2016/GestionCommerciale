using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCommerciale.Models
{
    public class MouvementStock
    {
        [Key]
        public int ID { get; set; }

        public int ArticleId { get; set; }
        [ForeignKey("ArticleId")]
        public virtual Article Article { get; set; } = null!;

        public DateTime Date { get; set; } = DateTime.Now;

        [Required, StringLength(20)]
        public string Type { get; set; } = "Entrée";   // Entrée, Sortie

        // Positive pour une entrée, négative pour une sortie
        public int Quantite { get; set; }

        // Quantité de l'article après le mouvement (utile pour l'historique)
        public int StockApres { get; set; }

        [StringLength(250)]
        public string? Commentaire { get; set; }
    }
}