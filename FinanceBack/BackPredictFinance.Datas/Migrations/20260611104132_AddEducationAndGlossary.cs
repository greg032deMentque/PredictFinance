using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationAndGlossary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EducationArticles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProductType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    BodyMarkdown = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationArticles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlossaryTerms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Term = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedTerm = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Definition = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlossaryTerms", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "EducationArticles",
                columns: new[] { "Id", "BodyMarkdown", "CreatedAtUtc", "DisplayOrder", "IsActive", "IsPublished", "ProductType", "Slug", "Summary", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { "3f1a2b4c-0001-0000-0000-000000000001", "## Définition\n\nL'assurance vie est un contrat d'épargne entre un souscripteur et une compagnie d'assurance. Elle permet de faire fructifier une épargne sur le long terme tout en bénéficiant d'une fiscalité allégée et d'une grande souplesse en cas de transmission de patrimoine.\n\n## À quoi ça sert ?\n\n- Constituer une épargne progressive\n- Préparer un projet à moyen ou long terme (retraite, achat immobilier, transmission)\n- Transmettre un capital à un bénéficiaire désigné hors succession\n\n## Comment ça fonctionne ?\n\nVous versez des primes (libres ou programmées) sur un contrat. Ces sommes sont investies sur des supports financiers :\n- **Fonds en euros** : capital garanti, rendement modéré\n- **Unités de compte (UC)** : investis sur des marchés financiers, potentiel plus élevé mais sans garantie du capital\n\n## Règles clés\n\n- **Plafonds de versement** : aucun plafond légal sur les versements\n- **Fiscalité** : après 8 ans de détention, abattement annuel de 4 600 € (personne seule) ou 9 200 € (couple) sur les gains, puis prélèvement forfaitaire de 7,5 % au-delà\n- **Disponibilité** : capital disponible à tout moment via un rachat partiel ou total\n- **Transmission** : hors succession jusqu'à 152 500 € par bénéficiaire pour les versements effectués avant 70 ans\n\n## Bonnes pratiques\n\n- Privilégier les contrats multisupports pour diversifier le risque\n- Vérifier les frais d'entrée, de gestion et d'arbitrage\n- Adapter la part fonds euros / UC à votre horizon et à votre profil de risque\n- Ne pas confondre assurance vie et assurance décès\n\n---\n*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, true, "LifeInsurance", "assurance-vie", "Un placement souple et fiscalement avantageux, adapté à l'épargne à moyen ou long terme.", "L'assurance vie", null },
                    { "3f1a2b4c-0001-0000-0000-000000000002", "## Définition\n\nLe Plan d'Épargne en Actions (PEA) est une enveloppe fiscale permettant d'investir en actions d'entreprises européennes tout en bénéficiant d'une exonération d'impôt sur les plus-values après 5 ans de détention.\n\n## À quoi ça sert ?\n\n- Investir en actions françaises et européennes avec un avantage fiscal\n- Constituer une épargne boursière à long terme\n- Percevoir des dividendes exonérés d'impôt sur le revenu après 5 ans\n\n## Comment ça fonctionne ?\n\nVous alimentez le PEA par des versements en numéraire. Les sommes sont investies dans des titres éligibles (actions d'entreprises de l'Espace Économique Européen, OPCVM éligibles). Les gains réalisés restent dans l'enveloppe sans imposition tant qu'ils ne sont pas retirés.\n\n## Règles clés\n\n- **Plafond de versement** : 150 000 € pour un PEA classique, 75 000 € pour un PEA-PME\n- **Fiscalité** : après 5 ans, les gains sont exonérés d'impôt sur le revenu (hors prélèvements sociaux de 17,2 %)\n- **Disponibilité** : tout retrait avant 5 ans entraîne la clôture du PEA et une imposition des gains ; après 5 ans, les retraits partiels sont possibles sans clôture\n- **Titres éligibles** : actions cotées ou non d'entreprises de l'EEE, certains OPCVM, fonds indiciels (ETF) éligibles\n\n## Bonnes pratiques\n\n- Ouvrir le PEA dès que possible pour faire courir le délai de 5 ans\n- Ne pas retirer avant 5 ans sauf nécessité absolue\n- Utiliser des ETF éligibles pour diversifier à moindre coût\n- Vérifier l'éligibilité PEA de chaque titre avant achat\n\n---\n*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, true, "PEA", "pea", "Une enveloppe fiscale pour investir en actions européennes avec une exonération d'impôt après 5 ans.", "Le Plan d'Épargne en Actions (PEA)", null },
                    { "3f1a2b4c-0001-0000-0000-000000000003", "## Définition\n\nLe Plan d'Épargne Logement (PEL) est un produit d'épargne réglementé proposé par les banques. Il permet d'accumuler une épargne sur une durée minimale de 4 ans en vue d'obtenir un prêt immobilier à taux préférentiel.\n\n## À quoi ça sert ?\n\n- Préparer un achat immobilier\n- Obtenir un droit à prêt à taux avantageux (sous conditions)\n- Bénéficier d'une épargne à taux fixe garanti\n\n## Comment ça fonctionne ?\n\nVous effectuez des versements réguliers obligatoires sur le PEL. Le taux d'intérêt est fixé à l'ouverture et garanti pendant toute la durée du plan. Au terme de la phase d'épargne, vous pouvez demander un prêt immobilier lié au PEL.\n\n## Règles clés\n\n- **Plafond de versement** : 61 200 €\n- **Versements** : minimum 540 € par an, libre répartition\n- **Durée minimale** : 4 ans pour obtenir le droit à prêt ; au-delà de 10 ans le PEL cesse de produire des droits à prêt supplémentaires\n- **Fiscalité** : les intérêts sont soumis au prélèvement forfaitaire unique (PFU 30 %) depuis 2018 pour les PEL ouverts après cette date\n- **Disponibilité** : clôture possible à tout moment mais perte des droits à prêt et pénalités sur intérêts si clôture avant 2 ans\n\n## Bonnes pratiques\n\n- Vérifier que le taux en vigueur à l'ouverture est compétitif par rapport aux taux du marché\n- Ne pas dépasser 10 ans sans l'utiliser (le prêt PEL perd de l'intérêt au-delà)\n- Comparer avec d'autres placements sécurisés si l'objectif immobilier est incertain\n\n---\n*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3, true, true, "PEL", "pel", "Un compte d'épargne réglementé orienté vers le financement d'un projet immobilier.", "Le Plan d'Épargne Logement (PEL)", null },
                    { "3f1a2b4c-0001-0000-0000-000000000004", "## Définition\n\nLe Plan d'Épargne Retraite (PER) est un produit d'épargne long terme créé par la loi PACTE (2019). Il vise à préparer la retraite en permettant une déduction fiscale des versements du revenu imposable, tout en offrant une grande souplesse de gestion.\n\n## À quoi ça sert ?\n\n- Préparer financièrement la retraite\n- Réduire son impôt sur le revenu via la déduction des versements volontaires\n- Investir sur des supports diversifiés (fonds euros, unités de compte)\n\n## Comment ça fonctionne ?\n\nVous versez librement sur le PER individuel (PERin). Les sommes sont investies selon votre profil de risque. À la retraite, vous pouvez sortir en capital, en rente viagère, ou une combinaison des deux.\n\n## Règles clés\n\n- **Plafond de déduction** : 10 % des revenus professionnels de l'année précédente (dans certaines limites), reportable sur 3 ans\n- **Fiscalité à la sortie** : si les versements ont été déduits, le capital et les gains sont imposables à la sortie (impôt sur le revenu + prélèvements sociaux sur les gains)\n- **Disponibilité** : les fonds sont bloqués jusqu'à la retraite, sauf cas de déblocage anticipé (achat résidence principale, invalidité, décès du conjoint, surendettement, expiration des droits chômage)\n- **Sortie en capital** : possible en totalité depuis la loi PACTE\n\n## Bonnes pratiques\n\n- Verser dans le PER est surtout avantageux si vous êtes dans une tranche marginale d'imposition élevée\n- Prévoir la fiscalité à la sortie : un fort capital entraînera une imposition significative\n- Ne pas mobiliser toute son épargne dans le PER si vous avez des besoins de liquidité à moyen terme\n- Comparer les frais de gestion des contrats PER, ils varient sensiblement\n\n---\n*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4, true, true, "PER", "per", "Une enveloppe d'épargne retraite flexible avec déduction fiscale à l'entrée.", "Le Plan d'Épargne Retraite (PER)", null }
                });

            migrationBuilder.InsertData(
                table: "GlossaryTerms",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "Definition", "IsActive", "IsPublished", "NormalizedTerm", "Term", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { "4a2b3c5d-0002-0000-0000-000000000001", "PEA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan d'Épargne en Actions : enveloppe fiscale permettant d'investir en actions européennes avec exonération d'impôt après 5 ans de détention.", true, true, "pea", "PEA", null },
                    { "4a2b3c5d-0002-0000-0000-000000000002", "PER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan d'Épargne Retraite : produit d'épargne long terme permettant de déduire les versements du revenu imposable et de préparer la retraite.", true, true, "per", "PER", null },
                    { "4a2b3c5d-0002-0000-0000-000000000003", "PEL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Plan d'Épargne Logement : produit d'épargne réglementé orienté vers l'acquisition immobilière, ouvrant droit à un prêt à taux préférentiel.", true, true, "pel", "PEL", null },
                    { "4a2b3c5d-0002-0000-0000-000000000004", "AssuranceVie", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Contrat d'épargne souple permettant de faire fructifier un capital sur le long terme, avec des avantages fiscaux et successoraux.", true, true, "assurance vie", "Assurance vie", null },
                    { "4a2b3c5d-0002-0000-0000-000000000005", "AssuranceVie", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Supports d'investissement non garantis en capital au sein d'une assurance vie ou d'un PER, investis sur des marchés financiers (actions, obligations, immobilier).", true, true, "unites de compte uc", "Unités de compte (UC)", null },
                    { "4a2b3c5d-0002-0000-0000-000000000006", "AssuranceVie", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Support à capital garanti disponible dans les contrats d'assurance vie. Le rendement est généralement plus modéré que les unités de compte.", true, true, "fonds en euros", "Fonds en euros", null },
                    { "4a2b3c5d-0002-0000-0000-000000000007", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Réduction appliquée sur la base imposable avant calcul de l'impôt. Exemple : abattement de 4 600 € sur les gains d'une assurance vie après 8 ans.", true, true, "abattement", "Abattement", null },
                    { "4a2b3c5d-0002-0000-0000-000000000008", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Montant maximum que l'on peut verser sur un produit d'épargne réglementé. Exemple : 150 000 € pour un PEA classique.", true, true, "plafond de versement", "Plafond de versement", null },
                    { "4a2b3c5d-0002-0000-0000-000000000009", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cotisations (CSG, CRDS, etc.) prélevées sur les revenus du capital. Leur taux global est de 17,2 % en France.", true, true, "prelevements sociaux", "Prélèvements sociaux", null },
                    { "4a2b3c5d-0002-0000-0000-000000000010", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Prélèvement Forfaitaire Unique de 30 % (12,8 % d'impôt + 17,2 % de prélèvements sociaux) applicable aux revenus du capital depuis 2018.", true, true, "flat tax pfu", "Flat tax (PFU)", null },
                    { "4a2b3c5d-0002-0000-0000-000000000011", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Versement automatique et régulier sur un produit d'épargne. Permet de lisser le coût d'entrée et de discipliner l'épargne.", true, true, "versement programme", "Versement programmé", null },
                    { "4a2b3c5d-0002-0000-0000-000000000012", "AssuranceVie", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Opération consistant à transférer des fonds d'un support à un autre au sein d'un même contrat (assurance vie, PER) sans sortie de l'enveloppe fiscale.", true, true, "arbitrage", "Arbitrage", null },
                    { "4a2b3c5d-0002-0000-0000-000000000013", "AssuranceVie", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Retrait total ou partiel d'une assurance vie. Un rachat partiel laisse le contrat ouvert ; un rachat total entraîne la clôture du contrat.", true, true, "rachat assurance vie", "Rachat (assurance vie)", null },
                    { "4a2b3c5d-0002-0000-0000-000000000014", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Capacité à récupérer son capital rapidement sans perte significative. Certains produits (PEL, PER) ont des contraintes de disponibilité.", true, true, "liquidite disponibilite", "Liquidité / Disponibilité", null },
                    { "4a2b3c5d-0002-0000-0000-000000000015", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Durée pendant laquelle l'investisseur prévoit de conserver son placement. Un horizon long permet généralement de prendre plus de risque pour viser un rendement supérieur.", true, true, "horizon de placement", "Horizon de placement", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_EducationArticles_IsActive_IsPublished_DisplayOrder",
                table: "EducationArticles",
                columns: new[] { "IsActive", "IsPublished", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_EducationArticles_Slug",
                table: "EducationArticles",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTerms_IsActive_IsPublished_Category",
                table: "GlossaryTerms",
                columns: new[] { "IsActive", "IsPublished", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_GlossaryTerms_NormalizedTerm",
                table: "GlossaryTerms",
                column: "NormalizedTerm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EducationArticles");

            migrationBuilder.DropTable(
                name: "GlossaryTerms");
        }
    }
}
