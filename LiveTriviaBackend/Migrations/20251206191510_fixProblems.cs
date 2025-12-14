using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace LiveTriviaBackend.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class fixProblems : Migration
    {
        /// <inheritdoc />

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        ALTER TABLE "PlayerAnswers"
        ADD COLUMN IF NOT EXISTS "Score" double precision NOT NULL DEFAULT 0.0;
    """);

            migrationBuilder.Sql("""
        ALTER TABLE "PlayerAnswers"
        ADD COLUMN IF NOT EXISTS "TimeLeft" integer NOT NULL DEFAULT 0;
    """);

            migrationBuilder.Sql("""
        ALTER TABLE "Games"
        ADD COLUMN IF NOT EXISTS "CategoryVotes" text NOT NULL DEFAULT '';
    """);

            migrationBuilder.Sql("""
        ALTER TABLE "Games"
        ADD COLUMN IF NOT EXISTS "PlayerVotes" text NOT NULL DEFAULT '';
    """);
        }


        /// <inheritdoc />

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""ALTER TABLE "PlayerAnswers" DROP COLUMN IF EXISTS "Score";""");
            migrationBuilder.Sql("""ALTER TABLE "PlayerAnswers" DROP COLUMN IF EXISTS "TimeLeft";""");
            migrationBuilder.Sql("""ALTER TABLE "Games" DROP COLUMN IF EXISTS "CategoryVotes";""");
            migrationBuilder.Sql("""ALTER TABLE "Games" DROP COLUMN IF EXISTS "PlayerVotes";""");
        }

    }
}
