using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailConfermata",
                table: "Utenti",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "EmailTokenConferma",
                table: "Utenti",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailTokenScadeIlUtc",
                table: "Utenti",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfermata",
                table: "Utenti");

            migrationBuilder.DropColumn(
                name: "EmailTokenConferma",
                table: "Utenti");

            migrationBuilder.DropColumn(
                name: "EmailTokenScadeIlUtc",
                table: "Utenti");
        }
    }
}
