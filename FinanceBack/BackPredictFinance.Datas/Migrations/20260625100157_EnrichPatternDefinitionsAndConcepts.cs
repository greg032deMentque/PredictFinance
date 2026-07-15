using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class EnrichPatternDefinitionsAndConcepts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnalysisNarrative",
                table: "PatternDefinitions",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DirectionLabel",
                table: "PatternDefinitions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FamilyLabel",
                table: "PatternDefinitions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Reliability",
                table: "PatternDefinitions",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReliabilityLabel",
                table: "PatternDefinitions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AnalysisConceptExplanations",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisConceptExplanations", x => x.Code);
                });

            migrationBuilder.InsertData(
                table: "AnalysisConceptExplanations",
                columns: new[] { "Code", "Explanation", "Label" },
                values: new object[,]
                {
                    { "bearish", "Orientation favorable à une baisse du cours : la figure anticipe un recul.", "Baissier" },
                    { "bullish", "Orientation favorable à une hausse du cours : la figure anticipe une progression.", "Haussier" },
                    { "continuation", "Figure qui suggère une simple pause avant la reprise de la tendance déjà en place (hausse ou baisse).", "Continuation" },
                    { "double_zone", "Niveau qui agit tantôt comme support, tantôt comme résistance selon la position du cours. Sa rupture est souvent surveillée de près.", "Zone « Double »" },
                    { "resistance", "Niveau de prix situé au-dessus du cours où les ventes ont tendance à l'emporter, freinant la hausse. Une cassure franche peut ouvrir la voie à une poursuite du mouvement.", "Résistance" },
                    { "reversal", "Figure qui suggère un changement de direction de la tendance en cours.", "Retournement" },
                    { "strength", "Estimation de la solidité d'un niveau, fondée notamment sur le nombre de touches et leur netteté. Plus la force est élevée, plus le niveau est jugé fiable.", "Force" },
                    { "support", "Niveau de prix situé sous le cours où les achats ont tendance à l'emporter, freinant la baisse. Plus il a été touché sans céder, plus il est jugé solide.", "Support" },
                    { "touches", "Nombre de fois où le cours est venu tester un niveau sans le franchir. Un niveau souvent touché est considéré comme plus significatif.", "Touches" }
                });

            migrationBuilder.UpdateData(
                table: "PatternDefinitions",
                keyColumn: "PatternId",
                keyValue: "BEAR_FLAG_CONTINUATION",
                columns: new[] { "AnalysisNarrative", "DirectionLabel", "FamilyLabel", "Reliability", "ReliabilityLabel" },
                values: new object[] { "Après une forte baisse, l'analyse évalue si le rebond en cours laisse place à une nouvelle jambe baissière une fois le drapeau cassé à la baisse.", "Baissière", "Continuation de tendance", 0.67m, "Modérée" });

            migrationBuilder.UpdateData(
                table: "PatternDefinitions",
                keyColumn: "PatternId",
                keyValue: "BULL_FLAG_CONTINUATION",
                columns: new[] { "AnalysisNarrative", "DirectionLabel", "FamilyLabel", "Reliability", "ReliabilityLabel" },
                values: new object[] { "Après une forte hausse, l'analyse évalue si la respiration en cours débouche sur une nouvelle jambe haussière une fois le drapeau cassé à la hausse.", "Haussière", "Continuation de tendance", 0.67m, "Modérée" });

            migrationBuilder.UpdateData(
                table: "PatternDefinitions",
                keyColumn: "PatternId",
                keyValue: "DOUBLE_BOTTOM",
                columns: new[] { "AnalysisNarrative", "DirectionLabel", "FamilyLabel", "Reliability", "ReliabilityLabel" },
                values: new object[] { "L'analyse compare les deux creux et surveille le franchissement de la ligne de cou, signal d'un possible retournement haussier.", "Haussière", "Retournement", 0.65m, "Modérée" });

            migrationBuilder.UpdateData(
                table: "PatternDefinitions",
                keyColumn: "PatternId",
                keyValue: "DOUBLE_TOP",
                columns: new[] { "AnalysisNarrative", "DirectionLabel", "FamilyLabel", "Reliability", "ReliabilityLabel" },
                values: new object[] { "L'analyse compare les deux sommets et surveille la cassure de la ligne de cou, signal d'un possible retournement baissier.", "Baissière", "Retournement", 0.64m, "Modérée" });

            migrationBuilder.UpdateData(
                table: "PatternDefinitions",
                keyColumn: "PatternId",
                keyValue: "HEAD_AND_SHOULDERS",
                columns: new[] { "AnalysisNarrative", "DirectionLabel", "FamilyLabel", "Reliability", "ReliabilityLabel" },
                values: new object[] { "L'analyse identifie la structure épaule-tête-épaule et attend la cassure de la ligne de cou pour valider un retournement baissier.", "Baissière", "Retournement", 0.51m, "Faible" });

            migrationBuilder.UpdateData(
                table: "PatternDefinitions",
                keyColumn: "PatternId",
                keyValue: "INVERSE_HEAD_AND_SHOULDERS",
                columns: new[] { "AnalysisNarrative", "DirectionLabel", "FamilyLabel", "Reliability", "ReliabilityLabel" },
                values: new object[] { "L'analyse identifie la structure épaule-tête-épaule inversée et attend le franchissement de la ligne de cou pour valider un retournement haussier.", "Haussière", "Retournement", 0.71m, "Fiable" });

            migrationBuilder.UpdateData(
                table: "PatternDefinitions",
                keyColumn: "PatternId",
                keyValue: "RECTANGLE_CONTINUATION",
                columns: new[] { "AnalysisNarrative", "DirectionLabel", "FamilyLabel", "Reliability", "ReliabilityLabel" },
                values: new object[] { "Tant que le cours évolue dans le rectangle, la tendance est en pause : l'analyse surveille la sortie de zone pour confirmer la reprise dans le sens initial.", "Suit la tendance", "Continuation de tendance", 0.68m, "Modérée" });

            migrationBuilder.UpdateData(
                table: "PatternDefinitions",
                keyColumn: "PatternId",
                keyValue: "SYMMETRICAL_TRIANGLE_CONTINUATION",
                columns: new[] { "AnalysisNarrative", "DirectionLabel", "FamilyLabel", "Reliability", "ReliabilityLabel" },
                values: new object[] { "L'analyse suit le resserrement des cours et attend la cassure d'un côté du triangle pour valider la poursuite de la tendance en place.", "Suit la tendance", "Continuation de tendance", 0.54m, "Faible" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisConceptExplanations");

            migrationBuilder.DropColumn(
                name: "AnalysisNarrative",
                table: "PatternDefinitions");

            migrationBuilder.DropColumn(
                name: "DirectionLabel",
                table: "PatternDefinitions");

            migrationBuilder.DropColumn(
                name: "FamilyLabel",
                table: "PatternDefinitions");

            migrationBuilder.DropColumn(
                name: "Reliability",
                table: "PatternDefinitions");

            migrationBuilder.DropColumn(
                name: "ReliabilityLabel",
                table: "PatternDefinitions");
        }
    }
}
