using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetTransactionSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AssetTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AssetTransactions_UserAssetId_IsDeleted",
                table: "AssetTransactions",
                columns: new[] { "UserAssetId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetTransactions_UserAssetId_IsDeleted",
                table: "AssetTransactions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AssetTransactions");
        }
    }
}
