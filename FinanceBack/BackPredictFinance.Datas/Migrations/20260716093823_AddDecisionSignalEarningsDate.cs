using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionSignalEarningsDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EarningsDateUtc",
                table: "DecisionSignals",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EarningsDateUtc",
                table: "DecisionSignals");
        }
    }
}
