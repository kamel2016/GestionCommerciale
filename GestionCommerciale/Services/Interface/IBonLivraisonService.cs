// Services/IBonLivraisonService.cs
using GestionCommerciale.Models;

namespace GestionCommerciale.Services.Interface
{
    public interface IBonLivraisonService
    {
        /// <summary>Crée un BL brouillon à partir d'une commande client.</summary>
        Task<BonLivraison> CreerDepuisCommandeAsync(int commandeClientId);

        /// <summary>Valide le BL : génère les sorties de stock et met à jour les quantités livrées.</summary>
        Task ValiderAsync(int bonLivraisonId);

        /// <summary>Annule un BL (et inverse les mouvements de stock s'il était validé).</summary>
        Task AnnulerAsync(int bonLivraisonId);
    }
}