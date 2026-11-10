using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class RetenueALaSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SoumisRetenueSource",
                table: "Fournisseurs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxRetenueSource",
                table: "Fournisseurs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantRetenueSource",
                table: "BonsReception",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxRetenueSource",
                table: "BonsReception",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoumisRetenueSource",
                table: "Fournisseurs");

            migrationBuilder.DropColumn(
                name: "TauxRetenueSource",
                table: "Fournisseurs");

            migrationBuilder.DropColumn(
                name: "MontantRetenueSource",
                table: "BonsReception");

            migrationBuilder.DropColumn(
                name: "TauxRetenueSource",
                table: "BonsReception");
        }
    }
}
