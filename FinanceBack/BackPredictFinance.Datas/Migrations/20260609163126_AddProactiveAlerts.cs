using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddProactiveAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AlertDayKeyUtc",
                table: "UserNotifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlertTrigger",
                table: "UserNotifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgressStatus",
                table: "PatternAssessments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AlertDataStaleEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AlertLevelCrossedEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "AlertPatternStateChangeEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_TargetEntityId_AlertTrigger_AlertDayKeyUtc",
                table: "UserNotifications",
                columns: new[] { "UserId", "TargetEntityId", "AlertTrigger", "AlertDayKeyUtc" },
                unique: true,
                filter: "[AlertTrigger] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_UserId_TargetEntityId_AlertTrigger_AlertDayKeyUtc",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "AlertDayKeyUtc",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "AlertTrigger",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "ProgressStatus",
                table: "PatternAssessments");

            migrationBuilder.DropColumn(
                name: "AlertDataStaleEnabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AlertLevelCrossedEnabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AlertPatternStateChangeEnabled",
                table: "AspNetUsers");
        }
    }
}
