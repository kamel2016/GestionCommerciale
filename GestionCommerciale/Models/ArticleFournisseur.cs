using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCommerciale.Models
{
    public class ArticleFournisseur
    {
        [Key]
        public int ID { get; set; }

        public int ArticleId { get; set; }
        [ForeignKey("ArticleId")]
        public virtual Article Article { get; set; } = null!;

        public int FournisseurId { get; set; }
        [ForeignKey("FournisseurId")]
        public virtual Fournisseur Fournisseur { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")] public decimal PrixGrosHT { get; set; }
        [Column(TypeName = "decimal(18,3)")] public decimal PrixDetailHT { get; set; }
        [Column(TypeName = "decimal(18,3)")] public decimal? PrixPromoHT { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal RemiseMax { get; set; }

        public DateTime? DernierAchat { get; set; }
    }
}