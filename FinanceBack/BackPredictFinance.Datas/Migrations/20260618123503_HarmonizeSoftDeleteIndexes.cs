using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class HarmonizeSoftDeleteIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTerms_IsDeleted",
                table: "GlossaryTerms",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EducationArticles_IsDeleted",
                table: "EducationArticles",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GlossaryTerms_IsDeleted",
                table: "GlossaryTerms");

            migrationBuilder.DropIndex(
                name: "IX_EducationArticles_IsDeleted",
                table: "EducationArticles");
        }
    }
}
