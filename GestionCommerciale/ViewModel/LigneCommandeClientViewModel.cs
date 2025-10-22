namespace GestionCommerciale.ViewModel
{
    public class LigneCommandeClientViewModel
    {
        public int ArticleId { get; set; }
        public string NomArticle { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal MontantLigne { get; set; }
    }
}