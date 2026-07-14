using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddWordingGovernanceAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParameterDictionaryEntries",
                columns: table => new
                {
                    ParameterId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CategoryCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayLabel = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RoleInCategory = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SimpleDefinition = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    HowToRead = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    WhyItMatters = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    LimitsOfInterpretation = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    WhatItSupports = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    WhatItDoesNotProve = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ImplicationWithoutPosition = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ImplicationWithPosition = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterDictionaryEntries", x => x.ParameterId);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationWordingVersions",
                columns: table => new
                {
                    WordingVersionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecommendationPolicyVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExplanationPolicyVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AffectedDomains = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationWordingVersions", x => x.WordingVersionId);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    NotificationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    TargetScreen = table.Column<int>(type: "int", nullable: true),
                    TargetEntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_UserNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationWordingScenarios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WordingVersionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScenarioCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecommendationKind = table.Column<int>(type: "int", nullable: false),
                    HoldingStatus = table.Column<int>(type: "int", nullable: false),
                    ActionVerbFamilyCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecommendationStrengthFamily = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TemplateSummary = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationWordingScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationWordingScenarios_RecommendationWordingVersions_WordingVersionId",
                        column: x => x.WordingVersionId,
                        principalTable: "RecommendationWordingVersions",
                        principalColumn: "WordingVersionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ParameterDictionaryEntries",
                columns: new[] { "ParameterId", "CategoryCode", "CreatedAtUtc", "DisplayLabel", "HowToRead", "ImplicationWithPosition", "ImplicationWithoutPosition", "IsActive", "IsPublished", "LimitsOfInterpretation", "RoleInCategory", "SimpleDefinition", "UpdatedAtUtc", "WhatItDoesNotProve", "WhatItSupports", "WhyItMatters" },
                values: new object[,]
                {
                    { "currentRatio", "liquidity", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Current ratio", "A higher value generally suggests more short-term balance-sheet flexibility, but an extreme value can also reflect idle assets.", "With a position, it can reduce some short-term stress concerns, but it does not replace portfolio discipline.", "Without a position, use it as a balance-sheet comfort signal, not as a trigger.", true, true, "It says little about long-term competitiveness and can differ strongly by industry.", "Indicate how short-term assets compare to short-term liabilities.", "Current ratio compares current assets to current liabilities.", null, "It does not prove profitability, attractive valuation, or a good entry point on its own.", "It supports a liquidity reading when short-term flexibility matters in the business context.", "It helps beginners understand whether the company appears able to absorb near-term obligations." },
                    { "debtToEquity", "debt", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Debt to equity", "A lower value generally indicates a lighter leverage profile, though normal levels differ by sector.", "With a position, it can frame how much leverage risk you are carrying, but it does not dictate the action by itself.", "Without a position, it can help filter out balance-sheet profiles that do not match your caution level.", true, true, "A single ratio cannot describe debt quality, refinancing profile, or cash generation by itself.", "Show how much debt stands against shareholder equity.", "Debt to equity compares the debt burden to the equity base.", null, "It does not prove that the company is safe in all scenarios or that the share is attractive now.", "It supports a leverage-risk reading when combined with profitability and liquidity data.", "It helps evaluate whether the company could become fragile if conditions deteriorate." },
                    { "dividendYield", "dividend", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Dividend yield", "A higher value can mean stronger income potential, but it can also reflect a falling share price or an unsustainable payout.", "With a position, it can explain part of the holding case, but it does not replace monitoring of trend, risk, and fundamentals.", "Without a position, it can clarify whether the stock fits an income objective, but not whether the timing is right.", true, true, "It must be read with payout sustainability and business quality, not as a standalone quality label.", "Relate the dividend distribution to the current share price.", "Dividend yield compares the annual dividend to the current market price.", null, "It does not prove valuation attractiveness, price support, or future dividend safety on its own.", "It supports an income-oriented reading when the payout looks sustainable.", "It helps beginners distinguish income characteristics from pure price expectations." },
                    { "operatingMargin", "profitability", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Operating margin", "A higher value generally means the core activity keeps more room after operating expenses.", "With a position, it can help explain why the company may remain robust, but it does not erase timing or trend risk.", "Without a position, it can help you understand business resilience before going further.", true, true, "It can vary by sector and cycle, so it must be read against the company context and not with a universal threshold.", "Show how much operating profit remains after core operating costs.", "Operating margin compares operating income to revenue.", null, "It does not prove future growth, balance-sheet safety, or a final recommendation on its own.", "It supports the reading that the business may have pricing power or cost discipline.", "It helps judge whether profitability comes from the normal business activity rather than from exceptional items." },
                    { "returnOnEquity", "profitability", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Return on equity", "A higher value generally indicates stronger capital efficiency, as long as it is not inflated by an unusually low equity base.", "With a position, it can reinforce conviction in the business profile, but it does not replace risk management or invalidation levels.", "Without a position, use it to qualify business quality, not to replace market-timing analysis.", true, true, "Read it with debt and margin metrics because a high value alone can hide balance-sheet fragility.", "Measure how efficiently the company turns shareholder capital into profit.", "Return on equity compares net profit to shareholder equity.", null, "It does not prove that the stock is cheap, timely to buy, or protected from a reversal on its own.", "It supports a profitability reading when it stays coherent with the rest of the operating picture.", "It helps beginners see whether profitability is supported by the capital already invested in the business." },
                    { "trailingPe", "valuation", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Trailing P/E", "A lower value can indicate a cheaper valuation, but only relative to the company quality, growth profile, and sector norms.", "With a position, it can help you reassess valuation stretch, but it does not replace the technical and portfolio context.", "Without a position, it can help you judge whether the market price already embeds optimism.", true, true, "It becomes weak when earnings are cyclical, temporarily distorted, or negative.", "Compare the share price to trailing earnings per share.", "Trailing P/E expresses how many times the market values the last reported earnings.", null, "It does not prove that the stock is undervalued, safe, or ready to buy on its own.", "It supports a valuation reading when earnings remain meaningful and comparable.", "It helps beginners see that a stock price should be read against earnings, not in isolation." }
                });

            migrationBuilder.InsertData(
                table: "RecommendationWordingVersions",
                columns: new[] { "WordingVersionId", "ActivatedAtUtc", "AffectedDomains", "CreatedAtUtc", "DisplayName", "ExplanationPolicyVersion", "IsActive", "RecommendationPolicyVersion", "UpdatedAtUtc" },
                values: new object[] { "REC_WORDING_V1", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), "recommendation,explanation", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), "PredictFinance V1 recommendation wording", "analysis-v1-explanation@prompt5", true, "analysis-v1-policy@prompt3", null });

            migrationBuilder.InsertData(
                table: "RecommendationWordingScenarios",
                columns: new[] { "Id", "ActionVerbFamilyCode", "CreatedAtUtc", "HoldingStatus", "RecommendationKind", "RecommendationStrengthFamily", "ScenarioCode", "TemplateSummary", "UpdatedAtUtc", "WordingVersionId" },
                values: new object[,]
                {
                    { "SCN_HELD_HOLD", "HOLD", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, 3, "Low,Medium,High", "HELD_HOLD", "Conserver avec suffixe de force fort, moyen, ou faible pour une position deja detenue.", null, "REC_WORDING_V1" },
                    { "SCN_HELD_REINFORCE", "REINFORCE", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, 4, "Low,Medium,High", "HELD_REINFORCE", "Renforcer avec suffixe de force fort, moyen, ou faible pour une position deja detenue.", null, "REC_WORDING_V1" },
                    { "SCN_HELD_SELL", "SELL", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, 6, "Low,Medium,High", "HELD_SELL", "Vendre avec suffixe de force fort, moyen, ou faible pour une sortie pedagogique de position.", null, "REC_WORDING_V1" },
                    { "SCN_HELD_WAIT", "WAIT", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), 1, 2, "Low,Medium,High", "HELD_WAIT", "Attendre avec une formulation prudente qui ne force pas de changement de posture sur la position actuelle.", null, "REC_WORDING_V1" },
                    { "SCN_NOT_HELD_BUY", "BUY", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), 0, 1, "Low,Medium,High", "NOT_HELD_BUY", "Acheter avec suffixe de force fort, moyen, ou faible pour une lecture d'entree pedagogique.", null, "REC_WORDING_V1" },
                    { "SCN_NOT_HELD_MONITOR", "MONITOR", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), 0, 0, "Low,Medium,High", "NOT_HELD_MONITOR", "Surveiller avec suffixe de force fort, moyen, ou faible tant que le signal n'est pas confirme.", null, "REC_WORDING_V1" },
                    { "SCN_NOT_HELD_WAIT", "WAIT", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Utc), 0, 2, "Low,Medium,High", "NOT_HELD_WAIT", "Attendre avec une formulation prudente qui refuse une prise de position immediate.", null, "REC_WORDING_V1" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParameterDictionaryEntries_CategoryCode_IsPublished_IsActive",
                table: "ParameterDictionaryEntries",
                columns: new[] { "CategoryCode", "IsPublished", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationWordingScenarios_RecommendationKind_HoldingStatus",
                table: "RecommendationWordingScenarios",
                columns: new[] { "RecommendationKind", "HoldingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationWordingScenarios_WordingVersionId_ScenarioCode",
                table: "RecommendationWordingScenarios",
                columns: new[] { "WordingVersionId", "ScenarioCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationWordingVersions_IsActive_ActivatedAtUtc",
                table: "RecommendationWordingVersions",
                columns: new[] { "IsActive", "ActivatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_Category_CreatedAtUtc",
                table: "UserNotifications",
                columns: new[] { "UserId", "Category", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_Status_CreatedAtUtc",
                table: "UserNotifications",
                columns: new[] { "UserId", "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParameterDictionaryEntries");

            migrationBuilder.DropTable(
                name: "RecommendationWordingScenarios");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "RecommendationWordingVersions");
        }
    }
}
