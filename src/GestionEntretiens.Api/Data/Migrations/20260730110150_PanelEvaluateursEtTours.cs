using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEntretiens.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PanelEvaluateursEtTours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Entretiens_Personnes_RecruteurId",
                table: "Entretiens");

            migrationBuilder.DropIndex(
                name: "IX_Entretiens_RecruteurId",
                table: "Entretiens");

            migrationBuilder.DropColumn(
                name: "TypeEntretien",
                table: "Demandes");

            // Volontairement PAS un RenameColumn : EF avait deviné un renommage parce que
            // les deux colonnes sont des int, mais leur contenu n'a rien à voir (des ids de
            // recruteur d'un côté, des valeurs d'enum de l'autre). On supprime et on recrée.
            migrationBuilder.DropColumn(
                name: "RecruteurId",
                table: "Entretiens");

            migrationBuilder.AddColumn<int>(
                name: "TypeEntretien",
                table: "Entretiens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TentativesCode",
                table: "Personnes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "EntretienEvaluateurs",
                columns: table => new
                {
                    EntretiensId = table.Column<int>(type: "integer", nullable: false),
                    EvaluateursId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntretienEvaluateurs", x => new { x.EntretiensId, x.EvaluateursId });
                    table.ForeignKey(
                        name: "FK_EntretienEvaluateurs_Entretiens_EntretiensId",
                        column: x => x.EntretiensId,
                        principalTable: "Entretiens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntretienEvaluateurs_Personnes_EvaluateursId",
                        column: x => x.EvaluateursId,
                        principalTable: "Personnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntretienEvaluateurs_EvaluateursId",
                table: "EntretienEvaluateurs",
                column: "EvaluateursId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntretienEvaluateurs");

            migrationBuilder.DropColumn(
                name: "TypeEntretien",
                table: "Entretiens");

            migrationBuilder.AddColumn<int>(
                name: "RecruteurId",
                table: "Entretiens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TentativesCode",
                table: "Personnes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TypeEntretien",
                table: "Demandes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Entretiens_RecruteurId",
                table: "Entretiens",
                column: "RecruteurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Entretiens_Personnes_RecruteurId",
                table: "Entretiens",
                column: "RecruteurId",
                principalTable: "Personnes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
