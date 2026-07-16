using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddFundamentalScoringPolicyVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundamentalScoringPolicyVersions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MinimumCategoriesRequiredFloor = table.Column<int>(type: "int", nullable: false),
                    MinimumCategoriesRequiredCeiling = table.Column<int>(type: "int", nullable: false),
                    MinimumCategoriesRequiredDefault = table.Column<int>(type: "int", nullable: false),
                    MinimumSectorSampleSize = table.Column<int>(type: "int", nullable: false),
                    CoveragePenaltySupported = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundamentalScoringPolicyVersions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundamentalScoringPolicyVersions_IsActive_ActivatedAtUtc",
                table: "FundamentalScoringPolicyVersions",
                columns: new[] { "IsActive", "ActivatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundamentalScoringPolicyVersions");
        }
    }
}
