using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bridge.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChallengeId",
                table: "Games",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: false),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    NumbersJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Challenges_Players_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_ChallengeId_PlayerId_Number",
                table: "Games",
                columns: new[] { "ChallengeId", "PlayerId", "Number" },
                unique: true,
                filter: "ChallengeId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_Code",
                table: "Challenges",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_CreatedById",
                table: "Challenges",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Challenges_ChallengeId",
                table: "Games",
                column: "ChallengeId",
                principalTable: "Challenges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Challenges_ChallengeId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Games_ChallengeId_PlayerId_Number",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ChallengeId",
                table: "Games");
        }
    }
}
