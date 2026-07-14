using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortfolioId",
                table: "AssetTransactions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PortfolioType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransactions_PortfolioId_TimestampUtc",
                table: "AssetTransactions",
                columns: new[] { "PortfolioId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_UserId_IsDeleted",
                table: "Portfolios",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_UserId_Name",
                table: "Portfolios",
                columns: new[] { "UserId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            // Backfill : chaque utilisateur ayant une activité finance (watchlist ou positions)
            // reçoit un portefeuille par défaut, et les transactions existantes y sont rattachées.
            // Doit s'exécuter avant l'activation de la clé étrangère (sinon les lignes existantes,
            // dont PortfolioId vaut '', violeraient la contrainte).
            migrationBuilder.Sql(@"
                INSERT INTO [Portfolios] ([Id], [UserId], [Name], [PortfolioType], [IsDeleted], [CreatedAtUtc])
                SELECT CONVERT(nvarchar(36), NEWID()), u.[UserId], N'Portefeuille principal', N'CompteTitres', 0, SYSUTCDATETIME()
                FROM (SELECT DISTINCT [UserId] FROM [UserAssets]) u;");

            migrationBuilder.Sql(@"
                UPDATE t
                SET t.[PortfolioId] = p.[Id]
                FROM [AssetTransactions] t
                INNER JOIN [UserAssets] ua ON ua.[Id] = t.[UserAssetId]
                INNER JOIN [Portfolios] p ON p.[UserId] = ua.[UserId] AND p.[IsDeleted] = 0;");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTransactions_Portfolios_PortfolioId",
                table: "AssetTransactions",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetTransactions_Portfolios_PortfolioId",
                table: "AssetTransactions");

            migrationBuilder.DropTable(
                name: "Portfolios");

            migrationBuilder.DropIndex(
                name: "IX_AssetTransactions_PortfolioId_TimestampUtc",
                table: "AssetTransactions");

            migrationBuilder.DropColumn(
                name: "PortfolioId",
                table: "AssetTransactions");
        }
    }
}
