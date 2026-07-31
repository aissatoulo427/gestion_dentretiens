using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEntretiens.Api.Data.Migrations
{
    /// <summary>
    /// Ajout du rôle Admin.
    ///
    /// Cette migration est VOLONTAIREMENT vide, et ce n'est pas une génération ratée.
    /// L'héritage est mappé en Table-Per-Hierarchy : un nouveau rôle n'ajoute ni table ni
    /// colonne, seulement une valeur possible du discriminateur — que le schéma ne décrit
    /// pas. La colonne fait déjà 21 caractères (dimensionnée pour « EvaluateurTechnique »),
    /// « Admin » y tient sans changement.
    ///
    /// Elle est conservée pour que l'historique des migrations reste aligné sur le snapshot
    /// du modèle. Sans elle, la migration suivante embarquerait ce changement.
    ///
    /// À ne pas confondre avec le cas où EF produit une migration vide à tort : cela arrive
    /// avec l'option --no-build, qui lui fait comparer un assembly périmé au snapshot.
    /// Toujours générer les migrations avec une compilation réelle.
    /// </summary>
    public partial class AjoutRoleAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
