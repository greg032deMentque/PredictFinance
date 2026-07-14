using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedContentAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GlossaryTerms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EducationArticles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FaqEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Question = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearnTopics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TopicId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    RoutePath = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalCards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetRoute = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalCards", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "EducationArticles",
                keyColumn: "Id",
                keyValue: "3f1a2b4c-0001-0000-0000-000000000001",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "EducationArticles",
                keyColumn: "Id",
                keyValue: "3f1a2b4c-0001-0000-0000-000000000002",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "EducationArticles",
                keyColumn: "Id",
                keyValue: "3f1a2b4c-0001-0000-0000-000000000003",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "EducationArticles",
                keyColumn: "Id",
                keyValue: "3f1a2b4c-0001-0000-0000-000000000004",
                column: "IsDeleted",
                value: false);

            migrationBuilder.InsertData(
                table: "FaqEntries",
                columns: new[] { "Id", "Answer", "Category", "CreatedAtUtc", "DisplayOrder", "IsActive", "IsDeleted", "IsPublished", "Question", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { "5c3d4e6f-0003-0000-0000-000000000001", "Le score agrège plusieurs dimensions techniques. Plus il est élevé, plus les signaux sont convergents. Il ne constitue pas un conseil en investissement et ne garantit pas de résultat.", "Comprendre les analyses", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, false, true, "Que signifie le score de confiance ?", null },
                    { "5c3d4e6f-0003-0000-0000-000000000002", "Seuls les instruments du périmètre V1 (marchés européens principaux) sont analysables. Consultez votre watchlist pour vérifier l'éligibilité de vos instruments.", "Comprendre les analyses", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, false, true, "Pourquoi mon instrument est-il non supporté ?", null },
                    { "5c3d4e6f-0003-0000-0000-000000000003", "Une analyse est un snapshot à un instant T. Elle ne se met pas à jour automatiquement. Relancez une analyse pour obtenir une lecture actualisée du marché.", "Comprendre les analyses", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, false, true, "Les résultats expirent-ils ?", null },
                    { "5c3d4e6f-0003-0000-0000-000000000004", "Rendez-vous dans Compte > Export des données. Votre export est disponible sous 72 heures par email. Il contient l'ensemble de vos données personnelles (RGPD Art. 20).", "Compte et données", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, false, true, "Comment exporter mes données ?", null },
                    { "5c3d4e6f-0003-0000-0000-000000000005", "Compte > Supprimer le compte. L'opération est irréversible. Toutes vos données seront effacées définitivement. Une confirmation par mot de passe est requise.", "Compte et données", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, false, true, "Comment supprimer mon compte ?", null },
                    { "5c3d4e6f-0003-0000-0000-000000000006", "Compte > Confidentialité et consentements. Vous pouvez modifier vos préférences à tout moment. Les emails transactionnels liés au compte ne peuvent pas être désactivés.", "Compte et données", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 6, true, false, true, "Où voir mes consentements RGPD ?", null },
                    { "5c3d4e6f-0003-0000-0000-000000000007", "Non. PredictFinance est un outil d'analyse pédagogique, pas un broker. Aucune connexion à un compte de courtage n'est possible en V1.", "Limites V1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7, true, false, true, "PredictFinance exécute-t-il des ordres ?", null },
                    { "5c3d4e6f-0003-0000-0000-000000000008", "Les simulations sont déterministes et basées sur les données historiques disponibles. Elles ne projettent pas le futur et ne constituent pas une garantie de rendement.", "Limites V1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 8, true, false, true, "Les simulations sont-elles fiables ?", null },
                    { "5c3d4e6f-0003-0000-0000-000000000009", "La V1 couvre les principaux marchés européens. Les actions US, les crypto-monnaies et les produits dérivés ne sont pas supportés dans cette version.", "Limites V1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 9, true, false, true, "Quels marchés sont couverts en V1 ?", null }
                });

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000001",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000002",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000003",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000004",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000005",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000006",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000007",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000008",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000009",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000010",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000011",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000012",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000013",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000014",
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000015",
                column: "IsDeleted",
                value: false);

            migrationBuilder.InsertData(
                table: "LearnTopics",
                columns: new[] { "Id", "CreatedAtUtc", "DisplayOrder", "IsActive", "IsDeleted", "IsPublished", "RoutePath", "Summary", "Title", "TopicId", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { "7e5f6a8b-0005-0000-0000-000000000001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, false, true, "client/analysis", "Comment lire le résultat d'une analyse et ses dimensions de confiance.", "Comprendre une analyse", "comprendre-analyse", null },
                    { "7e5f6a8b-0005-0000-0000-000000000002", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, false, true, "client/parameters", "Les paramètres contribuent au score final. Chacun est explicatif, pas décisionnel.", "Scoring et paramètres", "scoring-parametres", null },
                    { "7e5f6a8b-0005-0000-0000-000000000003", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, false, true, "client/instruments", "Comprendre pourquoi une valeur est éligible ou non au PEA.", "Éligibilité PEA", "pea-eligibilite", null },
                    { "7e5f6a8b-0005-0000-0000-000000000004", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, false, true, "client/learn", "PredictFinance ne conseille pas. Il informe. Comprendre la différence.", "Gestion du risque", "gestion-risque", null },
                    { "7e5f6a8b-0005-0000-0000-000000000005", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5, true, false, true, "client/history", "Comment exploiter l'historique pour comprendre l'évolution d'un signal.", "Historique des analyses", "historique-analyses", null }
                });

            migrationBuilder.InsertData(
                table: "LegalCards",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "DisplayOrder", "EffectiveDate", "Icon", "IsActive", "IsDeleted", "IsPublished", "Key", "TargetRoute", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { "6d4e5f7a-0004-0000-0000-000000000001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Définit les règles d'utilisation de PredictFinance, les droits et obligations des utilisateurs.", 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-file-text", true, false, true, "cgu", null, "Conditions Générales d'Utilisation", null },
                    { "6d4e5f7a-0004-0000-0000-000000000002", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Comment vos données sont collectées, utilisées et protégées conformément au RGPD.", 2, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-shield-lock", true, false, true, "confidentialite", null, "Politique de confidentialité", null },
                    { "6d4e5f7a-0004-0000-0000-000000000003", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PredictFinance n'est pas un conseiller en investissement. Toute décision d'investissement reste de votre entière responsabilité.", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bi-exclamation-triangle", true, false, true, "avertissement-risques", null, "Avertissement sur les risques", null },
                    { "6d4e5f7a-0004-0000-0000-000000000004", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Télécharger une copie de vos données personnelles (RGPD Art. 20 — Droit à la portabilité).", 4, null, "bi-download", true, false, true, "export-donnees", "client/account/data-export", "Exporter mes données", null },
                    { "6d4e5f7a-0004-0000-0000-000000000005", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Exercer votre droit à l'effacement de vos données personnelles (RGPD Art. 17).", 5, null, "bi-trash", true, false, true, "suppression-compte", "client/account/delete", "Supprimer mon compte", null },
                    { "6d4e5f7a-0004-0000-0000-000000000006", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pour toute question relative à vos droits ou pour déposer une réclamation : contact@predictfinance.fr", 6, null, "bi-envelope", true, false, true, "contact-reclamations", null, "Contact & Réclamations", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTerms_IsDeleted",
                table: "GlossaryTerms",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EducationArticles_IsDeleted",
                table: "EducationArticles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FaqEntries_IsActive_IsPublished_DisplayOrder",
                table: "FaqEntries",
                columns: new[] { "IsActive", "IsPublished", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FaqEntries_IsDeleted",
                table: "FaqEntries",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LearnTopics_IsActive_IsPublished_DisplayOrder",
                table: "LearnTopics",
                columns: new[] { "IsActive", "IsPublished", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LearnTopics_IsDeleted",
                table: "LearnTopics",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LearnTopics_TopicId",
                table: "LearnTopics",
                column: "TopicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalCards_IsActive_IsPublished_DisplayOrder",
                table: "LegalCards",
                columns: new[] { "IsActive", "IsPublished", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalCards_IsDeleted",
                table: "LegalCards",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_LegalCards_Key",
                table: "LegalCards",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaqEntries");

            migrationBuilder.DropTable(
                name: "LearnTopics");

            migrationBuilder.DropTable(
                name: "LegalCards");

            migrationBuilder.DropIndex(
                name: "IX_GlossaryTerms_IsDeleted",
                table: "GlossaryTerms");

            migrationBuilder.DropIndex(
                name: "IX_EducationArticles_IsDeleted",
                table: "EducationArticles");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GlossaryTerms");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EducationArticles");
        }
    }
}
