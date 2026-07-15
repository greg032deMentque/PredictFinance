using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectionAndReliabilityConcepts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AnalysisConceptExplanations",
                columns: new[] { "Code", "Explanation", "Label" },
                values: new object[,]
                {
                    { "reliability", "Taux de réussite observé sur un large échantillon passé pour ce type de figure (source Bulkowski). Plus il est élevé, plus la figure tend à se concrétiser une fois confirmée — ce n'est jamais une garantie.", "Fiabilité historique" },
                    { "trendfollowing", "La figure ne donne pas de direction propre : elle anticipe la poursuite du mouvement (hausse ou baisse) déjà en place avant son apparition.", "Suit la tendance" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AnalysisConceptExplanations",
                keyColumn: "Code",
                keyValue: "reliability");

            migrationBuilder.DeleteData(
                table: "AnalysisConceptExplanations",
                keyColumn: "Code",
                keyValue: "trendfollowing");
        }
    }
}
