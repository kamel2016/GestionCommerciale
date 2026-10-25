using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCommerciale.Models
{
    public class LigneBonReception
    {
        public int Id { get; set; }

        public int BonReceptionId { get; set; }
        public virtual BonReception BonReception { get; set; } = null!;

        public int ArticleId { get; set; }
        public virtual Article Article { get; set; } = null!;

        public int Quantite { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal PrixUnitaireHT { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TauxTVA { get; set; }
    }
}