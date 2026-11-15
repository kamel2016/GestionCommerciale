using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Enums
{
    public enum ModeFacturation
    {
        [Display(Name = "Client en compte (BL + facture mensuelle)")]
        EnCompte = 0,

        [Display(Name = "Client passager (facture directe)")]
        Passager = 1
    }
}