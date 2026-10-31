using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bridge.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    RequestJson = table.Column<string>(type: "TEXT", nullable: false),
                    Contract = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    Declarer = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    OurContract = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeclarerTricks = table.Column<int>(type: "INTEGER", nullable: false),
                    Made = table.Column<bool>(type: "INTEGER", nullable: false),
                    ScoreNorthSouth = table.Column<int>(type: "INTEGER", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_PlayerId_FinishedAt",
                table: "Games",
                columns: new[] { "PlayerId", "FinishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
