using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiveTriviaBackend.Migrations
{
    /// <inheritdoc />
    public partial class fixProblems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Score",
                table: "PlayerAnswers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TimeLeft",
                table: "PlayerAnswers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CategoryVotes",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlayerVotes",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "PlayerAnswers");

            migrationBuilder.DropColumn(
                name: "TimeLeft",
                table: "PlayerAnswers");

            migrationBuilder.DropColumn(
                name: "CategoryVotes",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PlayerVotes",
                table: "Games");
        }
    }
}
