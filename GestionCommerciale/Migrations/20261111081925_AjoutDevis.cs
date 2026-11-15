using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class AjoutDevis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TypeDocument",
                table: "SequencesDocument",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "Devis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    DevisParentId = table.Column<int>(type: "int", nullable: true),
                    DateDevis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateValidite = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    NomProspect = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TelephoneProspect = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EmailProspect = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AdresseProspect = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MatriculeFiscalProspect = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    Conditions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MontantBrutHT = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantRemise = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantHT = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantTVA = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantTTC = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    CommandeClientId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devis_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devis_CommandesClient_CommandeClientId",
                        column: x => x.CommandeClientId,
                        principalTable: "CommandesClient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devis_Devis_DevisParentId",
                        column: x => x.DevisParentId,
                        principalTable: "Devis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LigneDevis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevisId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaireHT = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    TauxRemise = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TauxTVA = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MontantBrutHT = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantRemise = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantHT = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantTVA = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    MontantTTC = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LigneDevis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LigneDevis_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LigneDevis_Devis_DevisId",
                        column: x => x.DevisId,
                        principalTable: "Devis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devis_ClientId",
                table: "Devis",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Devis_CommandeClientId",
                table: "Devis",
                column: "CommandeClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Devis_DevisParentId",
                table: "Devis",
                column: "DevisParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Devis_Numero_Version",
                table: "Devis",
                columns: new[] { "Numero", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LigneDevis_ArticleId",
                table: "LigneDevis",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_LigneDevis_DevisId",
                table: "LigneDevis",
                column: "DevisId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LigneDevis");

            migrationBuilder.DropTable(
                name: "Devis");

            migrationBuilder.AlterColumn<string>(
                name: "TypeDocument",
                table: "SequencesDocument",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);
        }
    }
}
