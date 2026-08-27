using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDueDateNudges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DueDateNudges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    NudgeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDateAtNudge = table.Column<DateOnly>(type: "date", nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DueDateNudges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DueDateNudges_ActionItems_ActionItemId",
                        column: x => x.ActionItemId,
                        principalTable: "ActionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DueDateNudges_ActionItemId_NudgeDate",
                table: "DueDateNudges",
                columns: new[] { "ActionItemId", "NudgeDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DueDateNudges");
        }
    }
}
