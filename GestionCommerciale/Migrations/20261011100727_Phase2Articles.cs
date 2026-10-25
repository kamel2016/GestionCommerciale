using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class Phase2Articles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Couleur",
                table: "Articles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Marque",
                table: "Articles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Modele",
                table: "Articles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Poids",
                table: "Articles",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Taille",
                table: "Articles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ArticleFournisseurs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    FournisseurId = table.Column<int>(type: "int", nullable: false),
                    PrixGrosHT = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PrixDetailHT = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    PrixPromoHT = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    RemiseMax = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DernierAchat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleFournisseurs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ArticleFournisseurs_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleFournisseurs_Fournisseurs_FournisseurId",
                        column: x => x.FournisseurId,
                        principalTable: "Fournisseurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MouvementsStock",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    StockApres = table.Column<int>(type: "int", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MouvementsStock", x => x.ID);
                    table.ForeignKey(
                        name: "FK_MouvementsStock_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleFournisseurs_ArticleId_FournisseurId",
                table: "ArticleFournisseurs",
                columns: new[] { "ArticleId", "FournisseurId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleFournisseurs_FournisseurId",
                table: "ArticleFournisseurs",
                column: "FournisseurId");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsStock_ArticleId",
                table: "MouvementsStock",
                column: "ArticleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleFournisseurs");

            migrationBuilder.DropTable(
                name: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "Couleur",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Marque",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Modele",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Poids",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Taille",
                table: "Articles");
        }
    }
}
