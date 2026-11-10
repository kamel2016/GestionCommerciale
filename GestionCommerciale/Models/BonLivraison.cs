// Models/BonLivraison.cs
using System.ComponentModel.DataAnnotations;
using GestionCommerciale.Enums;

namespace GestionCommerciale.Models
{
    public class BonLivraison
    {
        public int ID { get; set; }

        [Display(Name = "N° BL")]
        public string Numero { get; set; } = string.Empty;

        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Now;

        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        public int? CommandeClientId { get; set; }
        public CommandeClient? CommandeClient { get; set; }

        public StatutBonLivraison Statut { get; set; } = StatutBonLivraison.Brouillon;

        public string? Commentaire { get; set; }

        public ICollection<LigneBonLivraison> Lignes { get; set; } = new List<LigneBonLivraison>();

        public decimal TotalHT => Lignes.Sum(l => l.TotalHT);
    }
}