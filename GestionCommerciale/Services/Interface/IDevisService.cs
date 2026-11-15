using GestionCommerciale.Models;

namespace GestionCommerciale.Services.Interface
{
    public interface IDevisService
    {
        Task<Devis> CreerAsync(Devis devis, string utilisateur);
        Task MettreAJourAsync(Devis devis);
        Task EnvoyerAsync(int devisId);
        Task AccepterAsync(int devisId);
        Task RefuserAsync(int devisId);
        Task<Devis> NouvelleVersionAsync(int devisId, string utilisateur);
        Task<CommandeClient> TransformerEnCommandeAsync(int devisId, int? clientIdExistant, string utilisateur);
        Task<int> MarquerExpiresAsync();
    }
}