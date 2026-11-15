using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class AjoutCodeClientEtModeFacturation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandesClient_Clients_ClientId",
                table: "CommandesClient");

            migrationBuilder.AddColumn<decimal>(
                name: "TauxTVA",
                table: "LignesCommandeClient",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCommande",
                table: "CommandesClient",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Commentaire",
                table: "CommandesClient",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "CommandesClient",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantHTBrut",
                table: "CommandesClient",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantRemiseGlobale",
                table: "CommandesClient",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxRemiseGlobale",
                table: "CommandesClient",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Clients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ModeFacturation",
                table: "Clients",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Remplit le code des clients existants (CLI-00001, CLI-00002, ...)
            // AVANT de créer l'index unique, sinon tous les codes vides seraient en doublon.
            // EXEC(...) : la colonne Code vient d'être ajoutée, le SQL doit être compilé après coup.
            migrationBuilder.Sql(@"
                EXEC(N'UPDATE Clients
                       SET Code = ''CLI-'' + RIGHT(''00000'' + CAST(Id AS VARCHAR(10)), 5)
                       WHERE Code = '''' OR Code IS NULL');");

            migrationBuilder.CreateIndex(
                name: "IX_CommandesClient_NumeroCommande",
                table: "CommandesClient",
                column: "NumeroCommande",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Code",
                table: "Clients",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CommandesClient_Clients_ClientId",
                table: "CommandesClient",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommandesClient_Clients_ClientId",
                table: "CommandesClient");

            migrationBuilder.DropIndex(
                name: "IX_CommandesClient_NumeroCommande",
                table: "CommandesClient");

            migrationBuilder.DropIndex(
                name: "IX_Clients_Code",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "TauxTVA",
                table: "LignesCommandeClient");

            migrationBuilder.DropColumn(
                name: "MontantHTBrut",
                table: "CommandesClient");

            migrationBuilder.DropColumn(
                name: "MontantRemiseGlobale",
                table: "CommandesClient");

            migrationBuilder.DropColumn(
                name: "TauxRemiseGlobale",
                table: "CommandesClient");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ModeFacturation",
                table: "Clients");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCommande",
                table: "CommandesClient",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Commentaire",
                table: "CommandesClient",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "CommandesClient",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_CommandesClient_Clients_ClientId",
                table: "CommandesClient",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");
        }
    }
}
