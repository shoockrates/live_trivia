using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveTriviaBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionsToGameDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameSettings_GameRoomId",
                table: "GameSettings");

            migrationBuilder.CreateIndex(
                name: "IX_GameSettings_GameRoomId",
                table: "GameSettings",
                column: "GameRoomId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameSettings_GameRoomId",
                table: "GameSettings");

            migrationBuilder.CreateIndex(
                name: "IX_GameSettings_GameRoomId",
                table: "GameSettings",
                column: "GameRoomId");
        }
    }
}
