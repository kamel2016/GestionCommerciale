using System.ComponentModel.DataAnnotations;

namespace GestionCommerciale.Models
{
    public class Categorie
    {
        public Categorie()
        {
            this.Article = new HashSet<Article>();
        }

        public int ID { get; set; }
        [Required]
        public string Libelle { get; set; }

        public virtual ICollection<Article> Article { get; set; }

        public Categorie ToCategory()
        {
            return new Categorie()
            {
                ID = this.ID,
                Libelle = this.Libelle,
                //Article = this.Article.Select(p => p.ToArticle()).ToList()
            };
        }
    }
}