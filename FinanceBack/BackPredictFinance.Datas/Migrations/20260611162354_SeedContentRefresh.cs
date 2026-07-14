using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BackPredictFinance.Datas.Migrations
{
    /// <inheritdoc />
    public partial class SeedContentRefresh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EducationArticles",
                keyColumn: "Id",
                keyValue: "3f1a2b4c-0001-0000-0000-000000000001",
                column: "BodyMarkdown",
                value: "## Définition\n\nL'assurance vie est un contrat d'épargne entre un souscripteur et une compagnie d'assurance. Elle permet de faire fructifier une épargne sur le long terme tout en bénéficiant d'une fiscalité allégée et d'une grande souplesse en cas de transmission de patrimoine.\n\n## À quoi ça sert ?\n\n- Constituer une épargne progressive\n- Préparer un projet à moyen ou long terme (retraite, achat immobilier, transmission)\n- Transmettre un capital à un bénéficiaire désigné hors succession\n\n## Comment ça fonctionne ?\n\nVous versez des primes (libres ou programmées) sur un contrat. Ces sommes sont investies sur des supports financiers :\n- **Fonds en euros** : capital garanti, rendement modéré\n- **Unités de compte (UC)** : investis sur des marchés financiers, potentiel plus élevé mais sans garantie du capital\n\n## Règles clés\n\n- **Plafonds de versement** : aucun plafond légal sur les versements\n- **Fiscalité** : après 8 ans de détention, abattement annuel de 4 600 € (personne seule) ou 9 200 € (couple) sur les gains, puis prélèvement forfaitaire de 7,5 % au-delà pour les gains issus des primes versées jusqu'à 150 000 €, et 12,8 % au-delà de ce seuil (prélèvements sociaux de 17,2 % dans tous les cas)\n- **Disponibilité** : capital disponible à tout moment via un rachat partiel ou total\n- **Transmission** : hors succession jusqu'à 152 500 € par bénéficiaire pour les versements effectués avant 70 ans\n\n## Bonnes pratiques\n\n- Privilégier les contrats multisupports pour diversifier le risque\n- Vérifier les frais d'entrée, de gestion et d'arbitrage\n- Adapter la part fonds euros / UC à votre horizon et à votre profil de risque\n- Ne pas confondre assurance vie et assurance décès\n\n---\n*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*");

            migrationBuilder.UpdateData(
                table: "EducationArticles",
                keyColumn: "Id",
                keyValue: "3f1a2b4c-0001-0000-0000-000000000002",
                column: "BodyMarkdown",
                value: "## Définition\n\nLe Plan d'Épargne en Actions (PEA) est une enveloppe fiscale permettant d'investir en actions d'entreprises européennes tout en bénéficiant d'une exonération d'impôt sur les plus-values après 5 ans de détention.\n\n## À quoi ça sert ?\n\n- Investir en actions françaises et européennes avec un avantage fiscal\n- Constituer une épargne boursière à long terme\n- Percevoir des dividendes exonérés d'impôt sur le revenu après 5 ans\n\n## Comment ça fonctionne ?\n\nVous alimentez le PEA par des versements en numéraire. Les sommes sont investies dans des titres éligibles (actions d'entreprises de l'Espace Économique Européen, OPCVM éligibles). Les gains réalisés restent dans l'enveloppe sans imposition tant qu'ils ne sont pas retirés.\n\n## Règles clés\n\n- **Plafond de versement** : 150 000 € pour un PEA classique ; le PEA-PME porte le plafond global à 225 000 € (versements PEA + PEA-PME cumulés)\n- **PEA jeunes** : réservé aux 18-25 ans rattachés au foyer fiscal de leurs parents, plafonné à 20 000 €\n- **Fiscalité** : après 5 ans, les gains sont exonérés d'impôt sur le revenu (hors prélèvements sociaux de 17,2 %)\n- **Disponibilité** : tout retrait avant 5 ans entraîne la clôture du PEA et une imposition des gains ; après 5 ans, les retraits partiels sont possibles sans clôture\n- **Titres éligibles** : actions cotées ou non d'entreprises de l'EEE, certains OPCVM, fonds indiciels (ETF) éligibles\n\n## Bonnes pratiques\n\n- Ouvrir le PEA dès que possible pour faire courir le délai de 5 ans\n- Ne pas retirer avant 5 ans sauf nécessité absolue\n- Utiliser des ETF éligibles pour diversifier à moindre coût\n- Vérifier l'éligibilité PEA de chaque titre avant achat\n\n---\n*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*");

            migrationBuilder.UpdateData(
                table: "EducationArticles",
                keyColumn: "Id",
                keyValue: "3f1a2b4c-0001-0000-0000-000000000003",
                column: "BodyMarkdown",
                value: "## Définition\n\nLe Plan d'Épargne Logement (PEL) est un produit d'épargne réglementé proposé par les banques. Il permet d'accumuler une épargne sur une durée minimale de 4 ans en vue d'obtenir un prêt immobilier à taux préférentiel.\n\n## À quoi ça sert ?\n\n- Préparer un achat immobilier\n- Obtenir un droit à prêt à taux avantageux (sous conditions)\n- Bénéficier d'une épargne à taux fixe garanti\n\n## Comment ça fonctionne ?\n\nVous effectuez des versements réguliers obligatoires sur le PEL. Le taux d'intérêt est fixé à l'ouverture et garanti pendant toute la durée du plan. Au terme de la phase d'épargne, vous pouvez demander un prêt immobilier lié au PEL.\n\n## Règles clés\n\n- **Plafond de versement** : 61 200 €\n- **Versements** : minimum 540 € par an, libre répartition\n- **Durée minimale** : 4 ans pour obtenir le droit à prêt ; au-delà de 10 ans le PEL cesse de produire des droits à prêt supplémentaires\n- **Fiscalité** : les intérêts sont soumis au prélèvement forfaitaire unique (PFU 30 %) pour les PEL ouverts à partir de 2018 ; les PEL ouverts avant 2018 conservent l'ancien régime (exonération d'impôt sur le revenu pendant les 12 premières années, hors prélèvements sociaux)\n- **Disponibilité** : clôture possible à tout moment mais perte des droits à prêt et pénalités sur intérêts si clôture avant 2 ans\n\n## Bonnes pratiques\n\n- Vérifier que le taux en vigueur à l'ouverture est compétitif par rapport aux taux du marché\n- Ne pas dépasser 10 ans sans l'utiliser (le prêt PEL perd de l'intérêt au-delà)\n- Comparer avec d'autres placements sécurisés si l'objectif immobilier est incertain\n\n---\n*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*");

            migrationBuilder.InsertData(
                table: "GlossaryTerms",
                columns: new[] { "Id", "Category", "CreatedAtUtc", "Definition", "IsActive", "IsDeleted", "IsPublished", "NormalizedTerm", "Term", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { "4a2b3c5d-0002-0000-0000-000000000016", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Titre de propriété représentant une part du capital d'une entreprise. L'actionnaire peut percevoir des dividendes et profite (ou pâtit) de l'évolution du cours.", true, false, true, "action", "Action", null },
                    { "4a2b3c5d-0002-0000-0000-000000000017", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Titre de créance : en achetant une obligation, on prête de l'argent à un émetteur (État, entreprise) en échange d'intérêts et d'un remboursement à l'échéance.", true, false, true, "obligation", "Obligation", null },
                    { "4a2b3c5d-0002-0000-0000-000000000018", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Fonds indiciel coté en bourse qui réplique la performance d'un indice (ex : CAC 40). Permet de diversifier à faible coût ; certains ETF sont éligibles au PEA.", true, false, true, "etf tracker", "ETF (Tracker)", null },
                    { "4a2b3c5d-0002-0000-0000-000000000019", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Organisme de Placement Collectif en Valeurs Mobilières (SICAV, FCP) : fonds qui investit l'argent de plusieurs épargnants sur un panier de titres géré collectivement.", true, false, true, "opcvm", "OPCVM", null },
                    { "4a2b3c5d-0002-0000-0000-000000000020", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Part du bénéfice d'une entreprise distribuée à ses actionnaires. Son versement n'est pas garanti et dépend des résultats et de la décision de l'entreprise.", true, false, true, "dividende", "Dividende", null },
                    { "4a2b3c5d-0002-0000-0000-000000000021", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gain réalisé lorsqu'un actif est revendu plus cher que son prix d'achat. La moins-value est la perte inverse.", true, false, true, "plus-value", "Plus-value", null },
                    { "4a2b3c5d-0002-0000-0000-000000000022", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mesure de l'ampleur des variations du prix d'un actif. Une forte volatilité signifie des mouvements de cours plus marqués, à la hausse comme à la baisse.", true, false, true, "volatilite", "Volatilité", null },
                    { "4a2b3c5d-0002-0000-0000-000000000023", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Répartition de l'épargne sur plusieurs actifs, secteurs ou zones géographiques pour réduire le risque global du portefeuille.", true, false, true, "diversification", "Diversification", null },
                    { "4a2b3c5d-0002-0000-0000-000000000024", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Gain généré par un placement sur une période, exprimé en pourcentage du montant investi. Un rendement passé ne préjuge pas des rendements futurs.", true, false, true, "rendement", "Rendement", null },
                    { "4a2b3c5d-0002-0000-0000-000000000025", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mécanisme par lequel les intérêts produits sont réinvestis et génèrent à leur tour des intérêts. L'effet s'amplifie fortement avec la durée.", true, false, true, "interets composes", "Intérêts composés", null },
                    { "4a2b3c5d-0002-0000-0000-000000000026", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Possibilité de récupérer moins que le montant investi. Présent sur les supports non garantis comme les actions ou les unités de compte.", true, false, true, "risque de perte en capital", "Risque de perte en capital", null },
                    { "4a2b3c5d-0002-0000-0000-000000000027", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Taux d'imposition appliqué à la dernière tranche de vos revenus. Plus elle est élevée, plus l'avantage fiscal d'un PER est important.", true, false, true, "tranche marginale d'imposition tmi", "Tranche marginale d'imposition (TMI)", null },
                    { "4a2b3c5d-0002-0000-0000-000000000028", "PER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Revenu régulier versé à vie, par exemple à la sortie d'un PER, en échange du capital accumulé.", true, false, true, "rente viagere", "Rente viagère", null },
                    { "4a2b3c5d-0002-0000-0000-000000000029", "PER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Récupération de l'épargne sous forme d'un ou plusieurs versements, par opposition à la rente. Possible sur le PER depuis la loi PACTE.", true, false, true, "sortie en capital", "Sortie en capital", null },
                    { "4a2b3c5d-0002-0000-0000-000000000030", "PER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Retrait de fonds avant l'échéance normale d'un produit bloqué (ex : PER), autorisé seulement dans certains cas légaux (achat de la résidence principale, accidents de la vie).", true, false, true, "deblocage anticipe", "Déblocage anticipé", null },
                    { "4a2b3c5d-0002-0000-0000-000000000031", "PEA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Variante du PEA réservée aux titres de PME et ETI européennes. Depuis la loi PACTE, le plafond de versement est de 225 000 €, commun avec le PEA classique.", true, false, true, "pea-pme", "PEA-PME", null },
                    { "4a2b3c5d-0002-0000-0000-000000000032", "PEA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Zone regroupant l'Union européenne plus l'Islande, le Liechtenstein et la Norvège. Les titres éligibles au PEA doivent provenir de cette zone.", true, false, true, "espace economique europeen eee", "Espace Économique Européen (EEE)", null },
                    { "4a2b3c5d-0002-0000-0000-000000000033", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Frais annuels prélevés par l'assureur ou le gestionnaire pour administrer le contrat ou le fonds. Ils réduisent le rendement net du placement.", true, false, true, "frais de gestion", "Frais de gestion", null },
                    { "4a2b3c5d-0002-0000-0000-000000000034", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Niveau de risque qu'un épargnant est prêt à accepter, selon ses objectifs, son horizon de placement et sa tolérance aux pertes.", true, false, true, "profil de risque", "Profil de risque", null },
                    { "4a2b3c5d-0002-0000-0000-000000000035", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Répartition de l'épargne entre les grandes classes d'actifs (actions, obligations, monétaire, immobilier) selon le profil et l'horizon de l'investisseur.", true, false, true, "allocation d'actifs", "Allocation d'actifs", null },
                    { "4a2b3c5d-0002-0000-0000-000000000036", "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Enveloppe sans plafond ni restriction géographique permettant d'investir sur tous types de titres, mais sans avantage fiscal spécifique.", true, false, true, "compte-titres ordinaire cto", "Compte-titres ordinaire (CTO)", null },
                    { "4a2b3c5d-0002-0000-0000-000000000037", "PER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Loi de 2019 ayant réformé l'épargne retraite en créant le PER et en assouplissant ses règles (sortie en capital, transférabilité entre contrats).", true, false, true, "loi pacte", "Loi PACTE", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000016");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000017");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000018");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000019");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000020");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000021");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000022");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000023");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000024");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000025");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000026");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000027");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000028");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000029");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000030");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000031");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000032");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000033");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000034");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000035");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000036");

            migrationBuilder.DeleteData(
                table: "GlossaryTerms",
                keyColumn: "Id",
                keyValue: "4a2b3c5d-0002-0000-0000-000000000037");

            migrationBuilder.UpdateData(
                table: "EducationArticles",
                keyColumn: "Id",
                keyValue: "3f1a2b4c-0001-0000-0000-000000000001",
                column: "BodyMarkdown",
                value: "## Définition\n\nL'assurance vie est un contrat d'épargne entre un souscripteur et une compagnie d'assurance. Elle permet de faire fructifier une épargne sur le long terme tout en bénéficiant d'une fiscalité allégée et d'une grande souplesse en cas de transmission de patrimoine.\n\n## À quoi ça sert ?\n\n- Constituer une épargne progressive\n- Préparer un projet à moyen ou long terme (retraite, achat immobilier, transmission)\n- Transmettre un capital à un bénéficiaire désigné hors succession\n\n## Comment ça fonctionne ?\n\nVous versez des primes (libres ou programmées) sur un contrat. Ces sommes sont investies sur des supports financiers :\n- **Fonds en euros** : capital garanti, rendement modéré\n- **Unités de compte (UC)** : investis sur des marchés financiers, potentiel plus élevé mais sans garantie du capital\n\n## Règles clés\n\n- **Plafonds de versement** : aucun plafond légal sur les versements\n- **Fiscalité** : après 8 ans de détention, abattement annuel de 4 600 € (personne seule) ou 9 200 € (couple) sur les gains, puis prélèvement forfaitaire de 7,5 % au-delà\n- **Disponibilité** : capital disponible à tout moment via un rachat partiel ou total\n- **Transmission** : hors succession jusqu'à 152 500 € par bénéficiaire pour les versements effectués avant 70 ans\n\n## Bonnes pratiques\n\n- Privilégier les contrats multisupports pour diversifier le risque\n- Vérifier les frais d'entrée, de gestion et d'arbitrage\n- Adapter la part fonds euros / UC à votre horizon et à votre profil de risque\n- Ne pas confondre assurance vie et assurance décès\n\n---\n*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*");

            migrationBuilder.UpdateData(
                table: "EducationArticles",
                keyColumn: "Id",
                keyValue: "3f1a2b4c-0001-0000-0000-000000000002",
                column: "BodyMarkdown",
                value: "## Définition\n\nLe Plan d'Épargne en Actions (PEA) est une enveloppe fiscale permettant d'investir en actions d'entreprises européennes tout en bénéficiant d'une exonération d'impôt sur les plus-values après 5 ans de détention.\n\n## À quoi ça sert ?\n\n- Investir en actions françaises et européennes avec un avantage fiscal\n- Constituer une épargne boursière à long terme\n- Percevoir des dividendes exonérés d'impôt sur le revenu après 5 ans\n\n## Comment ça fonctionne ?\n\nVous alimentez le PEA par des versements en numéraire. Les sommes sont investies dans des titres éligibles (actions d'entreprises de l'Espace Économique Européen, OPCVM éligibles). Les gains réalisés restent dans l'enveloppe sans imposition tant qu'ils ne sont pas retirés.\n\n## Règles clés\n\n- **Plafond de versement** : 150 000 € pour un PEA classique, 75 000 € pour un PEA-PME\n- **Fiscalité** : après 5 ans, les gains sont exonérés d'impôt sur le revenu (hors prélèvements sociaux de 17,2 %)\n- **Disponibilité** : tout retrait avant 5 ans entraîne la clôture du PEA et une imposition des gains ; après 5 ans, les retraits partiels sont possibles sans clôture\n- **Titres éligibles** : actions cotées ou non d'entreprises de l'EEE, certains OPCVM, fonds indiciels (ETF) éligibles\n\n## Bonnes pratiques\n\n- Ouvrir le PEA dès que possible pour faire courir le délai de 5 ans\n- Ne pas retirer avant 5 ans sauf nécessité absolue\n- Utiliser des ETF éligibles pour diversifier à moindre coût\n- Vérifier l'éligibilité PEA de chaque titre avant achat\n\n---\n*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*");

            migrationBuilder.UpdateData(
                table: "EducationArticles",
                keyColumn: "Id",
                keyValue: "3f1a2b4c-0001-0000-0000-000000000003",
                column: "BodyMarkdown",
                value: "## Définition\n\nLe Plan d'Épargne Logement (PEL) est un produit d'épargne réglementé proposé par les banques. Il permet d'accumuler une épargne sur une durée minimale de 4 ans en vue d'obtenir un prêt immobilier à taux préférentiel.\n\n## À quoi ça sert ?\n\n- Préparer un achat immobilier\n- Obtenir un droit à prêt à taux avantageux (sous conditions)\n- Bénéficier d'une épargne à taux fixe garanti\n\n## Comment ça fonctionne ?\n\nVous effectuez des versements réguliers obligatoires sur le PEL. Le taux d'intérêt est fixé à l'ouverture et garanti pendant toute la durée du plan. Au terme de la phase d'épargne, vous pouvez demander un prêt immobilier lié au PEL.\n\n## Règles clés\n\n- **Plafond de versement** : 61 200 €\n- **Versements** : minimum 540 € par an, libre répartition\n- **Durée minimale** : 4 ans pour obtenir le droit à prêt ; au-delà de 10 ans le PEL cesse de produire des droits à prêt supplémentaires\n- **Fiscalité** : les intérêts sont soumis au prélèvement forfaitaire unique (PFU 30 %) depuis 2018 pour les PEL ouverts après cette date\n- **Disponibilité** : clôture possible à tout moment mais perte des droits à prêt et pénalités sur intérêts si clôture avant 2 ans\n\n## Bonnes pratiques\n\n- Vérifier que le taux en vigueur à l'ouverture est compétitif par rapport aux taux du marché\n- Ne pas dépasser 10 ans sans l'utiliser (le prêt PEL perd de l'intérêt au-delà)\n- Comparer avec d'autres placements sécurisés si l'objectif immobilier est incertain\n\n---\n*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*");
        }
    }
}
