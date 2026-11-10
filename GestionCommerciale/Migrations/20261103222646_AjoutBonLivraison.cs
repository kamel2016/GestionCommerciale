using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class AjoutBonLivraison : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuantiteLivree",
                table: "LignesCommandeClient",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BonsLivraison",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    CommandeClientId = table.Column<int>(type: "int", nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonsLivraison", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BonsLivraison_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BonsLivraison_CommandesClient_CommandeClientId",
                        column: x => x.CommandeClientId,
                        principalTable: "CommandesClient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LignesBonLivraison",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BonLivraisonId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    LigneCommandeClientId = table.Column<int>(type: "int", nullable: true),
                    Quantite = table.Column<int>(type: "int", nullable: false),
                    PrixUnitaire = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesBonLivraison", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LignesBonLivraison_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LignesBonLivraison_BonsLivraison_BonLivraisonId",
                        column: x => x.BonLivraisonId,
                        principalTable: "BonsLivraison",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LignesBonLivraison_LignesCommandeClient_LigneCommandeClientId",
                        column: x => x.LigneCommandeClientId,
                        principalTable: "LignesCommandeClient",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonsLivraison_ClientId",
                table: "BonsLivraison",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsLivraison_CommandeClientId",
                table: "BonsLivraison",
                column: "CommandeClientId");

            migrationBuilder.CreateIndex(
                name: "IX_BonsLivraison_Numero",
                table: "BonsLivraison",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LignesBonLivraison_ArticleId",
                table: "LignesBonLivraison",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesBonLivraison_BonLivraisonId",
                table: "LignesBonLivraison",
                column: "BonLivraisonId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesBonLivraison_LigneCommandeClientId",
                table: "LignesBonLivraison",
                column: "LigneCommandeClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LignesBonLivraison");

            migrationBuilder.DropTable(
                name: "BonsLivraison");

            migrationBuilder.DropColumn(
                name: "QuantiteLivree",
                table: "LignesCommandeClient");
        }
    }
}
