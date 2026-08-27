using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coach.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGarminDailyMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GarminDailyMetrics",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Steps = table.Column<int>(type: "integer", nullable: true),
                    StepGoal = table.Column<int>(type: "integer", nullable: true),
                    RestingHeartRateBpm = table.Column<int>(type: "integer", nullable: true),
                    TotalSleepMinutes = table.Column<int>(type: "integer", nullable: true),
                    DeepSleepMinutes = table.Column<int>(type: "integer", nullable: true),
                    RemSleepMinutes = table.Column<int>(type: "integer", nullable: true),
                    LightSleepMinutes = table.Column<int>(type: "integer", nullable: true),
                    AwakeMinutes = table.Column<int>(type: "integer", nullable: true),
                    SleepScore = table.Column<int>(type: "integer", nullable: true),
                    AverageStressLevel = table.Column<int>(type: "integer", nullable: true),
                    MaxStressLevel = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryHigh = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryLow = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryCharged = table.Column<int>(type: "integer", nullable: true),
                    BodyBatteryDrained = table.Column<int>(type: "integer", nullable: true),
                    ModerateIntensityMinutes = table.Column<int>(type: "integer", nullable: true),
                    VigorousIntensityMinutes = table.Column<int>(type: "integer", nullable: true),
                    IngestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GarminDailyMetrics", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "GarminActivitySummaries",
                columns: table => new
                {
                    ActivityId = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ActivityType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    DistanceMeters = table.Column<int>(type: "integer", nullable: true),
                    Calories = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GarminActivitySummaries", x => x.ActivityId);
                    table.ForeignKey(
                        name: "FK_GarminActivitySummaries_GarminDailyMetrics_Date",
                        column: x => x.Date,
                        principalTable: "GarminDailyMetrics",
                        principalColumn: "Date",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GarminActivitySummaries_Date",
                table: "GarminActivitySummaries",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GarminActivitySummaries");

            migrationBuilder.DropTable(
                name: "GarminDailyMetrics");
        }
    }
}
