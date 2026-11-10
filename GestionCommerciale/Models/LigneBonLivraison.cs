// Models/LigneBonLivraison.cs
namespace GestionCommerciale.Models
{
    public class LigneBonLivraison
    {
        public int ID { get; set; }

        public int BonLivraisonId { get; set; }
        public BonLivraison? BonLivraison { get; set; }

        public int ArticleId { get; set; }
        public Article? Article { get; set; }

        // Lien vers la ligne de commande d'origine (pour calculer le reliquat)
        public int? LigneCommandeClientId { get; set; }
        public LigneCommandeClient? LigneCommandeClient { get; set; }

        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }

        public decimal TotalHT => Quantite * PrixUnitaire;
    }
}