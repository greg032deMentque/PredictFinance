using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddPatternDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatternDefinitions",
                columns: table => new
                {
                    PatternId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Family = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternDefinitions", x => x.PatternId);
                });

            migrationBuilder.InsertData(
                table: "PatternDefinitions",
                columns: new[] { "PatternId", "Description", "Direction", "DisplayName", "Family" },
                values: new object[,]
                {
                    { "BEAR_FLAG_CONTINUATION", "Brève phase de rebond ou horizontale après une forte impulsion baissière. La figure se valide sur une cassure à la baisse, signalant la reprise probable du mouvement de baisse.", "Bearish", "Drapeau baissier", "continuation" },
                    { "BULL_FLAG_CONTINUATION", "Brève phase de respiration baissière ou horizontale après une forte impulsion haussière. La figure se valide sur une cassure à la hausse, signalant la reprise probable du mouvement initial.", "Bullish", "Drapeau haussier", "continuation" },
                    { "DOUBLE_BOTTOM", "Deux creux de niveau équivalent séparés par un rebond intermédiaire, dessinant un « W ». Figure de retournement haussier confirmée par le franchissement de la ligne de cou (le sommet du rebond).", "Bullish", "Double creux", "reversal" },
                    { "DOUBLE_TOP", "Deux sommets de niveau équivalent séparés par un creux intermédiaire, dessinant un « M ». Figure de retournement baissier confirmée par la cassure de la ligne de cou (le bas du creux).", "Bearish", "Double sommet", "reversal" },
                    { "HEAD_AND_SHOULDERS", "Structure en trois sommets dont celui du centre (la tête) est plus haut que les deux autres (les épaules). Figure de retournement baissier confirmée par la cassure de la ligne de cou.", "Bearish", "Tête-épaules", "reversal" },
                    { "INVERSE_HEAD_AND_SHOULDERS", "Structure en trois creux dont celui du centre (la tête) est plus profond que les deux autres (les épaules). Figure de retournement haussier confirmée par le franchissement de la ligne de cou.", "Bullish", "Tête-épaules inversé", "reversal" },
                    { "RECTANGLE_CONTINUATION", "Phase de consolidation horizontale entre un support et une résistance parallèles. La figure se valide lorsque le cours sort de la zone dans le sens de la tendance précédente, suggérant une reprise de celle-ci.", "TrendFollowing", "Rectangle de continuation", "continuation" },
                    { "SYMMETRICAL_TRIANGLE_CONTINUATION", "Resserrement progressif des cours entre une ligne de plus hauts décroissants et une ligne de plus bas croissants. La figure se valide lorsque le cours franchit l'un des côtés dans le sens de la tendance établie.", "TrendFollowing", "Triangle symétrique de continuation", "continuation" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatternDefinitions");
        }
    }
}
