namespace GestionCommerciale.ViewModel
{
    //public class ArticleViewModel
    //{
    //    public int Id { get; set; }
    //    public string Nom { get; set; } = string.Empty;
    //    public decimal Prix { get; set; }
    //    public int Stock { get; set; }
    //}

    public class ArticleViewModel
    {
        public int Id { get; set; }
        public string Reference { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Unit { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public decimal Weight { get; set; }
        public int VatRate { get; set; }
        public string Description { get; set; }
        public int StockMinimum { get; set; }
        public int StockMaximum { get; set; }
        public int CurrentStock { get; set; }
        public int ReservedStock { get; set; }
        public int OnOrderStock { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime LastStockUpdate { get; set; }
    }
}