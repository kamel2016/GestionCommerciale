namespace GestionCommerciale.ViewModel
{
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartItem => (CurrentPage - 1) * PageSize + 1;
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);

        public string SortBy { get; set; } = "NomComplet";
        public string SortDirection { get; set; } = "asc";

        public List<int> PageSizeOptions { get; set; } = new() { 5, 10, 25, 50, 100 };
    }
}