using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionCommerciale.Models
{
    public class Category
    {
        // ============================================
        // PROPRIÉTÉS PRINCIPALES
        // ============================================

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le code est obligatoire")]
        [StringLength(20, ErrorMessage = "Le code ne peut pas dépasser 20 caractères")]
        [Display(Name = "Code")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        [Display(Name = "Nom de la catégorie")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // ============================================
        // HIÉRARCHIE (CATÉGORIES/SOUS-CATÉGORIES)
        // ============================================

        [Display(Name = "Catégorie parente")]
        public int? ParentCategoryId { get; set; }

        [ForeignKey("ParentCategoryId")]
        public virtual Category? ParentCategory { get; set; }

        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

        // ============================================
        // PROPRIÉTÉS DE STATUT ET AFFICHAGE
        // ============================================

        [Display(Name = "Statut")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ordre d'affichage")]
        public int DisplayOrder { get; set; } = 0;

        [StringLength(50)]
        [Display(Name = "Icône")]
        public string? Icon { get; set; } // Ex: "fas fa-laptop", "bi-printer"

        [StringLength(50)]
        [Display(Name = "Couleur")]
        public string? Color { get; set; } // Ex: "#4CAF50", "primary"

        // ============================================
        // RELATIONS AVEC LES ARTICLES
        // ============================================

        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();

        // ============================================
        // AUDIT (DATES DE CRÉATION/MODIFICATION)
        // ============================================

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Date de modification")]
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        [Display(Name = "Créé par")]
        public string? CreatedBy { get; set; }

        [StringLength(100)]
        [Display(Name = "Modifié par")]
        public string? UpdatedBy { get; set; }

        // ============================================
        // PROPRIÉTÉS CALCULÉES (NON MAPPÉES EN BDD)
        // ============================================

        [NotMapped]
        [Display(Name = "Nombre d'articles")]
        public int ArticlesCount => Articles?.Count ?? 0;

        [NotMapped]
        [Display(Name = "Nombre de sous-catégories")]
        public int SubCategoriesCount => SubCategories?.Count ?? 0;

        [NotMapped]
        [Display(Name = "A des sous-catégories")]
        public bool HasSubCategories => SubCategories?.Any() ?? false;

        [NotMapped]
        [Display(Name = "Est une sous-catégorie")]
        public bool IsSubCategory => ParentCategoryId.HasValue;

        [NotMapped]
        [Display(Name = "Chemin complet")]
        public string FullPath
        {
            get
            {
                if (ParentCategory != null)
                {
                    return $"{ParentCategory.Name} > {Name}";
                }
                return Name;
            }
        }

        [NotMapped]
        [Display(Name = "Statut texte")]
        public string StatusText => IsActive ? "Active" : "Inactive";

        [NotMapped]
        [Display(Name = "Classe CSS du statut")]
        public string StatusClass => IsActive ? "badge-success" : "badge-secondary";

        // ============================================
        // MÉTHODES UTILITAIRES
        // ============================================

        public override string ToString()
        {
            return $"{Code} - {Name}";
        }

        /// <summary>
        /// Vérifie si la catégorie peut être supprimée
        /// </summary>
        public bool CanBeDeleted()
        {
            return !Articles.Any() && !SubCategories.Any();
        }

        /// <summary>
        /// Récupère toutes les sous-catégories de manière récursive
        /// </summary>
        public IEnumerable<Category> GetAllSubCategories()
        {
            var result = new List<Category>();

            foreach (var subCategory in SubCategories)
            {
                result.Add(subCategory);
                result.AddRange(subCategory.GetAllSubCategories());
            }

            return result;
        }

        /// <summary>
        /// Compte le nombre total d'articles (incluant les sous-catégories)
        /// </summary>
        public int GetTotalArticleCount()
        {
            int count = ArticlesCount;

            foreach (var subCategory in SubCategories)
            {
                count += subCategory.GetTotalArticleCount();
            }

            return count;
        }
    }
}