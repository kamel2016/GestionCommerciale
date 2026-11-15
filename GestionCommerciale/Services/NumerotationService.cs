// Services/NumerotationService.cs
using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Data;
using GestionCommerciale.Services.Interface;

namespace GestionCommerciale.Services
{
    public class NumerotationService : INumerotationService
    {
        private readonly ApplicationDbContext _context;

        public NumerotationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Doit être appelé DANS la transaction de l'appelant :
        /// si l'enregistrement échoue, le numéro est annulé (pas de trou).
        /// </summary>
        public async Task<string> ProchainNumeroAsync(string typeDocument, DateTime date)
        {
            int exercice = date.Year;

            // UPDLOCK + HOLDLOCK : verrou jusqu'à la fin de la transaction (SQL Server)
            var numeros = await _context.Database.SqlQuery<int>($@"
            MERGE SequencesDocument WITH (UPDLOCK, HOLDLOCK) AS cible
            USING (SELECT {typeDocument} AS TypeDocument, {exercice} AS Exercice) AS src
                ON cible.TypeDocument = src.TypeDocument AND cible.Exercice = src.Exercice
            WHEN MATCHED THEN
                UPDATE SET DernierNumero = cible.DernierNumero + 1
            WHEN NOT MATCHED THEN
                INSERT (TypeDocument, Exercice, DernierNumero) VALUES (src.TypeDocument, src.Exercice, 1)
            OUTPUT inserted.DernierNumero AS Value;").ToListAsync();

            return $"{typeDocument}-{exercice}-{numeros.Single():D5}";
        }

        //public async Task<string> ProchainNumeroBonLivraisonAsync()
        //{
        //    var prefixe = $"BL-{DateTime.Now.Year}-";

        //    var dernier = await _context.BonsLivraison
        //        .Where(b => b.Numero.StartsWith(prefixe))
        //        .OrderByDescending(b => b.Numero)
        //        .Select(b => b.Numero)
        //        .FirstOrDefaultAsync();

        //    var n = 1;
        //    if (dernier != null && int.TryParse(dernier.Substring(prefixe.Length), out var val))
        //        n = val + 1;

        //    return $"{prefixe}{n:D5}";
        //}
    }
}