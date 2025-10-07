using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LiveTriviaBackend.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeDatabaseSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentAnswerIndexes",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "GameRoomId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Players",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Questions",
                table: "Games");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Questions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Questions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Players",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Games",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "Games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "Games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Games",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GamePlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameRoomId = table.Column<string>(type: "character varying(50)", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamePlayers_Games_GameRoomId",
                        column: x => x.GameRoomId,
                        principalTable: "Games",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GamePlayers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameQuestion",
                columns: table => new
                {
                    GameRoomId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameQuestion", x => new { x.GameRoomId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_GameQuestion_Games_GameRoomId",
                        column: x => x.GameRoomId,
                        principalTable: "Games",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameQuestion_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    GameRoomId = table.Column<string>(type: "character varying(50)", nullable: false),
                    SelectedAnswerIndexes = table.Column<string>(type: "text", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerAnswers_Games_GameRoomId",
                        column: x => x.GameRoomId,
                        principalTable: "Games",
                        principalColumn: "RoomId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerAnswers_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Category",
                table: "Questions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Category_Difficulty",
                table: "Questions",
                columns: new[] { "Category", "Difficulty" });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Difficulty",
                table: "Questions",
                column: "Difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Name",
                table: "Players",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Score",
                table: "Players",
                column: "Score");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CurrentQuestionIndex",
                table: "Games",
                column: "CurrentQuestionIndex");

            migrationBuilder.CreateIndex(
                name: "IX_Games_State",
                table: "Games",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_GameRoomId_PlayerId",
                table: "GamePlayers",
                columns: new[] { "GameRoomId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_PlayerId",
                table: "GamePlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameQuestion_QuestionId",
                table: "GameQuestion",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnswers_GameRoomId_PlayerId_QuestionId",
                table: "PlayerAnswers",
                columns: new[] { "GameRoomId", "PlayerId", "QuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnswers_PlayerId",
                table: "PlayerAnswers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAnswers_QuestionId",
                table: "PlayerAnswers",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GamePlayers");

            migrationBuilder.DropTable(
                name: "GameQuestion");

            migrationBuilder.DropTable(
                name: "PlayerAnswers");

            migrationBuilder.DropIndex(
                name: "IX_Questions_Category",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_Category_Difficulty",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_Difficulty",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Players_Name",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_Score",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Games_CurrentQuestionIndex",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_State",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "CurrentAnswerIndexes",
                table: "Players",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GameRoomId",
                table: "Players",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Players",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Questions",
                table: "Games",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
