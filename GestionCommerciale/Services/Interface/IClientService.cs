using GestionCommerciale.Models;

namespace GestionCommerciale.Services.Interface
{
    /// <summary>Résultat d'une création ou d'une vérification de client.</summary>
    public class ResultatClient
    {
        public bool Succes => Erreurs.Count == 0;

        /// <summary>Vrai si le client demandé n'existe pas (modification).</summary>
        public bool Introuvable { get; set; }

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
        /// Vrai si le client a au moins un document : commande, devis, bon de livraison ou facture.
        /// Dans ce cas son mode de facturation ne peut plus être modifié.
        /// </summary>
        Task<bool> ADesDocumentsAsync(int clientId, CancellationToken cancellationToken = default);

        /// <summary>Lit un client sans le suivre (lecture seule). Null s'il n'existe pas.</summary>
        Task<Client?> ObtenirAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Modifie un client existant. Seuls les champs modifiables du formulaire sont copiés :
        /// le code, la date de création et l'Id ne changent jamais, quoi que contienne <paramref name="saisie"/>.
        /// </summary>
        Task<ResultatClient> ModifierAsync(int id, Client saisie, CancellationToken cancellationToken = default);

        /// <summary>
        /// Vérifie l'unicité de l'email et du matricule fiscal (insensible à la casse, après trim,
        /// champs vides ignorés). <paramref name="idExclu"/> sert à la modification : le client lui-même est ignoré.
        /// </summary>
        Task<ResultatClient> VerifierUniciteAsync(Client client, int? idExclu = null, CancellationToken cancellationToken = default);
    }
}
