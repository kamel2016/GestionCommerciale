using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class AjoutMouvementStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MouvementsStock_ArticleId",
                table: "MouvementsStock");

            migrationBuilder.RenameColumn(
                name: "StockApres",
                table: "MouvementsStock",
                newName: "QuantiteAvant");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "MouvementsStock",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<decimal>(
                name: "CoutUnitaire",
                table: "MouvementsStock",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "QuantiteApres",
                table: "MouvementsStock",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MouvementsStock",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceId",
                table: "MouvementsStock",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "MouvementsStock",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CMUP",
                table: "Articles",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DeviseAchatId",
                table: "Articles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GereNumeroSerie",
                table: "Articles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PrixAchatDevise",
                table: "Articles",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Articles",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Devises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbole = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    EstDevisePrincipale = table.Column<bool>(type: "bit", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TauxChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviseId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Taux = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TauxChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TauxChanges_Devises_DeviseId",
                        column: x => x.DeviseId,
                        principalTable: "Devises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Devises",
                columns: new[] { "Id", "Actif", "Code", "DateCreation", "DateModification", "EstDevisePrincipale", "Libelle", "Symbole" },
                values: new object[,]
                {
                    { 1, true, "TND", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Dinar tunisien", "د.ت" },
                    { 2, true, "EUR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, "Euro", "€" },
                    { 3, true, "USD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, "Dollar américain", "$" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsStock_ArticleId_Date",
                table: "MouvementsStock",
                columns: new[] { "ArticleId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_DeviseAchatId",
                table: "Articles",
                column: "DeviseAchatId");

            migrationBuilder.CreateIndex(
                name: "IX_Devises_Code",
                table: "Devises",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devises_EstDevisePrincipale",
                table: "Devises",
                column: "EstDevisePrincipale",
                unique: true,
                filter: "[EstDevisePrincipale] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TauxChanges_DeviseId_Date",
                table: "TauxChanges",
                columns: new[] { "DeviseId", "Date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Devises_DeviseAchatId",
                table: "Articles",
                column: "DeviseAchatId",
                principalTable: "Devises",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Devises_DeviseAchatId",
                table: "Articles");

            migrationBuilder.DropTable(
                name: "TauxChanges");

            migrationBuilder.DropTable(
                name: "Devises");

            migrationBuilder.DropIndex(
                name: "IX_MouvementsStock_ArticleId_Date",
                table: "MouvementsStock");

            migrationBuilder.DropIndex(
                name: "IX_Articles_DeviseAchatId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "CoutUnitaire",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "QuantiteApres",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "MouvementsStock");

            migrationBuilder.DropColumn(
                name: "CMUP",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "DeviseAchatId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "GereNumeroSerie",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "PrixAchatDevise",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Articles");

            migrationBuilder.RenameColumn(
                name: "QuantiteAvant",
                table: "MouvementsStock",
                newName: "StockApres");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "MouvementsStock",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_MouvementsStock_ArticleId",
                table: "MouvementsStock",
                column: "ArticleId");
        }
    }
}
