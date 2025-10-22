namespace GestionCommerciale.ViewModel.DTO
{
    public class LigneCommandeClientDto
    {
        public int ArticleId { get; set; }
        public string ArticleNom { get; set; } = string.Empty;
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal MontantLigne { get; set; }
    }
}