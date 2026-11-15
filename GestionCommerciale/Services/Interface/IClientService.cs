using GestionCommerciale.Models;

namespace GestionCommerciale.Services.Interface
{
    /// <summary>Résultat d'une création ou d'une vérification de client.</summary>
    public class ResultatClient
    {
        public bool Succes => Erreurs.Count == 0;

        /// <summary>Couples (nom du champ, message). Champ vide = erreur générale.</summary>
        public List<(string Champ, string Message)> Erreurs { get; } = new();
    }

    public interface IClientService
    {
        /// <summary>
        /// Nettoie (trim), valide, vérifie l'unicité, génère le code client puis enregistre.
        /// </summary>
        Task<ResultatClient> CreerAsync(Client client, CancellationToken cancellationToken = default);

        /// <summary>
        /// Vérifie l'unicité de l'email et du matricule fiscal (insensible à la casse, après trim,
        /// champs vides ignorés). <paramref name="idExclu"/> sert à la modification : le client lui-même est ignoré.
        /// </summary>
        Task<ResultatClient> VerifierUniciteAsync(Client client, int? idExclu = null, CancellationToken cancellationToken = default);
    }
}
