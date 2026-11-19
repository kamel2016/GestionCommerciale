using System.ComponentModel.DataAnnotations;

/// <summary>
/// Type d'un mouvement de stock. CONVENTION : la quantité d'un mouvement est TOUJOURS positive ; le sens (entrée ou sortie)
/// est porté uniquement par le type (voir <see cref="TypeMouvementStockExtensions.Sens"/>), jamais par le signe de la quantité.
/// </summary>
public enum TypeMouvementStock
{
    /// <summary>Entrée en stock via un bon de réception fournisseur.</summary>
    [Display(Name = "Entrée (réception fournisseur)")]
    EntreeReception = 0,

    /// <summary>Sortie de stock via un bon de livraison client.</summary>
    [Display(Name = "Sortie (livraison client)")]
    SortieLivraison = 1,

    /// <summary>
    /// ANCIEN type d'ajustement, dont le sens dépendait du signe de la quantité. Conservé pour lire d'éventuels
    /// mouvements anciens ; plus aucun nouveau mouvement ne l'utilise (voir AjustementPositif et AjustementNegatif).
    /// </summary>
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
    TransfertInterne = 5,

    /// <summary>Ajustement d'inventaire qui AJOUTE du stock (écart de comptage en plus). Motif obligatoire.</summary>
    [Display(Name = "Ajustement d'inventaire (+)")]
    AjustementPositif = 6,

    /// <summary>Ajustement d'inventaire qui RETIRE du stock (casse, écart de comptage en moins). Motif obligatoire.</summary>
    [Display(Name = "Ajustement d'inventaire (-)")]
    AjustementNegatif = 7,

    /// <summary>Entrée de stock due à l'annulation d'un bon de livraison validé (inverse de la sortie de livraison).</summary>
    [Display(Name = "Entrée (annulation de livraison)")]
    EntreeAnnulationLivraison = 8
}

/// <summary>Le sens d'un mouvement, défini UNE seule fois ici : tout le reste du code (calcul du stock, affichages) s'y réfère.</summary>
public static class TypeMouvementStockExtensions
{
    /// <summary>+1 : le type augmente le stock ; -1 : il le diminue ; 0 : type non utilisable pour un nouveau mouvement (ancien ajustement signé, transfert).</summary>
    public static int Sens(this TypeMouvementStock type) => type switch
    {
        TypeMouvementStock.EntreeReception => 1,
        TypeMouvementStock.RetourClient => 1,
        TypeMouvementStock.AjustementPositif => 1,
        TypeMouvementStock.EntreeAnnulationLivraison => 1,
        TypeMouvementStock.SortieLivraison => -1,
        TypeMouvementStock.RetourFournisseur => -1,
        TypeMouvementStock.AjustementNegatif => -1,
        _ => 0
    };
}
