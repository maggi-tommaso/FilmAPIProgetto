using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTmdbFieldsToFilm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackdropUrl",
                table: "Films",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LinguaOriginale",
                table: "Films",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PosterUrl",
                table: "Films",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TitoloOriginale",
                table: "Films",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TmdbId",
                table: "Films",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrailerUrl",
                table: "Films",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "VotoMedio",
                table: "Films",
                type: "decimal(65,30)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackdropUrl",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "LinguaOriginale",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "PosterUrl",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "TitoloOriginale",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "TrailerUrl",
                table: "Films");

            migrationBuilder.DropColumn(
                name: "VotoMedio",
                table: "Films");
        }
    }
}
