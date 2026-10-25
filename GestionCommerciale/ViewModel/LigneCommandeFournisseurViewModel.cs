namespace GestionCommerciale.ViewModel
{
    public class LigneCommandeFournisseurViewModel
    {
        public int ArticleId { get; set; }
        public string NomArticle { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal MontantLigne { get; set; }
    }
}