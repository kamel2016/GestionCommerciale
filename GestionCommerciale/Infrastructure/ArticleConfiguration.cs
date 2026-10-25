using GestionCommerciale.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionCommerciale.Infrastructure.Persistence;

public sealed class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("Articles");
        builder.HasKey(x => x.ID);
        builder.Property(x => x.ID).ValueGeneratedOnAdd(); // SQL Server IDENTITY

        builder.Property(x => x.Reference).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Libelle).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CodeBarre).HasMaxLength(50);
        builder.Property(x => x.Unite).IsRequired().HasMaxLength(20);

        builder.Property(x => x.PrixAchatHT).HasPrecision(18, 3);
        builder.Property(x => x.PrixVenteHT).HasPrecision(18, 3);
        builder.Property(x => x.PrixVenteTTC).HasPrecision(18, 3);
        builder.Property(x => x.TauxTVA).HasPrecision(18, 3);

        builder.Property(x => x.DateCreation).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.Reference).IsUnique();
        builder.HasIndex(x => x.CodeBarre);
        builder.HasIndex(x => x.EstActif);

        // Les entités Category et Fournisseur existent déjà dans le modèle fourni.
        builder.HasOne(x => x.Category).WithMany()
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Fournisseur).WithMany()
            .HasForeignKey(x => x.FournisseurId).OnDelete(DeleteBehavior.Restrict);
    }
}