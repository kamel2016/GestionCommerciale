using GestionCommerciale.Data;
using GestionCommerciale.Models;
using GestionCommerciale.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace GestionCommerciale.Services
{
    public class SequenceService : ISequenceService
    {
        private readonly ApplicationDbContext _db;
        public SequenceService(ApplicationDbContext db) => _db = db;

        // À appeler DANS la transaction de validation de la facture
        public async Task<string> ProchainNumeroAsync(string type, int exercice)
        {
            var seq = await _db.SequencesDocument
                .FirstOrDefaultAsync(s => s.TypeDocument == type && s.Exercice == exercice);

            if (seq == null)
            {
                seq = new SequenceDocument { TypeDocument = type, Exercice = exercice };
                _db.SequencesDocument.Add(seq);
            }
            seq.DernierNumero++;
            await _db.SaveChangesAsync(); // le rowversion protège contre la concurrence
            return $"{type}-{exercice}-{seq.DernierNumero:D5}";
        }
    }
}
