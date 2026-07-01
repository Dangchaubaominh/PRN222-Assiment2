using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RagChatbot.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningSessionsSummariesQuizzes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_UserId_SubjectId",
                table: "ChatMessages");

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "ChatMessages",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    KeyPoints = table.Column<string>(type: "text", nullable: false),
                    LearningObjectives = table.Column<string>(type: "text", nullable: false),
                    ImportantTerms = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentSummaries_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LearningActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningActivities", x => x.Id);
                });



            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SessionId",
                table: "ChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_UserId_SubjectId_SessionId",
                table: "ChatMessages",
                columns: new[] { "UserId", "SubjectId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatSessions_UserId_SubjectId",
                table: "ChatSessions",
                columns: new[] { "UserId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentSummaries_DocumentId",
                table: "DocumentSummaries",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningActivities_UserId_SubjectId_ActivityType",
                table: "LearningActivities",
                columns: new[] { "UserId", "SubjectId", "ActivityType" });



            migrationBuilder.Sql("""
                INSERT INTO "ChatSessions" ("SubjectId", "UserId", "Title", "CreatedAt", "UpdatedAt")
                SELECT "SubjectId", "UserId", 'Phiên chat cũ', MIN("CreatedAt"), MAX("CreatedAt")
                FROM "ChatMessages"
                WHERE "SessionId" IS NULL
                GROUP BY "SubjectId", "UserId";

                UPDATE "ChatMessages" AS m
                SET "SessionId" = s."Id"
                FROM "ChatSessions" AS s
                WHERE m."SessionId" IS NULL
                  AND m."SubjectId" = s."SubjectId"
                  AND m."UserId" = s."UserId";
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_ChatSessions_SessionId",
                table: "ChatMessages",
                column: "SessionId",
                principalTable: "ChatSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_ChatSessions_SessionId",
                table: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatSessions");

            migrationBuilder.DropTable(
                name: "DocumentSummaries");

            migrationBuilder.DropTable(
                name: "LearningActivities");



            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_SessionId",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_UserId_SubjectId_SessionId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "ChatMessages");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_UserId_SubjectId",
                table: "ChatMessages",
                columns: new[] { "UserId", "SubjectId" });
        }
    }
}
