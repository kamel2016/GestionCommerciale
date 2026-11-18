using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionCommerciale.Migrations
{
    /// <inheritdoc />
    public partial class UniciteCodeBarre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_CodeBarre",
                table: "Articles");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CodeBarre",
                table: "Articles",
                column: "CodeBarre",
                unique: true,
                filter: "[CodeBarre] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_CodeBarre",
                table: "Articles");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CodeBarre",
                table: "Articles",
                column: "CodeBarre");
        }
    }
}
