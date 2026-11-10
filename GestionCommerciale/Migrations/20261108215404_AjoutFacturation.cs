using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class AjoutFacturation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_Clients_ClientId",
                table: "BonsLivraison");

            migrationBuilder.DropColumn(
                name: "CodePostal",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "Fax",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "Gouvernorat",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "PrefixeBonCommande",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "PrefixeBonLivraison",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "PrefixeDevis",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "PrefixeFacture",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "ProchainNumeroBonCommande",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "ProchainNumeroBonLivraison",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "ProchainNumeroDevis",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "ProchainNumeroFacture",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "SiteWeb",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "Ville",
                table: "ParametresSociete");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ParametresSociete",
                newName: "ID");

            migrationBuilder.AlterColumn<string>(
                name: "RaisonSociale",
                table: "ParametresSociete",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "MatriculeFiscale",
                table: "ParametresSociete",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LogoPath",
                table: "ParametresSociete",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ParametresSociete",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Adresse",
                table: "ParametresSociete",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxFodec",
                table: "ParametresSociete",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxRetenueSource",
                table: "ParametresSociete",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "BonsLivraison",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "Factures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateEcheance = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    BonLivraisonId = table.Column<int>(type: "int", nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    RemiseGlobalePourcent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalHt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontantFodec = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTva = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TimbreFiscal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTtc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RetenueSourceApplicable = table.Column<bool>(type: "bit", nullable: false),
                    MontantRetenueSource = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAPayer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Factures_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SequencesDocument",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeDocument = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Exercice = table.Column<int>(type: "int", nullable: false),
                    DernierNumero = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SequencesDocument", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TauxTva",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Libelle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Taux = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TauxTva", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LignesFacture",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactureId = table.Column<int>(type: "int", nullable: false),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantite = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrixUnitaireHt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemisePourcent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TauxTva = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalHt = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LignesFacture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LignesFacture_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LignesFacture_Factures_FactureId",
                        column: x => x.FactureId,
                        principalTable: "Factures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TauxTva",
                columns: new[] { "ID", "Actif", "Libelle", "Taux" },
                values: new object[,]
                {
                    { 1, true, "Exonéré (0 %)", 0m },
                    { 2, true, "TVA 7 %", 7m },
                    { 3, true, "TVA 13 %", 13m },
                    { 4, true, "TVA 19 %", 19m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Factures_ClientId",
                table: "Factures",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_Numero",
                table: "Factures",
                column: "Numero",
                unique: true,
                filter: "[Numero] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LignesFacture_ArticleId",
                table: "LignesFacture",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_LignesFacture_FactureId",
                table: "LignesFacture",
                column: "FactureId");

            migrationBuilder.CreateIndex(
                name: "IX_SequencesDocument_TypeDocument_Exercice",
                table: "SequencesDocument",
                columns: new[] { "TypeDocument", "Exercice" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_Clients_ClientId",
                table: "BonsLivraison",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BonsLivraison_Clients_ClientId",
                table: "BonsLivraison");

            migrationBuilder.DropTable(
                name: "LignesFacture");

            migrationBuilder.DropTable(
                name: "SequencesDocument");

            migrationBuilder.DropTable(
                name: "TauxTva");

            migrationBuilder.DropTable(
                name: "Factures");

            migrationBuilder.DropColumn(
                name: "TauxFodec",
                table: "ParametresSociete");

            migrationBuilder.DropColumn(
                name: "TauxRetenueSource",
                table: "ParametresSociete");

            migrationBuilder.RenameColumn(
                name: "ID",
                table: "ParametresSociete",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "RaisonSociale",
                table: "ParametresSociete",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "MatriculeFiscale",
                table: "ParametresSociete",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LogoPath",
                table: "ParametresSociete",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ParametresSociete",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Adresse",
                table: "ParametresSociete",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodePostal",
                table: "ParametresSociete",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fax",
                table: "ParametresSociete",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gouvernorat",
                table: "ParametresSociete",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrefixeBonCommande",
                table: "ParametresSociete",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrefixeBonLivraison",
                table: "ParametresSociete",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrefixeDevis",
                table: "ParametresSociete",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrefixeFacture",
                table: "ParametresSociete",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProchainNumeroBonCommande",
                table: "ParametresSociete",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProchainNumeroBonLivraison",
                table: "ParametresSociete",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProchainNumeroDevis",
                table: "ParametresSociete",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProchainNumeroFacture",
                table: "ParametresSociete",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SiteWeb",
                table: "ParametresSociete",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ville",
                table: "ParametresSociete",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "BonsLivraison",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BonsLivraison_Clients_ClientId",
                table: "BonsLivraison",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
