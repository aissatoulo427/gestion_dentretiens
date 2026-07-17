using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEntretiens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AjoutMotDePasse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotDePasse",
                table: "Personnes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MotDePasse",
                table: "Personnes");
        }
    }
}
