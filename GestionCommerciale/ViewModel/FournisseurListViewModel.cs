using GestionCommerciale.Models;

namespace GestionCommerciale.ViewModel
{
    public class FournisseurListViewModel
    {
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public List<Fournisseur> Fournisseurs { get; set; } = new();
    }
}