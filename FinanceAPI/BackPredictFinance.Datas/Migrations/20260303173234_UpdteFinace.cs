using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class UpdteFinace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recommendation_UserAssets_UserAssetId",
                table: "Recommendation");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAssets_Portfolios_PortfolioId",
                table: "UserAssets");

            migrationBuilder.DropTable(
                name: "AuditTrails");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "MarketPrices");

            migrationBuilder.DropTable(
                name: "PatternPrediction");

            migrationBuilder.DropTable(
                name: "Portfolios");

            migrationBuilder.DropTable(
                name: "PriceAlerts");

            migrationBuilder.DropTable(
                name: "IAModelVersions");

            migrationBuilder.DropIndex(
                name: "IX_UserAssets_PortfolioId",
                table: "UserAssets");

            migrationBuilder.DropIndex(
                name: "IX_UserAssets_UserId",
                table: "UserAssets");

            migrationBuilder.DropIndex(
                name: "IX_PriceHistories_AssetId",
                table: "PriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_AssetTransactions_UserAssetId",
                table: "AssetTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Recommendation",
                table: "Recommendation");

            migrationBuilder.DropColumn(
                name: "PortfolioId",
                table: "UserAssets");

            migrationBuilder.RenameTable(
                name: "Recommendation",
                newName: "Recommendations");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendation_UserAssetId",
                table: "Recommendations",
                newName: "IX_Recommendations_UserAssetId");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "Assets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Recommendations",
                table: "Recommendations",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssets_UserId_AssetId",
                table: "UserAssets",
                columns: new[] { "UserId", "AssetId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserAssets_Quantity_NonNegative",
                table: "UserAssets",
                sql: "[Quantity] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_AssetId_RetrievedAtUtc",
                table: "PriceHistories",
                columns: new[] { "AssetId", "RetrievedAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_PriceHistories_Price_Positive",
                table: "PriceHistories",
                sql: "[Price] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransactions_UserAssetId_TimestampUtc",
                table: "AssetTransactions",
                columns: new[] { "UserAssetId", "TimestampUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetTransactions_Fees_NonNegative",
                table: "AssetTransactions",
                sql: "[Fees] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetTransactions_Quantity_Positive",
                table: "AssetTransactions",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssetTransactions_UnitPrice_Positive",
                table: "AssetTransactions",
                sql: "[UnitPrice] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Symbol",
                table: "Assets",
                column: "Symbol",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Recommendations_UserAssets_UserAssetId",
                table: "Recommendations",
                column: "UserAssetId",
                principalTable: "UserAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Recommendations_UserAssets_UserAssetId",
                table: "Recommendations");

            migrationBuilder.DropIndex(
                name: "IX_UserAssets_UserId_AssetId",
                table: "UserAssets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_UserAssets_Quantity_NonNegative",
                table: "UserAssets");

            migrationBuilder.DropIndex(
                name: "IX_PriceHistories_AssetId_RetrievedAtUtc",
                table: "PriceHistories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PriceHistories_Price_Positive",
                table: "PriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_AssetTransactions_UserAssetId_TimestampUtc",
                table: "AssetTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetTransactions_Fees_NonNegative",
                table: "AssetTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetTransactions_Quantity_Positive",
                table: "AssetTransactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssetTransactions_UnitPrice_Positive",
                table: "AssetTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Assets_Symbol",
                table: "Assets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Recommendations",
                table: "Recommendations");

            migrationBuilder.RenameTable(
                name: "Recommendations",
                newName: "Recommendation");

            migrationBuilder.RenameIndex(
                name: "IX_Recommendations_UserAssetId",
                table: "Recommendation",
                newName: "IX_Recommendation_UserAssetId");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioId",
                table: "UserAssets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Recommendation",
                table: "Recommendation",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AuditTrails",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChangedColumns = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityStateEnum = table.Column<int>(type: "int", nullable: false),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RateToBase = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    RetrievedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GuidName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiniatureName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IAModelVersions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeployedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IAModelVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketPrices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPrices_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Portfolios_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceAlerts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Threshold = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceAlerts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceAlerts_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatternPrediction",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IAModelVersionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PatternType = table.Column<int>(type: "int", nullable: false),
                    PredictedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Probability = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternPrediction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatternPrediction_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatternPrediction_IAModelVersions_IAModelVersionId",
                        column: x => x.IAModelVersionId,
                        principalTable: "IAModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAssets_PortfolioId",
                table: "UserAssets",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssets_UserId",
                table: "UserAssets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_AssetId",
                table: "PriceHistories",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransactions_UserAssetId",
                table: "AssetTransactions",
                column: "UserAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPrices_AssetId",
                table: "MarketPrices",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PatternPrediction_AssetId",
                table: "PatternPrediction",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PatternPrediction_IAModelVersionId",
                table: "PatternPrediction",
                column: "IAModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_UserId",
                table: "Portfolios",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_AssetId",
                table: "PriceAlerts",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAlerts_UserId",
                table: "PriceAlerts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recommendation_UserAssets_UserAssetId",
                table: "Recommendation",
                column: "UserAssetId",
                principalTable: "UserAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAssets_Portfolios_PortfolioId",
                table: "UserAssets",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id");
        }
    }
}
