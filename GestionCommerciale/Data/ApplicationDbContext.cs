using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Models;

namespace GestionCommerciale.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Définition des DbSet pour vos entités
        public DbSet<Article> Articles { get; set; }
        public DbSet<Fournisseur> Fournisseurs { get; set; }
        public DbSet<Categorie> Categories { get; set; }
        public DbSet<CommandeClient> CommandesClient { get; set; }
        public DbSet<LigneCommandeClient> LignesCommandeClient { get; set; }
        public DbSet<Client> Clients { get; set; }


        // Configuration des entités
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration de l'entité Article
            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CodeBarre).HasMaxLength(50);
                entity.Property(e => e.Quantite);
                entity.Property(e => e.PrixVenteHT).HasPrecision(18, 3);
                entity.Property(e => e.PrixVenteTTC).HasPrecision(18, 3);
                entity.Property(e => e.EstActif);
            });

            // Configuration de l'entité Product
            modelBuilder.Entity<Fournisseur>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Telephone).HasMaxLength(10);
                entity.Property(e => e.Adresse).HasMaxLength(500);
                entity.Property(e => e.PersonneContact).HasMaxLength(200);
                entity.Property(e => e.DateCreation);
                entity.Property(e => e.DateModification);

                // Relation avec CommandesFournisseur
                //    entity.HasOne(p => p.CommandesFournisseur)
                //          .WithMany(c => c.)
                //          .HasForeignKey(p => p.CategoryId)
                //          .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuration de l'entité Category
            modelBuilder.Entity<Categorie>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.Libelle).IsUnique();
            });

            // Configuration de l'entité CommandeClient
            modelBuilder.Entity<CommandeClient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NumeroCommande).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DateCommande).IsRequired();
                entity.Property(e => e.DateLivraison).IsRequired();
                entity.Property(e => e.DateCreation).IsRequired();
                entity.Property(e => e.DateModification);
                entity.Property(e => e.ClientId);
                entity.Property(e => e.Statut);
                entity.Property(e => e.MontantHT).HasPrecision(18, 3);
                entity.Property(e => e.MontantTTC).HasPrecision(18, 3);
                entity.Property(e => e.MontantTVA).HasPrecision(18, 3);
                entity.Property(e => e.Commentaire).HasMaxLength(500);
            });

            // Configuration de l'entité Client
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Prenom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.Telephone).HasMaxLength(10);
                entity.Property(e => e.Adresse).HasMaxLength(500);
                entity.Property(e => e.DateCreation);
                entity.Property(e => e.DateModification);
            });

            // Configuration de l'entité LigneCommandeClient
            modelBuilder.Entity<LigneCommandeClient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ArticleId).IsUnique();
                entity.HasIndex(e => e.CommandeClientId).IsUnique();
                entity.Property(e => e.Quantite);
                entity.Property(e => e.PrixUnitaire).HasPrecision(18, 3);
                entity.Property(e => e.MontantLigne).HasPrecision(18, 3);
            });

            // Données de test (optionnel)
            SeedData(modelBuilder);
        }

        // Méthode pour insérer des données de test
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Catégories de test
            modelBuilder.Entity<Article>().HasData(
                new Article { ID = 1, Reference = "ART001", Libelle = "Ordinateur portable", CodeBarre = "123456789", Quantite = 10, PrixVenteHT = 800.00m, PrixVenteTTC = 960.00m },
                new Article { ID = 2, Reference = "ART002", Libelle = "Souris sans fil", CodeBarre = "987654321", Quantite = 25, PrixVenteHT = 25.00m, PrixVenteTTC = 30.00m },
                new Article { ID = 3, Reference = "ART003", Libelle = "Clavier mécanique", CodeBarre = "456789123", Quantite = 15, PrixVenteHT = 120.00m, PrixVenteTTC = 144.00m },
                new Article { ID = 4, Reference = "ART004", Libelle = "Écran 24 pouces", CodeBarre = "789123456", Quantite = 8, PrixVenteHT = 200.00m, PrixVenteTTC = 240.00m },
                new Article { ID = 5, Reference = "ART005", Libelle = "Imprimante laser", CodeBarre = "321654987", Quantite = 5, PrixVenteHT = 300.00m, PrixVenteTTC = 360.00m }
            );

            modelBuilder.Entity<LigneCommandeClient>().HasData(
                new LigneCommandeClient
                {
                    Id = 1,
                    CommandeClientId = 1,
                    ArticleId = 1,
                    Quantite = 12,
                    PrixUnitaire = 12.125m,
                    MontantLigne = 145.500m
                },
                new LigneCommandeClient
                {
                    Id = 2,
                    CommandeClientId = 1,
                    ArticleId = 2,
                    Quantite = 7,
                    PrixUnitaire = 11.741m,
                    MontantLigne = 82.187m
                },                
                new LigneCommandeClient
                {
                    Id = 3,
                    CommandeClientId = 1,
                    ArticleId = 3,
                    Quantite = 5,
                    PrixUnitaire = 8.450m,
                    MontantLigne = 42.250m
                }
            );

            modelBuilder.Entity<Client>().HasData(
                new Client
                {
                    Id = 1,
                    Nom = "Dupont",
                    Prenom = "Alice",
                    Email = "alice.dupont@example.com",
                    Telephone = "0601020304",
                    Adresse = "123 Rue de Paris, 75001 Paris",
                    DateCreation = DateTime.Now.AddDays(-30)
                },
                new Client
                {
                    Id = 2,
                    Nom = "Martin",
                    Prenom = "Jean",
                    Email = "jean.martin@example.com",
                    Telephone = "0605060708",
                    Adresse = "45 Avenue des Champs-Élysées, 75008 Paris",
                    DateCreation = DateTime.Now.AddDays(-25)
                },
                new Client
                {
                    Id = 3,
                    Nom = "Leroy",
                    Prenom = "Claire",
                    Email = "claire.leroy@example.com",
                    Telephone = "0611223344",
                    Adresse = "88 Boulevard Haussmann, 75009 Paris",
                    DateCreation = DateTime.Now.AddDays(-20)
                },
                new Client
                {
                    Id = 4,
                    Nom = "Nguyen",
                    Prenom = "Marc",
                    Email = "marc.nguyen@example.com",
                    Telephone = "0622334455",
                    Adresse = "12 Rue du Faubourg Saint-Honoré, 75008 Paris",
                    DateCreation = DateTime.Now.AddDays(-15)
                },
                new Client
                {
                    Id = 5,
                    Nom = "Bernard",
                    Prenom = "Lucie",
                    Email = "lucie.bernard@example.com",
                    Telephone = "0633445566",
                    Adresse = "5 Place Bellecour, 69002 Lyon",
                    DateCreation = DateTime.Now.AddDays(-10)
                },
                new Client
                {
                    Id = 6,
                    Nom = "Petit",
                    Prenom = "Antoine",
                    Email = "antoine.petit@example.com",
                    Telephone = "0644556677",
                    Adresse = "17 Rue Sainte-Catherine, 33000 Bordeaux",
                    DateCreation = DateTime.Now.AddDays(-5)
                }
            );

            // Utilisateurs de test
            modelBuilder.Entity<Fournisseur>().HasData(
                 new Fournisseur
                 {
                     Id = 1,
                     Nom = "TechDistrib SARL",
                     Email = "contact@techdistrib.com",
                     Telephone = "0123456789",
                     Adresse = "12 Rue des Industries, 75010 Paris",
                     PersonneContact = "Sophie Morel",
                     DateCreation = DateTime.Now
                 },
                new Fournisseur
                {
                    Id = 2,
                    Nom = "Solutions BTP",
                    Email = "info@solutionsbtp.fr",
                    Telephone = "0987654321",
                    Adresse = "8 Avenue des Travaux, 69007 Lyon",
                    PersonneContact = "Jean-Luc Martin",
                    DateCreation = DateTime.Now.AddDays(-10)
                },
                new Fournisseur
                {
                    Id = 3,
                    Nom = "Fournipro Services",
                    Email = "service@fournipro.com",
                    Telephone = "0678452310",
                    Adresse = "5 Boulevard Fournisseurs, 31000 Toulouse",
                    PersonneContact = "Claire Dubois",
                    DateCreation = DateTime.Now.AddMonths(-2)
                },
                new Fournisseur
                {
                    Id = 4,
                    Nom = "Matériel Express",
                    Email = "contact@matexpress.fr",
                    Telephone = "0147852369",
                    Adresse = "17 Rue du Commerce, 59800 Lille",
                    PersonneContact = "Karim El Amrani",
                    DateCreation = DateTime.Now.AddMonths(-1).AddDays(-3)
                },
                new Fournisseur
                {
                    Id = 5,
                    Nom = "InnovaTech Distribution",
                    Email = "support@innovatech.fr",
                    Telephone = "0612345678",
                    Adresse = "20 Chemin des Entrepreneurs, 13008 Marseille",
                    PersonneContact = "Nathalie Bernard",
                    DateCreation = DateTime.Now.AddDays(-30)
                }
            );

            // Produits de test
            modelBuilder.Entity<Categorie>().HasData(
                new Categorie { ID = 1, Libelle = "Electroniques" },
                new Categorie { ID = 2, Libelle = "Vidéos" },
                new Categorie { ID = 3, Libelle = "Télésurveillance" },
                new Categorie { ID = 4, Libelle = "Caméras embarquée" },
                new Categorie { ID = 5, Libelle = "Luminaires" }
            );
        }
    }
}