namespace GestionCommerciale.Models
{
    public class LigneFacture
    {
        public int Id { get; set; }
        public int FactureId { get; set; }
        public Facture Facture { get; set; } = null!;
        public int ArticleId { get; set; }
        public Article Article { get; set; } = null!;
        public string Designation { get; set; } = "";
        public decimal Quantite { get; set; }
        public decimal PrixUnitaireHt { get; set; }
        public decimal RemisePourcent { get; set; }
        public decimal TauxTva { get; set; }        // 7, 13, 19
        public decimal TotalHt { get; set; }
    }
}