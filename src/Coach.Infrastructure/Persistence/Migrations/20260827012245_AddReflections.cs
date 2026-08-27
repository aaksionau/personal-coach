using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coach.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReflections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reflections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachSlug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reflections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reflections_Coaches_CoachSlug",
                        column: x => x.CoachSlug,
                        principalTable: "Coaches",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reflections_CoachSlug_CreatedAtUtc",
                table: "Reflections",
                columns: new[] { "CoachSlug", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reflections");
        }
    }
}
