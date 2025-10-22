namespace GestionCommerciale.ViewModel.DTO
{
    /// <summary>
    /// DTO pour l'historique global du stock d'un article
    /// </summary>
    public class StockHistoryDto
    {
        /// <summary>
        /// Nombre total de mouvements enregistrés pour cet article
        /// </summary>
        public int TotalMovements { get; set; }

        /// <summary>
        /// Liste des derniers mouvements de stock (limité aux N derniers)
        /// </summary>
        public List<StockMovementDto> LastMovements { get; set; } = new();
    }
}