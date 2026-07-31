using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEntretiens.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefonteRolesRhEvaluateurTechnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Creneaux_Personnes_RecruteurId",
                table: "Creneaux");

            migrationBuilder.DropForeignKey(
                name: "FK_Demandes_Personnes_RecruteurId",
                table: "Demandes");

            migrationBuilder.RenameColumn(
                name: "RecruteurId",
                table: "Demandes",
                newName: "RhId");

            migrationBuilder.RenameIndex(
                name: "IX_Demandes_RecruteurId",
                table: "Demandes",
                newName: "IX_Demandes_RhId");

            migrationBuilder.RenameColumn(
                name: "RecruteurId",
                table: "Creneaux",
                newName: "EmployeId");

            migrationBuilder.RenameIndex(
                name: "IX_Creneaux_RecruteurId",
                table: "Creneaux",
                newName: "IX_Creneaux_EmployeId");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Personnes",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            // Ajouté à la main : EF ne migre jamais les VALEURS du discriminateur, seulement
            // le schéma. Sans cette ligne, les lignes existantes gardent 'Recruteur', une
            // valeur que le modèle ne connaît plus — les comptes deviendraient illisibles.
            // 'RH' (2 caractères) tient dans l'ancienne largeur comme dans la nouvelle.
            migrationBuilder.Sql(
                @"UPDATE ""Personnes"" SET ""Discriminator"" = 'RH' WHERE ""Discriminator"" = 'Recruteur';");

            migrationBuilder.AddForeignKey(
                name: "FK_Creneaux_Personnes_EmployeId",
                table: "Creneaux",
                column: "EmployeId",
                principalTable: "Personnes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Demandes_Personnes_RhId",
                table: "Demandes",
                column: "RhId",
                principalTable: "Personnes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Creneaux_Personnes_EmployeId",
                table: "Creneaux");

            migrationBuilder.DropForeignKey(
                name: "FK_Demandes_Personnes_RhId",
                table: "Demandes");

            migrationBuilder.RenameColumn(
                name: "RhId",
                table: "Demandes",
                newName: "RecruteurId");

            migrationBuilder.RenameIndex(
                name: "IX_Demandes_RhId",
                table: "Demandes",
                newName: "IX_Demandes_RecruteurId");

            migrationBuilder.RenameColumn(
                name: "EmployeId",
                table: "Creneaux",
                newName: "RecruteurId");

            migrationBuilder.RenameIndex(
                name: "IX_Creneaux_EmployeId",
                table: "Creneaux",
                newName: "IX_Creneaux_RecruteurId");

            // Symétrique du Up. Doit précéder le rétrécissement de la colonne ci-dessous.
            // ATTENTION : ce Down échoue s'il reste des lignes 'EvaluateurTechnique'
            // (19 caractères) — elles ne tiennent pas dans varchar(13). Il faut d'abord
            // décider quoi en faire : ce rôle n'existe pas dans le schéma d'avant, aucune
            // conversion automatique n'est possible sans perdre l'information.
            migrationBuilder.Sql(
                @"UPDATE ""Personnes"" SET ""Discriminator"" = 'Recruteur' WHERE ""Discriminator"" = 'RH';");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Personnes",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(21)",
                oldMaxLength: 21);

            migrationBuilder.AddForeignKey(
                name: "FK_Creneaux_Personnes_RecruteurId",
                table: "Creneaux",
                column: "RecruteurId",
                principalTable: "Personnes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Demandes_Personnes_RecruteurId",
                table: "Demandes",
                column: "RecruteurId",
                principalTable: "Personnes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
