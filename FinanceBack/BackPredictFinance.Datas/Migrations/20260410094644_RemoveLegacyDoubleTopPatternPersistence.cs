using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyDoubleTopPatternPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalysisRuns_AnalysisBatches_AnalysisBatchId",
                table: "AnalysisRuns");

            migrationBuilder.DropTable(
                name: "AnalysisBatches");

            migrationBuilder.DropIndex(
                name: "IX_AnalysisRuns_AnalysisBatchId",
                table: "AnalysisRuns");

            migrationBuilder.DropColumn(
                name: "Pattern",
                table: "PatternAssessments");

            migrationBuilder.DropColumn(
                name: "AnalysisBatchId",
                table: "AnalysisRuns");

            migrationBuilder.DropColumn(
                name: "RequestedPattern",
                table: "AnalysisRuns");

            migrationBuilder.AddColumn<string>(
                name: "PatternId",
                table: "PatternAssessments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AssetPeaEligibilities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UniverseId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EligibilityStatus = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CheckedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicyVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReviewerNote = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetPeaEligibilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetPeaEligibilities_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetPeaEligibilities_AssetId_UniverseId",
                table: "AssetPeaEligibilities",
                columns: new[] { "AssetId", "UniverseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetPeaEligibilities_UniverseId_EligibilityStatus",
                table: "AssetPeaEligibilities",
                columns: new[] { "UniverseId", "EligibilityStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetPeaEligibilities");

            migrationBuilder.DropColumn(
                name: "PatternId",
                table: "PatternAssessments");

            migrationBuilder.AddColumn<int>(
                name: "Pattern",
                table: "PatternAssessments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AnalysisBatchId",
                table: "AnalysisRuns",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestedPattern",
                table: "AnalysisRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AnalysisBatches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedPattern = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisBatches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_AnalysisBatchId",
                table: "AnalysisRuns",
                column: "AnalysisBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisBatches_UserId_RequestedAtUtc",
                table: "AnalysisBatches",
                columns: new[] { "UserId", "RequestedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_AnalysisRuns_AnalysisBatches_AnalysisBatchId",
                table: "AnalysisRuns",
                column: "AnalysisBatchId",
                principalTable: "AnalysisBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
