using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Models
{
    public class SequenceDocument
    {
        public int Id { get; set; }
        public string TypeDocument { get; set; } = "";  // "FAC", "AVO"
        public int Exercice { get; set; }
        public int DernierNumero { get; set; }
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}