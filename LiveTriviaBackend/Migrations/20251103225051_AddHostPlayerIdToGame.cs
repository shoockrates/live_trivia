using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace LiveTriviaBackend.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddHostPlayerIdToGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HostPlayerId",
                table: "Games",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_HostPlayerId",
                table: "Games",
                column: "HostPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Players_HostPlayerId",
                table: "Games",
                column: "HostPlayerId",
                principalTable: "Players",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Players_HostPlayerId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_HostPlayerId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "HostPlayerId",
                table: "Games");
        }
    }
}
