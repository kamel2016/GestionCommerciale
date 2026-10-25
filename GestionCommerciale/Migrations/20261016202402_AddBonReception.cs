using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class AddBonReception : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonsReception",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroInterne = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FournisseurId = table.Column<int>(type: "int", nullable: false),
                    ReferenceFournisseur = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DateReception = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HeureReception = table.Column<TimeSpan>(type: "time", nullable: false),
                    MontantHT = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantTVA = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantTTC = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonsReception", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonsReception_Fournisseurs_FournisseurId",
                        column: x => x.FournisseurId,
                        principalTable: "Fournisseurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LignesBonReception",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BonReceptionId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesBonReception", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LignesBonReception_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LignesBonReception_BonsReception_BonReceptionId",
                        column: x => x.BonReceptionId,
                        principalTable: "BonsReception",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonsReception_FournisseurId",
                table: "BonsReception",
                column: "FournisseurId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesBonReception_ArticleId",
                table: "LignesBonReception",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesBonReception_BonReceptionId",
                table: "LignesBonReception",
                column: "BonReceptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LignesBonReception");

            migrationBuilder.DropTable(
                name: "BonsReception");
        }
    }
}
