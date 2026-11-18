using GestionCommerciale.Models;

namespace GestionCommerciale.Helpers
{
    /// <summary>
    /// Nettoyage et règles de validation d'un article qui ne demandent pas la base de données
    /// (les contrôles d'unicité et de catégorie sont dans ArticleService).
    /// Le prix de vente TTC est LIBRE (décision C) : aucune règle de cohérence avec le HT ou la TVA.
    /// </summary>
    public static class ArticleValidation
    {
        public const int LongueurReference = 50;
        public const int LongueurLibelle = 200;
        public const int LongueurCodeBarre = 50;
        public const int LongueurUnite = 20;
        public const int LongueurMarqueModele = 100;
        public const int LongueurCouleurTaille = 50;

        public const decimal TvaMin = 0m;
        public const decimal TvaMax = 100m;
        public const int TvaDecimalesMax = 2;

        /// <summary>
        /// Retire les espaces en début et en fin. Référence, libellé et unité ne deviennent JAMAIS NULL (vides = erreur
        /// de validation) ; les champs facultatifs vides ou blancs deviennent NULL.
        /// </summary>
        public static void Normaliser(Article article)
        {
            article.Reference = (article.Reference ?? string.Empty).Trim();
            article.Libelle = (article.Libelle ?? string.Empty).Trim();
            article.Unite = (article.Unite ?? string.Empty).Trim();

            article.CodeBarre = Facultatif(article.CodeBarre);
            article.Marque = Facultatif(article.Marque);
            article.Modele = Facultatif(article.Modele);
            article.Couleur = Facultatif(article.Couleur);
            article.Taille = Facultatif(article.Taille);
            article.Description = Facultatif(article.Description);
        }

        private static string? Facultatif(string? valeur)
        {
            var nettoye = valeur?.Trim();
            return string.IsNullOrEmpty(nettoye) ? null : nettoye;
        }

        /// <summary>Valide un article DÉJÀ normalisé. Renvoie des couples (nom du champ, message), au plus un message par règle.</summary>
        public static List<(string Champ, string Message)> Valider(Article article)
        {
            var erreurs = new List<(string, string)>();

            // Champs obligatoires et longueurs (règles existantes, attributs de Article.cs)
            if (article.Reference.Length == 0)
                erreurs.Add((nameof(Article.Reference), "La référence est obligatoire."));
            else if (article.Reference.Length > LongueurReference)
                erreurs.Add((nameof(Article.Reference), $"La référence ne doit pas dépasser {LongueurReference} caractères."));

            if (article.Libelle.Length == 0)
                erreurs.Add((nameof(Article.Libelle), "Le libellé est obligatoire."));
            else if (article.Libelle.Length > LongueurLibelle)
                erreurs.Add((nameof(Article.Libelle), $"Le libellé ne doit pas dépasser {LongueurLibelle} caractères."));

            if (article.Unite.Length == 0)
                erreurs.Add((nameof(Article.Unite), "L'unité est obligatoire."));
            else if (article.Unite.Length > LongueurUnite)
                erreurs.Add((nameof(Article.Unite), $"L'unité ne doit pas dépasser {LongueurUnite} caractères."));

            Longueur(erreurs, nameof(Article.CodeBarre), article.CodeBarre, LongueurCodeBarre, "Le code-barres");
            Longueur(erreurs, nameof(Article.Marque), article.Marque, LongueurMarqueModele, "La marque");
            Longueur(erreurs, nameof(Article.Modele), article.Modele, LongueurMarqueModele, "Le modèle");
            Longueur(erreurs, nameof(Article.Couleur), article.Couleur, LongueurCouleurTaille, "La couleur");
            Longueur(erreurs, nameof(Article.Taille), article.Taille, LongueurCouleurTaille, "La taille");

            // Prix (décision C : le TTC est libre, seul le signe est contrôlé) et poids
            if (article.PrixAchatHT < 0)
                erreurs.Add((nameof(Article.PrixAchatHT), "Le prix d'achat HT doit être positif ou nul."));
            if (article.PrixVenteHT < 0)
                erreurs.Add((nameof(Article.PrixVenteHT), "Le prix de vente HT doit être positif ou nul."));
            if (article.PrixVenteTTC < 0)
                erreurs.Add((nameof(Article.PrixVenteTTC), "Le prix de vente TTC doit être positif ou nul."));
            if (article.Poids < 0)
                erreurs.Add((nameof(Article.Poids), "Le poids doit être positif ou nul."));

            // N4 : TVA de 0 à 100, 2 décimales au plus, aucune liste de taux imposée
            if (article.TauxTVA < TvaMin || article.TauxTVA > TvaMax)
                erreurs.Add((nameof(Article.TauxTVA), $"Le taux de TVA doit être compris entre {TvaMin:0} et {TvaMax:0}."));
            else if (decimal.Round(article.TauxTVA, TvaDecimalesMax) != article.TauxTVA)
                erreurs.Add((nameof(Article.TauxTVA), $"Le taux de TVA accepte au plus {TvaDecimalesMax} décimales."));

            // N5 : stocks (max = min et max = 0 avec min = 0 sont valides)
            if (article.StockMin < 0)
                erreurs.Add((nameof(Article.StockMin), "Le stock minimum doit être positif ou nul."));
            if (article.StockMax.HasValue && article.StockMax.Value < 0)
                erreurs.Add((nameof(Article.StockMax), "Le stock maximum doit être positif ou nul."));
            else if (article.StockMax.HasValue && article.StockMin >= 0 && article.StockMax.Value < article.StockMin)
                erreurs.Add((nameof(Article.StockMax), "Le stock maximum doit être supérieur ou égal au stock minimum."));

            return erreurs;
        }

        private static void Longueur(List<(string, string)> erreurs, string champ, string? valeur, int max, string libelle)
        {
            if (valeur != null && valeur.Length > max)
                erreurs.Add((champ, $"{libelle} ne doit pas dépasser {max} caractères."));
        }
    }
}
