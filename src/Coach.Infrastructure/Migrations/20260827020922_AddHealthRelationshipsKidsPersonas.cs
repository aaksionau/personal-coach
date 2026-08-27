using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Coach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthRelationshipsKidsPersonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Coaches",
                columns: new[] { "Slug", "DisplayName" },
                values: new object[,]
                {
                    { "health", "Health Coach" },
                    { "kids", "Kids Coach" },
                    { "relationships", "Relationships Coach" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Coaches",
                keyColumn: "Slug",
                keyValue: "health");

            migrationBuilder.DeleteData(
                table: "Coaches",
                keyColumn: "Slug",
                keyValue: "kids");

            migrationBuilder.DeleteData(
                table: "Coaches",
                keyColumn: "Slug",
                keyValue: "relationships");
        }
    }
}
