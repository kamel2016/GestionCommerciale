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
                entity.HasIndex(e => e.Reference).IsUnique();

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Articles)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Fournisseur)
                      .WithMany(f => f.Articles)
                      .HasForeignKey(e => e.FournisseurId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── ArticleFournisseur ───────────────────────────
            modelBuilder.Entity<ArticleFournisseur>(entity =>
            {
                entity.HasIndex(af => new { af.ArticleId, af.FournisseurId }).IsUnique();

                entity.HasOne(af => af.Fournisseur)
                      .WithMany()
                      .HasForeignKey(af => af.FournisseurId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

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
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TauxTVADefaut).HasPrecision(5, 2);
                entity.Property(e => e.TimbreFiscal).HasPrecision(18, 3);
            });
        }
    }
}