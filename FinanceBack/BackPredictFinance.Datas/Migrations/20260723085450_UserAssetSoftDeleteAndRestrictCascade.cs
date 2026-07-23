using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class UserAssetSoftDeleteAndRestrictCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetTransactions_UserAssets_UserAssetId",
                table: "AssetTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Recommendations_UserAssets_UserAssetId",
                table: "Recommendations");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserAssets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTransactions_UserAssets_UserAssetId",
                table: "AssetTransactions",
                column: "UserAssetId",
                principalTable: "UserAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Recommendations_UserAssets_UserAssetId",
                table: "Recommendations",
                column: "UserAssetId",
                principalTable: "UserAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetTransactions_UserAssets_UserAssetId",
                table: "AssetTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Recommendations_UserAssets_UserAssetId",
                table: "Recommendations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserAssets");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTransactions_UserAssets_UserAssetId",
                table: "AssetTransactions",
                column: "UserAssetId",
                principalTable: "UserAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Recommendations_UserAssets_UserAssetId",
                table: "Recommendations",
                column: "UserAssetId",
                principalTable: "UserAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
