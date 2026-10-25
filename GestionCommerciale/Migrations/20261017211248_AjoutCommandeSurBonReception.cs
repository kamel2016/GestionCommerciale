using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCommandeSurBonReception : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Categories_CategoryId",
                table: "Articles");

            migrationBuilder.AddColumn<int>(
                name: "CommandeFournisseurId",
                table: "BonsReception",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "Poids",
                table: "Articles",
                type: "decimal(10,3)",
                precision: 10,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonsReception_CommandeFournisseurId",
                table: "BonsReception",
                column: "CommandeFournisseurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Categories_CategoryId",
                table: "Articles",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsReception_CommandesFournisseur_CommandeFournisseurId",
                table: "BonsReception",
                column: "CommandeFournisseurId",
                principalTable: "CommandesFournisseur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Categories_CategoryId",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_BonsReception_CommandesFournisseur_CommandeFournisseurId",
                table: "BonsReception");

            migrationBuilder.DropIndex(
                name: "IX_BonsReception_CommandeFournisseurId",
                table: "BonsReception");

            migrationBuilder.DropColumn(
                name: "CommandeFournisseurId",
                table: "BonsReception");

            migrationBuilder.AlterColumn<decimal>(
                name: "Poids",
                table: "Articles",
                type: "decimal(18,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,3)",
                oldPrecision: 10,
                oldScale: 3,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Categories_CategoryId",
                table: "Articles",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
