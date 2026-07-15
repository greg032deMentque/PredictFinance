using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddFundamentalsAndScreenerPresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnalysisRuns_AssetId",
                table: "AnalysisRuns");

            migrationBuilder.CreateTable(
                name: "AssetFundamentalsSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MarketCap = table.Column<decimal>(type: "decimal(24,4)", precision: 24, scale: 4, nullable: true),
                    TrailingPE = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    DividendYield = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetFundamentalsSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetFundamentalsSnapshots_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserScreenerPresets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    QueryJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserScreenerPresets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserScreenerPresets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_AssetId_StartedAtUtc",
                table: "AnalysisRuns",
                columns: new[] { "AssetId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetFundamentalsSnapshots_AssetId_AsOfUtc",
                table: "AssetFundamentalsSnapshots",
                columns: new[] { "AssetId", "AsOfUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserScreenerPresets_UserId_IsDeleted",
                table: "UserScreenerPresets",
                columns: new[] { "UserId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetFundamentalsSnapshots");

            migrationBuilder.DropTable(
                name: "UserScreenerPresets");

            migrationBuilder.DropIndex(
                name: "IX_AnalysisRuns_AssetId_StartedAtUtc",
                table: "AnalysisRuns");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_AssetId",
                table: "AnalysisRuns",
                column: "AssetId");
        }
    }
}
