using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisHistoryDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Assets",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Assets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Assets",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "Assets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProfileSyncUtc",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderSymbol",
                table: "Assets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Assets",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Assets",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnalysisBatches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedPattern = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "AssetCandleSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Interval = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCandleSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetCandleSnapshots_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetQuoteSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    DayVariationPct = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetQuoteSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetQuoteSnapshots_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisRuns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AnalysisBatchId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RequestedPattern = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RawPayload = table.Column<string>(type: "nvarchar(max)", maxLength: 32000, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisRuns_AnalysisBatches_AnalysisBatchId",
                        column: x => x.AnalysisBatchId,
                        principalTable: "AnalysisBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AnalysisRuns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalysisRuns_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DecisionSignals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AnalysisRunId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    IsActionable = table.Column<bool>(type: "bit", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    HorizonDays = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionSignals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecisionSignals_AnalysisRuns_AnalysisRunId",
                        column: x => x.AnalysisRunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AnalysisRunId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ModelStatus = table.Column<int>(type: "int", nullable: false),
                    ModelMessage = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Precision = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    F1 = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    RocAuc = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    PositiveSamples = table.Column<int>(type: "int", nullable: true),
                    SelectedThreshold = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelSnapshots_AnalysisRuns_AnalysisRunId",
                        column: x => x.AnalysisRunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatternAssessments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AnalysisRunId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Pattern = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Probability = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    NecklinePrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    TargetPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    InvalidationPrice = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true),
                    FirstPeakAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecondPeakAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatternAssessments_AnalysisRuns_AnalysisRunId",
                        column: x => x.AnalysisRunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisBatches_UserId_RequestedAtUtc",
                table: "AnalysisBatches",
                columns: new[] { "UserId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_AnalysisBatchId",
                table: "AnalysisRuns",
                column: "AnalysisBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_AssetId",
                table: "AnalysisRuns",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisRuns_UserId_StartedAtUtc",
                table: "AnalysisRuns",
                columns: new[] { "UserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetCandleSnapshots_AssetId_Interval_TimestampUtc",
                table: "AssetCandleSnapshots",
                columns: new[] { "AssetId", "Interval", "TimestampUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetQuoteSnapshots_AssetId_AsOfUtc",
                table: "AssetQuoteSnapshots",
                columns: new[] { "AssetId", "AsOfUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DecisionSignals_AnalysisRunId",
                table: "DecisionSignals",
                column: "AnalysisRunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelSnapshots_AnalysisRunId",
                table: "ModelSnapshots",
                column: "AnalysisRunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatternAssessments_AnalysisRunId_IsPrimary",
                table: "PatternAssessments",
                columns: new[] { "AnalysisRunId", "IsPrimary" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetCandleSnapshots");

            migrationBuilder.DropTable(
                name: "AssetQuoteSnapshots");

            migrationBuilder.DropTable(
                name: "DecisionSignals");

            migrationBuilder.DropTable(
                name: "ModelSnapshots");

            migrationBuilder.DropTable(
                name: "PatternAssessments");

            migrationBuilder.DropTable(
                name: "AnalysisRuns");

            migrationBuilder.DropTable(
                name: "AnalysisBatches");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LastProfileSyncUtc",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ProviderSymbol",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Sector",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Assets");
        }
    }
}
