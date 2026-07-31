using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionEntretiens.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AjoutOtpReinitialisation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeReinitialisation",
                table: "Personnes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationCode",
                table: "Personnes",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TentativesCode",
                table: "Personnes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeReinitialisation",
                table: "Personnes");

            migrationBuilder.DropColumn(
                name: "ExpirationCode",
                table: "Personnes");

            migrationBuilder.DropColumn(
                name: "TentativesCode",
                table: "Personnes");
        }
    }
}
