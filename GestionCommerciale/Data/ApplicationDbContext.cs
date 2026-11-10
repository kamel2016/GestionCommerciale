using Microsoft.EntityFrameworkCore;
using GestionCommerciale.Models;
using System;

namespace GestionCommerciale.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BonReception> BonsReception { get; set; }
        public DbSet<LigneBonReception> LignesBonReception { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Fournisseur> Fournisseurs { get; set; }
        public DbSet<CommandeClient> CommandesClient { get; set; }
        public DbSet<LigneCommandeClient> LignesCommandeClient { get; set; }
        public DbSet<CommandeFournisseur> CommandesFournisseur { get; set; }
        public DbSet<ParametresSociete> ParametresSociete { get; set; }
        public DbSet<ArticleFournisseur> ArticleFournisseurs => Set<ArticleFournisseur>();
        public DbSet<MouvementStock> MouvementsStock => Set<MouvementStock>();
        public DbSet<LigneCommandeFournisseur> LignesCommandeFournisseur { get; set; }
        public DbSet<BonLivraison> BonsLivraison { get; set; }
        public DbSet<LigneBonLivraison> LignesBonLivraison { get; set; }

        public DbSet<Facture> Factures { get; set; }
        public DbSet<LigneFacture> LignesFacture { get; set; }
        public DbSet<SequenceDocument> SequencesDocument { get; set; }
        public DbSet<TauxTva> TauxTva { get; set; }
        public DbSet<Reglement> Reglements { get; set; }

        // ============================================
        // LOT 1 — DEVISES ET TAUX DE CHANGE
        // ============================================
        public DbSet<Devise> Devises => Set<Devise>();
        public DbSet<TauxChange> TauxChanges => Set<TauxChange>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // ── Category ─────────────────────────────────────
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Code).IsUnique();

                // Hiérarchie : interdit la suppression en cascade d'une catégorie parente
                entity.HasOne(c => c.ParentCategory)
                      .WithMany(c => c.SubCategories)
                      .HasForeignKey(c => c.ParentCategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── Article ──────────────────────────────────────
            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.Reference).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Libelle).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CodeBarre).HasMaxLength(50);
                entity.Property(e => e.PrixAchatHT).HasPrecision(18, 3);
                entity.Property(e => e.PrixVenteHT).HasPrecision(18, 3);
                entity.Property(e => e.PrixVenteTTC).HasPrecision(18, 3);
                entity.Property(e => e.TauxTVA).HasPrecision(5, 2);
                entity.Property(e => e.Poids).HasPrecision(10, 3);

                // Lot 1 — CMUP, prix d'achat en devise étrangère
                entity.Property(e => e.CMUP).HasPrecision(18, 3);
                entity.Property(e => e.PrixAchatDevise).HasPrecision(18, 3);

                entity.HasIndex(e => e.Reference).IsUnique();

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Articles)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Fournisseur)
                      .WithMany(f => f.Articles)
                      .HasForeignKey(e => e.FournisseurId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Lot 1 — devise d'achat (optionnelle, SetNull pour ne pas perdre l'historique)
                entity.HasOne(e => e.DeviseAchat)
                      .WithMany()
                      .HasForeignKey(e => e.DeviseAchatId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Facture>().HasIndex(f => f.Numero).IsUnique();

            modelBuilder.Entity<SequenceDocument>().HasIndex(s => new { s.TypeDocument, s.Exercice }).IsUnique();

            modelBuilder.Entity<TauxTva>().HasData(
                new TauxTva { ID = 1, Libelle = "Exonéré (0 %)", Taux = 0, Actif = true },
                new TauxTva { ID = 2, Libelle = "TVA 7 %", Taux = 7, Actif = true },
                new TauxTva { ID = 3, Libelle = "TVA 13 %", Taux = 13, Actif = true },
                new TauxTva { ID = 4, Libelle = "TVA 19 %", Taux = 19, Actif = true }
            );

            // ── ArticleFournisseur ───────────────────────────
            modelBuilder.Entity<ArticleFournisseur>(entity =>
            {
                entity.HasIndex(af => new { af.ArticleId, af.FournisseurId }).IsUnique();

                entity.HasOne(af => af.Fournisseur)
                      .WithMany()
                      .HasForeignKey(af => af.FournisseurId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BonLivraison>()
                .HasIndex(b => b.Numero)
                .IsUnique();

            modelBuilder.Entity<LigneBonLivraison>()
                .Property(l => l.PrixUnitaire)
                .HasPrecision(18, 3);

            modelBuilder.Entity<BonLivraison>()
                .HasMany(b => b.Lignes)
                .WithOne(l => l.BonLivraison)
                .HasForeignKey(l => l.BonLivraisonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LigneBonLivraison>()
                .HasOne(l => l.LigneCommandeClient)
                .WithMany()
                .HasForeignKey(l => l.LigneCommandeClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Client ───────────────────────────────────────
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PlafondCredit).HasPrecision(18, 3);
                entity.Property(e => e.RemiseDefaut).HasPrecision(5, 2);
            });

            // ── Fournisseur ──────────────────────────────────
            modelBuilder.Entity<Fournisseur>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nom).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RemiseAccordee).HasPrecision(5, 2);
            });

            // ── CommandeClient ───────────────────────────────
            modelBuilder.Entity<CommandeClient>(entity =>
            {
                entity.Property(c => c.MontantHT).HasPrecision(18, 3);
                entity.Property(c => c.MontantTVA).HasPrecision(18, 3);
                entity.Property(c => c.MontantTTC).HasPrecision(18, 3);
            });

            // ── LigneCommandeClient ──────────────────────────
            modelBuilder.Entity<LigneCommandeClient>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CommandeClientId, e.ArticleId }).IsUnique();
                entity.Property(e => e.PrixUnitaire).HasPrecision(18, 3);
                entity.Property(e => e.MontantLigne).HasPrecision(18, 3);

                entity.HasOne(e => e.CommandeClient)
                      .WithMany(c => c.LignesCommande)
                      .HasForeignKey(e => e.CommandeClientId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Article)
                      .WithMany()
                      .HasForeignKey(e => e.ArticleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── BonReception ─────────────────────────────────
            modelBuilder.Entity<BonReception>(entity =>
            {
                entity.HasOne(b => b.CommandeFournisseur)
                      .WithMany(c => c.BonsReception)
                      .HasForeignKey(b => b.CommandeFournisseurId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── ParametresSociete ────────────────────────────
            modelBuilder.Entity<ParametresSociete>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.TauxTVADefaut).HasPrecision(5, 2);
                entity.Property(e => e.TimbreFiscal).HasPrecision(18, 3);
            });

            // ============================================
            // LOT 1 — Devise
            // ============================================
            modelBuilder.Entity<Devise>(entity =>
            {
                entity.HasIndex(d => d.Code).IsUnique();
                entity.Property(d => d.Libelle).IsRequired().HasMaxLength(100);
                entity.Property(d => d.Symbole).HasMaxLength(5);

                // S'assure qu'au plus une devise est marquée principale.
                // (Filtré par EstDevisePrincipale = true.)
                entity.HasIndex(d => d.EstDevisePrincipale)
                      .IsUnique()
                      .HasFilter("[EstDevisePrincipale] = 1");

                // Seed des 3 devises métier. TND est la devise principale.
                entity.HasData(
                    new Devise
                    {
                        Id = 1,
                        Code = "TND",
                        Libelle = "Dinar tunisien",
                        Symbole = "د.ت",
                        EstDevisePrincipale = true,
                        Actif = true,
                        DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new Devise
                    {
                        Id = 2,
                        Code = "EUR",
                        Libelle = "Euro",
                        Symbole = "€",
                        EstDevisePrincipale = false,
                        Actif = true,
                        DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new Devise
                    {
                        Id = 3,
                        Code = "USD",
                        Libelle = "Dollar américain",
                        Symbole = "$",
                        EstDevisePrincipale = false,
                        Actif = true,
                        DateCreation = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    }
                );
            });

            modelBuilder.Entity<Reglement>()
                .HasOne(r => r.Facture)
                .WithMany(f => f.Reglements)
                .HasForeignKey(r => r.FactureId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================
            // LOT 1 — TauxChange
            // ============================================
            modelBuilder.Entity<TauxChange>(entity =>
            {
                entity.Property(t => t.Taux).HasPrecision(18, 6);
                entity.Property(t => t.Source).HasMaxLength(50);

                // Un seul taux par devise et par date.
                entity.HasIndex(t => new { t.DeviseId, t.Date }).IsUnique();

                entity.HasOne(t => t.Devise)
                      .WithMany(d => d.TauxChanges)
                      .HasForeignKey(t => t.DeviseId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ============================================
            // LOT 1 — MouvementStock : le champ Type est désormais un enum.
            // ============================================
            modelBuilder.Entity<MouvementStock>(entity =>
            {
                entity.Property(m => m.Type).HasConversion<int>();
                entity.Property(m => m.SourceType).HasMaxLength(50);
                entity.Property(m => m.CoutUnitaire).HasPrecision(18, 3);
                entity.HasIndex(m => new { m.ArticleId, m.Date });
            });
        }
    }
}
