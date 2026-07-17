using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestionEntretiens.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Personnes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nom = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    Prenom = table.Column<string>(type: "text", nullable: true),
                    Telephone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personnes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Demandes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Poste = table.Column<string>(type: "text", nullable: true),
                    TypeEntretien = table.Column<int>(type: "integer", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    RecruteurId = table.Column<int>(type: "integer", nullable: false),
                    CandidatId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demandes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Demandes_Personnes_CandidatId",
                        column: x => x.CandidatId,
                        principalTable: "Personnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Demandes_Personnes_RecruteurId",
                        column: x => x.RecruteurId,
                        principalTable: "Personnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Creneaux",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateDebut = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Disponible = table.Column<bool>(type: "boolean", nullable: false),
                    RecruteurId = table.Column<int>(type: "integer", nullable: false),
                    DemandeEntretienId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Creneaux", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Creneaux_Demandes_DemandeEntretienId",
                        column: x => x.DemandeEntretienId,
                        principalTable: "Demandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Creneaux_Personnes_RecruteurId",
                        column: x => x.RecruteurId,
                        principalTable: "Personnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Entretiens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateHeure = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LieuOuLien = table.Column<string>(type: "text", nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    Modalite = table.Column<int>(type: "integer", nullable: false),
                    DemandeEntretienId = table.Column<int>(type: "integer", nullable: false),
                    CandidatId = table.Column<int>(type: "integer", nullable: false),
                    RecruteurId = table.Column<int>(type: "integer", nullable: false),
                    CreneauId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entretiens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entretiens_Creneaux_CreneauId",
                        column: x => x.CreneauId,
                        principalTable: "Creneaux",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entretiens_Demandes_DemandeEntretienId",
                        column: x => x.DemandeEntretienId,
                        principalTable: "Demandes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entretiens_Personnes_CandidatId",
                        column: x => x.CandidatId,
                        principalTable: "Personnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entretiens_Personnes_RecruteurId",
                        column: x => x.RecruteurId,
                        principalTable: "Personnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Note = table.Column<int>(type: "integer", nullable: false),
                    Commentaire = table.Column<string>(type: "text", nullable: true),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    DateSaisie = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntretienId = table.Column<int>(type: "integer", nullable: false),
                    AuteurId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Entretiens_EntretienId",
                        column: x => x.EntretienId,
                        principalTable: "Entretiens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Personnes_AuteurId",
                        column: x => x.AuteurId,
                        principalTable: "Personnes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Creneaux_DemandeEntretienId",
                table: "Creneaux",
                column: "DemandeEntretienId");

            migrationBuilder.CreateIndex(
                name: "IX_Creneaux_RecruteurId",
                table: "Creneaux",
                column: "RecruteurId");

            migrationBuilder.CreateIndex(
                name: "IX_Demandes_CandidatId",
                table: "Demandes",
                column: "CandidatId");

            migrationBuilder.CreateIndex(
                name: "IX_Demandes_RecruteurId",
                table: "Demandes",
                column: "RecruteurId");

            migrationBuilder.CreateIndex(
                name: "IX_Entretiens_CandidatId",
                table: "Entretiens",
                column: "CandidatId");

            migrationBuilder.CreateIndex(
                name: "IX_Entretiens_CreneauId",
                table: "Entretiens",
                column: "CreneauId");

            migrationBuilder.CreateIndex(
                name: "IX_Entretiens_DemandeEntretienId",
                table: "Entretiens",
                column: "DemandeEntretienId");

            migrationBuilder.CreateIndex(
                name: "IX_Entretiens_RecruteurId",
                table: "Entretiens",
                column: "RecruteurId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_AuteurId",
                table: "Feedbacks",
                column: "AuteurId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_EntretienId",
                table: "Feedbacks",
                column: "EntretienId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "Entretiens");

            migrationBuilder.DropTable(
                name: "Creneaux");

            migrationBuilder.DropTable(
                name: "Demandes");

            migrationBuilder.DropTable(
                name: "Personnes");
        }
    }
}
