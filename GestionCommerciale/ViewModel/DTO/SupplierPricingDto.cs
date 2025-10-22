namespace GestionCommerciale.ViewModel.DTO
{
    public class SupplierPricingDto
    {
        public int SupplierId { get; set; }
        public string SupplierCode { get; set; }
        public string SupplierName { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal PromotionalPrice { get; set; }
        public decimal RetailMargin { get; set; }
        public int RetailMaxDiscount { get; set; }
        public DateTime LastPurchaseDate { get; set; }
    }
}