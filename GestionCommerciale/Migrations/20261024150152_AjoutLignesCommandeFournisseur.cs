using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class AjoutLignesCommandeFournisseur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LigneCommandeFournisseur_Articles_ArticleId",
                table: "LigneCommandeFournisseur");

            migrationBuilder.DropForeignKey(
                name: "FK_LigneCommandeFournisseur_CommandesFournisseur_CommandeFournisseurId",
                table: "LigneCommandeFournisseur");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LigneCommandeFournisseur",
                table: "LigneCommandeFournisseur");

            migrationBuilder.RenameTable(
                name: "LigneCommandeFournisseur",
                newName: "LignesCommandeFournisseur");

            migrationBuilder.RenameIndex(
                name: "IX_LigneCommandeFournisseur_CommandeFournisseurId",
                table: "LignesCommandeFournisseur",
                newName: "IX_LignesCommandeFournisseur_CommandeFournisseurId");

            migrationBuilder.RenameIndex(
                name: "IX_LigneCommandeFournisseur_ArticleId",
                table: "LignesCommandeFournisseur",
                newName: "IX_LignesCommandeFournisseur_ArticleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LignesCommandeFournisseur",
                table: "LignesCommandeFournisseur",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LignesCommandeFournisseur_Articles_ArticleId",
                table: "LignesCommandeFournisseur",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LignesCommandeFournisseur_CommandesFournisseur_CommandeFournisseurId",
                table: "LignesCommandeFournisseur",
                column: "CommandeFournisseurId",
                principalTable: "CommandesFournisseur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LignesCommandeFournisseur_Articles_ArticleId",
                table: "LignesCommandeFournisseur");

            migrationBuilder.DropForeignKey(
                name: "FK_LignesCommandeFournisseur_CommandesFournisseur_CommandeFournisseurId",
                table: "LignesCommandeFournisseur");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LignesCommandeFournisseur",
                table: "LignesCommandeFournisseur");

            migrationBuilder.RenameTable(
                name: "LignesCommandeFournisseur",
                newName: "LigneCommandeFournisseur");

            migrationBuilder.RenameIndex(
                name: "IX_LignesCommandeFournisseur_CommandeFournisseurId",
                table: "LigneCommandeFournisseur",
                newName: "IX_LigneCommandeFournisseur_CommandeFournisseurId");

            migrationBuilder.RenameIndex(
                name: "IX_LignesCommandeFournisseur_ArticleId",
                table: "LigneCommandeFournisseur",
                newName: "IX_LigneCommandeFournisseur_ArticleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LigneCommandeFournisseur",
                table: "LigneCommandeFournisseur",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LigneCommandeFournisseur_Articles_ArticleId",
                table: "LigneCommandeFournisseur",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LigneCommandeFournisseur_CommandesFournisseur_CommandeFournisseurId",
                table: "LigneCommandeFournisseur",
                column: "CommandeFournisseurId",
                principalTable: "CommandesFournisseur",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
