using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthUsersAndTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Utenti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cognome = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Provider = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "local")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatoIlUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UltimoAccessoUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utenti", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BigliettiUtente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    Codice = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AcquistatoIlUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ConvalidatoIlUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsConvalidato = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BigliettiUtente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BigliettiUtente_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SessioniAccesso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UtenteId = table.Column<int>(type: "int", nullable: false),
                    CreatoIlUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ScadeIlUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevocatoIlUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessioniAccesso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessioniAccesso_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BigliettiUtente_Codice",
                table: "BigliettiUtente",
                column: "Codice",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BigliettiUtente_IsConvalidato",
                table: "BigliettiUtente",
                column: "IsConvalidato");

            migrationBuilder.CreateIndex(
                name: "IX_BigliettiUtente_UtenteId",
                table: "BigliettiUtente",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_SessioniAccesso_ScadeIlUtc",
                table: "SessioniAccesso",
                column: "ScadeIlUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SessioniAccesso_UtenteId",
                table: "SessioniAccesso",
                column: "UtenteId");

            migrationBuilder.CreateIndex(
                name: "IX_Utenti_Email",
                table: "Utenti",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utenti_Username",
                table: "Utenti",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BigliettiUtente");

            migrationBuilder.DropTable(
                name: "SessioniAccesso");

            migrationBuilder.DropTable(
                name: "Utenti");
        }
    }
}
