using System.Text.RegularExpressions;
using GestionCommerciale.Models;

namespace GestionCommerciale.Helpers
{
    /// <summary>
    /// Règles de validation des clients, centralisées pour Create et Edit.
    /// Chaque méthode renvoie null si la valeur est correcte, sinon le message d'erreur.
    /// </summary>
    public static class ClientValidation
    {
        // Matricule fiscal : validation SOUPLE (décision du 2026-11-16).
        // Chiffres, lettres et "/" seulement, 20 caractères maximum, sans contrôle de la clé.
        // À resserrer plus tard avec l'avis du comptable.
        private static readonly Regex MatriculeRegex = new(@"^[A-Za-z0-9/]{1,20}\z", RegexOptions.Compiled);

        // Code postal tunisien : 4 chiffres.
        private static readonly Regex CodePostalRegex = new(@"^[0-9]{4}\z", RegexOptions.Compiled);

        public static string? ValiderMatriculeFiscale(string? valeur)
        {
            if (string.IsNullOrWhiteSpace(valeur)) return null; // champ facultatif
            return MatriculeRegex.IsMatch(valeur.Trim(' '))
                ? null
                : "Le matricule fiscal ne peut contenir que des chiffres, des lettres et « / » (20 caractères maximum).";
        }

        public static string? ValiderCodePostal(string? valeur)
        {
            if (string.IsNullOrWhiteSpace(valeur)) return null; // champ facultatif
            return CodePostalRegex.IsMatch(valeur.Trim(' '))
                ? null
                : "Le code postal doit contenir exactement 4 chiffres.";
        }

        public static string? ValiderRemise(decimal valeur)
            => valeur is >= 0 and <= 100
                ? null
                : "La remise doit être comprise entre 0 et 100 %.";

        public static string? ValiderPlafondCredit(decimal valeur)
            => valeur >= 0
                ? null
                : "Le plafond de crédit ne peut pas être négatif.";

        public static string? ValiderDelaiPaiement(int valeur)
            => valeur >= 0
                ? null
                : "Le délai de paiement ne peut pas être négatif.";

        /// <summary>
        /// Contrôle tous les champs d'un client. Renvoie des couples (nom du champ, message),
        /// prêts à être ajoutés au ModelState : ModelState.AddModelError($"Client.{champ}", message).
        /// </summary>
        public static IEnumerable<(string Champ, string Message)> Valider(Client client)
        {
            var erreurs = new List<(string, string)>();

            void Ajouter(string champ, string? message)
            {
                if (message != null) erreurs.Add((champ, message));
            }

            Ajouter(nameof(Client.MatriculeFiscale), ValiderMatriculeFiscale(client.MatriculeFiscale));
            Ajouter(nameof(Client.CodePostal), ValiderCodePostal(client.CodePostal));
            Ajouter(nameof(Client.RemiseDefaut), ValiderRemise(client.RemiseDefaut));
            Ajouter(nameof(Client.PlafondCredit), ValiderPlafondCredit(client.PlafondCredit));
            Ajouter(nameof(Client.DelaiPaiement), ValiderDelaiPaiement(client.DelaiPaiement));

            return erreurs;
        }
    }
}
