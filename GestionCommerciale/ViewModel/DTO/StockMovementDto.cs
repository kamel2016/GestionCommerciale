namespace GestionCommerciale.ViewModel.DTO
{
    /// <summary>
    /// DTO pour un mouvement de stock individuel
    /// </summary>
    public class StockMovementDto
    {
        /// <summary>
        /// Date et heure du mouvement
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Type de mouvement (Réception, Vente, Réservation, Ajustement, etc.)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Quantité du mouvement
        /// Positive pour une entrée de stock (réception, retour)
        /// Négative pour une sortie de stock (vente, réservation)
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Référence du mouvement (numéro de bon de livraison, de commande, etc.)
        /// </summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Commentaire ou description du mouvement
        /// </summary>
        public string Comment { get; set; } = string.Empty;
    }
}