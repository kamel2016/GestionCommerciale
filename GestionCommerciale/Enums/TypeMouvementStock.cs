using System.ComponentModel.DataAnnotations;

public enum TypeMouvementStock
{
    /// <summary>Entrée en stock via un bon de réception fournisseur.</summary>
    [Display(Name = "Entrée (réception fournisseur)")]
    EntreeReception = 0,

    /// <summary>Sortie de stock via un bon de livraison client.</summary>
    [Display(Name = "Sortie (livraison client)")]
    SortieLivraison = 1,

    /// <summary>Ajustement manuel d'inventaire (écart de comptage, casse, etc.).</summary>
    [Display(Name = "Ajustement d'inventaire")]
    AjustementInventaire = 2,

    /// <summary>Retour client (entrée de stock après retour marchandise).</summary>
    [Display(Name = "Retour client")]
    RetourClient = 3,

    /// <summary>Retour fournisseur (sortie de stock après retour marchandise).</summary>
    [Display(Name = "Retour fournisseur")]
    RetourFournisseur = 4,

    /// <summary>Transfert interne entre emplacements (réservé pour usage futur).</summary>
    [Display(Name = "Transfert interne")]
    TransfertInterne = 5
}