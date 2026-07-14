using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignalOutcomes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AnalysisRunId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PatternAssessmentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DecisionSignalId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    EvaluationWindowDays = table.Column<int>(type: "int", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstHitAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PolicyVersion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ConfidenceLabel = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignalOutcomes_AnalysisRuns_AnalysisRunId",
                        column: x => x.AnalysisRunId,
                        principalTable: "AnalysisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SignalOutcomes_DecisionSignals_DecisionSignalId",
                        column: x => x.DecisionSignalId,
                        principalTable: "DecisionSignals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignalOutcomes_PatternAssessments_PatternAssessmentId",
                        column: x => x.PatternAssessmentId,
                        principalTable: "PatternAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutcomes_AnalysisRunId",
                table: "SignalOutcomes",
                column: "AnalysisRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutcomes_DecisionSignalId",
                table: "SignalOutcomes",
                column: "DecisionSignalId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutcomes_EvaluatedAtUtc",
                table: "SignalOutcomes",
                column: "EvaluatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutcomes_PatternAssessmentId",
                table: "SignalOutcomes",
                column: "PatternAssessmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignalOutcomes");
        }
    }
}
