using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFilmPreferito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FilmPreferitoId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FilmPreferitoId",
                table: "Users",
                column: "FilmPreferitoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Films_FilmPreferitoId",
                table: "Users",
                column: "FilmPreferitoId",
                principalTable: "Films",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Films_FilmPreferitoId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_FilmPreferitoId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FilmPreferitoId",
                table: "Users");
        }
    }
}
