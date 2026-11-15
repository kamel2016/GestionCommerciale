namespace GestionCommerciale.Enums
{
    public enum StatutDevis
    {
        Brouillon = 0,
        Envoye = 1,
        Accepte = 2,
        Refuse = 3,
        Expire = 4,
        Transforme = 5,   // devenu commande client
        Remplace = 6      // une nouvelle version existe
    }
}