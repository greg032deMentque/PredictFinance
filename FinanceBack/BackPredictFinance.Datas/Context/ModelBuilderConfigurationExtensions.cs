using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Datas.Context
{
    internal static class ModelBuilderConfigurationExtensions
    {
        public static void ConfiguraModels(ModelBuilder modelBuilder)
        {
            ConfigureRefreshTokens(modelBuilder);
            ConfigureDomainConstraints(modelBuilder);
            ConfigureAnalysisDomain(modelBuilder);
            ConfigureParameterDictionary(modelBuilder);
            ConfigurePatternDefinitions(modelBuilder);
            ConfigureAnalysisConceptExplanations(modelBuilder);
            ConfigureWordingGovernance(modelBuilder);
            ConfigureNotifications(modelBuilder);
            ConfigureSignalOutcomes(modelBuilder);
            ConfigureUserAlertPreferences(modelBuilder);
            ConfigureEducationDomain(modelBuilder);
            ConfigureContentDomain(modelBuilder);
            ConfigurePortfolios(modelBuilder);
            ConfigureUserScreenerPresets(modelBuilder);
        }

        private static void ConfigureUserScreenerPresets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserScreenerPreset>(entity =>
            {
                entity.ToTable("UserScreenerPresets");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
                entity.Property(x => x.QueryJson).HasMaxLength(4000).IsRequired();

                entity.HasIndex(x => new { x.UserId, x.IsDeleted });

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigurePortfolios(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Portfolio>(entity =>
            {
                entity.ToTable("Portfolios");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
                entity.Property(x => x.PortfolioType).HasConversion<string>().HasMaxLength(32).IsRequired();
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();

                // Unicité du nom par utilisateur, restreinte aux portefeuilles non supprimés :
                // un nom libéré par soft-delete peut être réutilisé.
                entity.HasIndex(x => new { x.UserId, x.Name })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0");
                entity.HasIndex(x => new { x.UserId, x.IsDeleted });

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AssetTransaction>(entity =>
            {
                entity.HasIndex(x => new { x.PortfolioId, x.TimestampUtc });
                entity.HasIndex(x => new { x.UserAssetId, x.IsDeleted });

                // Restrict : un portefeuille porteur de transactions ne peut pas être supprimé
                // physiquement (la suppression est un soft-delete applicatif).
                entity.HasOne(x => x.Portfolio)
                    .WithMany(x => x.Transactions)
                    .HasForeignKey(x => x.PortfolioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureUserAlertPreferences(ModelBuilder modelBuilder)
        {
            // Decision arbitree : alertes proactives activees par defaut, y compris pour les
            // utilisateurs existants au moment de la migration (sinon DATA_STALE/PATTERN_STATE_CHANGE
            // ne se declencheraient jamais pour eux). Le defaut colonne doit donc etre true.
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(x => x.AlertPatternStateChangeEnabled).HasDefaultValue(true);
                entity.Property(x => x.AlertLevelCrossedEnabled).HasDefaultValue(true);
                entity.Property(x => x.AlertDataStaleEnabled).HasDefaultValue(true);
            });
        }

        private static void ConfigureRefreshTokens(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.TokenHash).HasMaxLength(512).IsRequired();
                entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(512);
                entity.Property(x => x.FingerprintHash).HasMaxLength(512);
                entity.Property(x => x.DeviceId).HasMaxLength(200);

                entity.HasIndex(x => x.TokenHash).IsUnique();
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.ExpiresAtUtc);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureDomainConstraints(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.Property(x => x.Symbol).HasMaxLength(32).IsRequired();
                entity.Property(x => x.ProviderSymbol).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Isin).HasMaxLength(12);
                entity.Property(x => x.Exchange).HasMaxLength(32);
                entity.Property(x => x.Currency).HasMaxLength(12).IsRequired();
                entity.Property(x => x.Country).HasMaxLength(64);
                entity.Property(x => x.Sector).HasMaxLength(128);
                entity.Property(x => x.Category).HasMaxLength(128);
                entity.Property(x => x.Summary).HasMaxLength(4000);

                entity.HasIndex(x => x.Symbol).IsUnique();
                entity.HasIndex(x => x.Isin).IsUnique().HasFilter("[Isin] IS NOT NULL");
            });

            modelBuilder.Entity<AssetPeaEligibility>(entity =>
            {
                entity.Property(x => x.UniverseId).HasMaxLength(64).IsRequired();
                entity.Property(x => x.SourceReference).HasMaxLength(512).IsRequired();
                entity.Property(x => x.PolicyVersion).HasMaxLength(64).IsRequired();
                entity.Property(x => x.ReviewerNote).HasMaxLength(1024);
                entity.HasIndex(x => new { x.AssetId, x.UniverseId }).IsUnique();
                entity.HasIndex(x => new { x.UniverseId, x.EligibilityStatus });
                entity.HasOne(x => x.Asset)
                    .WithMany(x => x.PeaEligibilities)
                    .HasForeignKey(x => x.AssetId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserAsset>(entity =>
            {
                entity.HasIndex(x => new { x.UserId, x.AssetId }).IsUnique();
                entity.Property(x => x.Quantity).HasPrecision(18, 8);

                entity.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint("CK_UserAssets_Quantity_NonNegative", "[Quantity] >= 0");
                });
            });

            modelBuilder.Entity<AssetTransaction>(entity =>
            {
                entity.HasIndex(x => new { x.UserAssetId, x.TimestampUtc });
                entity.Property(x => x.Quantity).HasPrecision(18, 8);
                entity.Property(x => x.UnitPrice).HasPrecision(18, 8);
                entity.Property(x => x.Fees).HasPrecision(18, 8);

                entity.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint("CK_AssetTransactions_Quantity_Positive", "[Quantity] > 0");
                    tableBuilder.HasCheckConstraint("CK_AssetTransactions_UnitPrice_Positive", "[UnitPrice] > 0");
                    tableBuilder.HasCheckConstraint("CK_AssetTransactions_Fees_NonNegative", "[Fees] >= 0");
                });
            });

            modelBuilder.Entity<PriceHistory>(entity =>
            {
                entity.Property(x => x.Price).HasPrecision(18, 8);
                entity.Property(x => x.Volume).HasPrecision(18, 4);
                entity.HasIndex(x => new { x.AssetId, x.RetrievedAtUtc });

                entity.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint("CK_PriceHistories_Price_Positive", "[Price] > 0");
                });
            });

            modelBuilder.Entity<AssetQuoteSnapshot>(entity =>
            {
                entity.Property(x => x.LastPrice).HasPrecision(18, 8);
                entity.Property(x => x.DayVariationPct).HasPrecision(9, 4);
                entity.Property(x => x.Source).HasMaxLength(64).IsRequired();
                entity.HasIndex(x => new { x.AssetId, x.AsOfUtc });
            });

            modelBuilder.Entity<AssetFundamentalsSnapshot>(entity =>
            {
                entity.Property(x => x.MarketCap).HasPrecision(24, 4);
                entity.Property(x => x.TrailingPE).HasPrecision(9, 4);
                entity.Property(x => x.DividendYield).HasPrecision(9, 6);
                entity.Property(x => x.Source).HasMaxLength(64).IsRequired();
                entity.HasIndex(x => new { x.AssetId, x.AsOfUtc });
            });

            modelBuilder.Entity<AssetCandleSnapshot>(entity =>
            {
                entity.Property(x => x.Interval).HasMaxLength(16).IsRequired();
                entity.Property(x => x.Source).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Open).HasPrecision(18, 8);
                entity.Property(x => x.High).HasPrecision(18, 8);
                entity.Property(x => x.Low).HasPrecision(18, 8);
                entity.Property(x => x.Close).HasPrecision(18, 8);
                entity.Property(x => x.Volume).HasPrecision(18, 4);
                entity.HasIndex(x => new { x.AssetId, x.Interval, x.TimestampUtc }).IsUnique();
            });
        }

        private static void ConfigureAnalysisDomain(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnalysisRun>(entity =>
            {
                entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
                entity.Property(x => x.RawPayload).HasMaxLength(32000);
                entity.Property(x => x.ErrorMessage).HasMaxLength(2048);
                entity.HasIndex(x => new { x.UserId, x.StartedAtUtc });
                entity.HasIndex(x => new { x.AssetId, x.StartedAtUtc });
            });

            modelBuilder.Entity<PatternAssessment>(entity =>
            {
                entity.Property(x => x.PatternId).HasMaxLength(128).IsRequired();
                entity.Property(x => x.Phase).HasMaxLength(64).IsRequired();
                entity.Property(x => x.ProgressStatus).HasConversion<int>();
                entity.Property(x => x.Direction).HasConversion<string>().HasMaxLength(16);
                entity.Property(x => x.Probability).HasPrecision(9, 6);
                entity.Property(x => x.Confidence).HasPrecision(9, 6);
                entity.Property(x => x.CurrentPrice).HasPrecision(18, 8);
                entity.Property(x => x.NecklinePrice).HasPrecision(18, 8);
                entity.Property(x => x.TargetPrice).HasPrecision(18, 8);
                entity.Property(x => x.InvalidationPrice).HasPrecision(18, 8);
                entity.HasIndex(x => new { x.AnalysisRunId, x.IsPrimary });
            });

            modelBuilder.Entity<DecisionSignal>(entity =>
            {
                entity.Property(x => x.Confidence).HasPrecision(9, 6);
                entity.Property(x => x.Reason).HasMaxLength(2048).IsRequired();
                entity.HasIndex(x => x.AnalysisRunId).IsUnique();
            });

            modelBuilder.Entity<ModelSnapshot>(entity =>
            {
                entity.Property(x => x.ModelVersion).HasMaxLength(256).IsRequired();
                entity.Property(x => x.ModelMessage).HasMaxLength(2048).IsRequired();
                entity.Property(x => x.Precision).HasPrecision(9, 6);
                entity.Property(x => x.F1).HasPrecision(9, 6);
                entity.Property(x => x.RocAuc).HasPrecision(9, 6);
                entity.Property(x => x.SelectedThreshold).HasPrecision(9, 6);
                entity.HasIndex(x => x.AnalysisRunId).IsUnique();
            });
        }

        private static void ConfigureParameterDictionary(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ParameterDictionaryEntry>(entity =>
            {
                entity.HasKey(x => x.ParameterId);
                entity.Property(x => x.ParameterId).HasMaxLength(64).IsRequired();
                entity.Property(x => x.CategoryCode).HasMaxLength(64).IsRequired();
                entity.Property(x => x.DisplayLabel).HasMaxLength(160).IsRequired();
                entity.Property(x => x.RoleInCategory).HasMaxLength(512).IsRequired();
                entity.Property(x => x.SimpleDefinition).HasMaxLength(1024).IsRequired();
                entity.Property(x => x.HowToRead).HasMaxLength(1024).IsRequired();
                entity.Property(x => x.WhyItMatters).HasMaxLength(1024).IsRequired();
                entity.Property(x => x.LimitsOfInterpretation).HasMaxLength(1024).IsRequired();
                entity.Property(x => x.WhatItSupports).HasMaxLength(1024).IsRequired();
                entity.Property(x => x.WhatItDoesNotProve).HasMaxLength(1024).IsRequired();
                entity.Property(x => x.ImplicationWithoutPosition).HasMaxLength(1024).IsRequired();
                entity.Property(x => x.ImplicationWithPosition).HasMaxLength(1024).IsRequired();
                entity.HasIndex(x => new { x.CategoryCode, x.IsPublished, x.IsActive });

                entity.HasData(BuildSeedEntries());
            });
        }

        private static void ConfigurePatternDefinitions(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PatternDefinition>(entity =>
            {
                entity.HasKey(x => x.PatternId);
                entity.Property(x => x.PatternId).HasMaxLength(128).IsRequired();
                entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
                entity.Property(x => x.Family).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(2048).IsRequired();
                entity.Property(x => x.Direction).HasMaxLength(64).IsRequired();
                entity.Property(x => x.FamilyLabel).HasMaxLength(64).IsRequired();
                entity.Property(x => x.DirectionLabel).HasMaxLength(64).IsRequired();
                entity.Property(x => x.AnalysisNarrative).HasMaxLength(2048).IsRequired();
                entity.Property(x => x.Reliability).HasPrecision(4, 2);
                entity.Property(x => x.ReliabilityLabel).HasMaxLength(32).IsRequired();

                entity.HasData(BuildPatternDefinitionSeedEntries());
            });
        }

        private static void ConfigureAnalysisConceptExplanations(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnalysisConceptExplanation>(entity =>
            {
                entity.HasKey(x => x.Code);
                entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Label).HasMaxLength(128).IsRequired();
                entity.Property(x => x.Explanation).HasMaxLength(1024).IsRequired();

                entity.HasData(BuildAnalysisConceptSeedEntries());
            });
        }

        private static AnalysisConceptExplanation[] BuildAnalysisConceptSeedEntries()
        {
            return
            [
                new AnalysisConceptExplanation
                {
                    Code = "support",
                    Label = "Support",
                    Explanation = "Niveau de prix situé sous le cours où les achats ont tendance à l'emporter, freinant la baisse. Plus il a été touché sans céder, plus il est jugé solide."
                },
                new AnalysisConceptExplanation
                {
                    Code = "resistance",
                    Label = "Résistance",
                    Explanation = "Niveau de prix situé au-dessus du cours où les ventes ont tendance à l'emporter, freinant la hausse. Une cassure franche peut ouvrir la voie à une poursuite du mouvement."
                },
                new AnalysisConceptExplanation
                {
                    Code = "touches",
                    Label = "Touches",
                    Explanation = "Nombre de fois où le cours est venu tester un niveau sans le franchir. Un niveau souvent touché est considéré comme plus significatif."
                },
                new AnalysisConceptExplanation
                {
                    Code = "strength",
                    Label = "Force",
                    Explanation = "Estimation de la solidité d'un niveau, fondée notamment sur le nombre de touches et leur netteté. Plus la force est élevée, plus le niveau est jugé fiable."
                },
                new AnalysisConceptExplanation
                {
                    Code = "double_zone",
                    Label = "Zone « Double »",
                    Explanation = "Niveau qui agit tantôt comme support, tantôt comme résistance selon la position du cours. Sa rupture est souvent surveillée de près."
                },
                new AnalysisConceptExplanation
                {
                    Code = "continuation",
                    Label = "Continuation",
                    Explanation = "Figure qui suggère une simple pause avant la reprise de la tendance déjà en place (hausse ou baisse)."
                },
                new AnalysisConceptExplanation
                {
                    Code = "reversal",
                    Label = "Retournement",
                    Explanation = "Figure qui suggère un changement de direction de la tendance en cours."
                },
                new AnalysisConceptExplanation
                {
                    Code = "bullish",
                    Label = "Haussier",
                    Explanation = "Orientation favorable à une hausse du cours : la figure anticipe une progression."
                },
                new AnalysisConceptExplanation
                {
                    Code = "bearish",
                    Label = "Baissier",
                    Explanation = "Orientation favorable à une baisse du cours : la figure anticipe un recul."
                },
                new AnalysisConceptExplanation
                {
                    Code = "trendfollowing",
                    Label = "Suit la tendance",
                    Explanation = "La figure ne donne pas de direction propre : elle anticipe la poursuite du mouvement (hausse ou baisse) déjà en place avant son apparition."
                },
                new AnalysisConceptExplanation
                {
                    Code = "reliability",
                    Label = "Fiabilité historique",
                    Explanation = "Taux de réussite observé sur un large échantillon passé pour ce type de figure (source Bulkowski). Plus il est élevé, plus la figure tend à se concrétiser une fois confirmée — ce n'est jamais une garantie."
                }
            ];
        }

        private static PatternDefinition[] BuildPatternDefinitionSeedEntries()
        {
            return
            [
                new PatternDefinition
                {
                    PatternId = "RECTANGLE_CONTINUATION",
                    DisplayName = "Rectangle de continuation",
                    Family = "continuation",
                    Description = "Phase de consolidation horizontale entre un support et une résistance parallèles. La figure se valide lorsque le cours sort de la zone dans le sens de la tendance précédente, suggérant une reprise de celle-ci.",
                    Direction = "TrendFollowing",
                    FamilyLabel = "Continuation de tendance",
                    DirectionLabel = "Suit la tendance",
                    AnalysisNarrative = "Tant que le cours évolue dans le rectangle, la tendance est en pause : l'analyse surveille la sortie de zone pour confirmer la reprise dans le sens initial.",
                    Reliability = 0.68m,
                    ReliabilityLabel = "Modérée"
                },
                new PatternDefinition
                {
                    PatternId = "SYMMETRICAL_TRIANGLE_CONTINUATION",
                    DisplayName = "Triangle symétrique de continuation",
                    Family = "continuation",
                    Description = "Resserrement progressif des cours entre une ligne de plus hauts décroissants et une ligne de plus bas croissants. La figure se valide lorsque le cours franchit l'un des côtés dans le sens de la tendance établie.",
                    Direction = "TrendFollowing",
                    FamilyLabel = "Continuation de tendance",
                    DirectionLabel = "Suit la tendance",
                    AnalysisNarrative = "L'analyse suit le resserrement des cours et attend la cassure d'un côté du triangle pour valider la poursuite de la tendance en place.",
                    Reliability = 0.54m,
                    ReliabilityLabel = "Faible"
                },
                new PatternDefinition
                {
                    PatternId = "BULL_FLAG_CONTINUATION",
                    DisplayName = "Drapeau haussier",
                    Family = "continuation",
                    Description = "Brève phase de respiration baissière ou horizontale après une forte impulsion haussière. La figure se valide sur une cassure à la hausse, signalant la reprise probable du mouvement initial.",
                    Direction = "Bullish",
                    FamilyLabel = "Continuation de tendance",
                    DirectionLabel = "Haussière",
                    AnalysisNarrative = "Après une forte hausse, l'analyse évalue si la respiration en cours débouche sur une nouvelle jambe haussière une fois le drapeau cassé à la hausse.",
                    Reliability = 0.67m,
                    ReliabilityLabel = "Modérée"
                },
                new PatternDefinition
                {
                    PatternId = "BEAR_FLAG_CONTINUATION",
                    DisplayName = "Drapeau baissier",
                    Family = "continuation",
                    Description = "Brève phase de rebond ou horizontale après une forte impulsion baissière. La figure se valide sur une cassure à la baisse, signalant la reprise probable du mouvement de baisse.",
                    Direction = "Bearish",
                    FamilyLabel = "Continuation de tendance",
                    DirectionLabel = "Baissière",
                    AnalysisNarrative = "Après une forte baisse, l'analyse évalue si le rebond en cours laisse place à une nouvelle jambe baissière une fois le drapeau cassé à la baisse.",
                    Reliability = 0.67m,
                    ReliabilityLabel = "Modérée"
                },
                new PatternDefinition
                {
                    PatternId = "DOUBLE_BOTTOM",
                    DisplayName = "Double creux",
                    Family = "reversal",
                    Description = "Deux creux de niveau équivalent séparés par un rebond intermédiaire, dessinant un « W ». Figure de retournement haussier confirmée par le franchissement de la ligne de cou (le sommet du rebond).",
                    Direction = "Bullish",
                    FamilyLabel = "Retournement",
                    DirectionLabel = "Haussière",
                    AnalysisNarrative = "L'analyse compare les deux creux et surveille le franchissement de la ligne de cou, signal d'un possible retournement haussier.",
                    Reliability = 0.65m,
                    ReliabilityLabel = "Modérée"
                },
                new PatternDefinition
                {
                    PatternId = "DOUBLE_TOP",
                    DisplayName = "Double sommet",
                    Family = "reversal",
                    Description = "Deux sommets de niveau équivalent séparés par un creux intermédiaire, dessinant un « M ». Figure de retournement baissier confirmée par la cassure de la ligne de cou (le bas du creux).",
                    Direction = "Bearish",
                    FamilyLabel = "Retournement",
                    DirectionLabel = "Baissière",
                    AnalysisNarrative = "L'analyse compare les deux sommets et surveille la cassure de la ligne de cou, signal d'un possible retournement baissier.",
                    Reliability = 0.64m,
                    ReliabilityLabel = "Modérée"
                },
                new PatternDefinition
                {
                    PatternId = "INVERSE_HEAD_AND_SHOULDERS",
                    DisplayName = "Tête-épaules inversé",
                    Family = "reversal",
                    Description = "Structure en trois creux dont celui du centre (la tête) est plus profond que les deux autres (les épaules). Figure de retournement haussier confirmée par le franchissement de la ligne de cou.",
                    Direction = "Bullish",
                    FamilyLabel = "Retournement",
                    DirectionLabel = "Haussière",
                    AnalysisNarrative = "L'analyse identifie la structure épaule-tête-épaule inversée et attend le franchissement de la ligne de cou pour valider un retournement haussier.",
                    Reliability = 0.71m,
                    ReliabilityLabel = "Fiable"
                },
                new PatternDefinition
                {
                    PatternId = "HEAD_AND_SHOULDERS",
                    DisplayName = "Tête-épaules",
                    Family = "reversal",
                    Description = "Structure en trois sommets dont celui du centre (la tête) est plus haut que les deux autres (les épaules). Figure de retournement baissier confirmée par la cassure de la ligne de cou.",
                    Direction = "Bearish",
                    FamilyLabel = "Retournement",
                    DirectionLabel = "Baissière",
                    AnalysisNarrative = "L'analyse identifie la structure épaule-tête-épaule et attend la cassure de la ligne de cou pour valider un retournement baissier.",
                    Reliability = 0.51m,
                    ReliabilityLabel = "Faible"
                }
            ];
        }

        private static void ConfigureWordingGovernance(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RecommendationWordingVersion>(entity =>
            {
                entity.HasKey(x => x.WordingVersionId);
                entity.Property(x => x.WordingVersionId).HasMaxLength(64).IsRequired();
                entity.Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
                entity.Property(x => x.RecommendationPolicyVersion).HasMaxLength(64).IsRequired();
                entity.Property(x => x.ExplanationPolicyVersion).HasMaxLength(64).IsRequired();
                entity.Property(x => x.AffectedDomains).HasMaxLength(256).IsRequired();
                entity.HasIndex(x => new { x.IsActive, x.ActivatedAtUtc });

                entity.HasData(BuildWordingVersionSeedEntries());
            });

            modelBuilder.Entity<RecommendationWordingScenario>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasMaxLength(64).IsRequired();
                entity.Property(x => x.WordingVersionId).HasMaxLength(64).IsRequired();
                entity.Property(x => x.ScenarioCode).HasMaxLength(64).IsRequired();
                entity.Property(x => x.ActionVerbFamilyCode).HasMaxLength(64).IsRequired();
                entity.Property(x => x.RecommendationStrengthFamily).HasMaxLength(64).IsRequired();
                entity.Property(x => x.TemplateSummary).HasMaxLength(1024).IsRequired();
                entity.HasIndex(x => new { x.WordingVersionId, x.ScenarioCode }).IsUnique();
                entity.HasIndex(x => new { x.RecommendationKind, x.HoldingStatus });
                entity.HasOne(x => x.WordingVersion)
                    .WithMany(x => x.Scenarios)
                    .HasForeignKey(x => x.WordingVersionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasData(BuildWordingScenarioSeedEntries());
            });
        }

        private static void ConfigureNotifications(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserNotification>(entity =>
            {
                entity.HasKey(x => x.NotificationId);
                entity.Property(x => x.NotificationId).HasMaxLength(64).IsRequired();
                entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
                entity.Property(x => x.Summary).HasMaxLength(512).IsRequired();
                entity.Property(x => x.TargetEntityId).HasMaxLength(128);
                entity.Property(x => x.AlertTrigger).HasConversion<int?>();
                entity.HasIndex(x => new { x.UserId, x.Status, x.CreatedAtUtc });
                entity.HasIndex(x => new { x.UserId, x.Category, x.CreatedAtUtc });
                entity.HasIndex(x => new { x.UserId, x.TargetEntityId, x.AlertTrigger, x.AlertDayKeyUtc })
                    .IsUnique()
                    .HasFilter("[AlertTrigger] IS NOT NULL");
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureSignalOutcomes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SignalOutcome>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.PolicyVersion).HasMaxLength(256).IsRequired();
                entity.Property(x => x.Outcome).HasConversion<int>();
                entity.Property(x => x.ConfidenceLabel).HasConversion<int>();
                entity.HasIndex(x => x.AnalysisRunId);
                entity.HasIndex(x => x.DecisionSignalId);
                entity.HasIndex(x => x.EvaluatedAtUtc);
                entity.HasIndex(x => x.PatternAssessmentId).IsUnique();
                entity.HasOne(x => x.AnalysisRun)
                    .WithMany()
                    .HasForeignKey(x => x.AnalysisRunId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.PatternAssessment)
                    .WithMany()
                    .HasForeignKey(x => x.PatternAssessmentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.DecisionSignal)
                    .WithMany()
                    .HasForeignKey(x => x.DecisionSignalId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static ParameterDictionaryEntry[] BuildSeedEntries()
        {
            var seedTimestampUtc = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            return
            [
                new ParameterDictionaryEntry
                {
                    ParameterId = "returnOnEquity",
                    CategoryCode = "profitability",
                    DisplayLabel = "Return on equity",
                    RoleInCategory = "Measure how efficiently the company turns shareholder capital into profit.",
                    SimpleDefinition = "Return on equity compares net profit to shareholder equity.",
                    HowToRead = "A higher value generally indicates stronger capital efficiency, as long as it is not inflated by an unusually low equity base.",
                    WhyItMatters = "It helps beginners see whether profitability is supported by the capital already invested in the business.",
                    LimitsOfInterpretation = "Read it with debt and margin metrics because a high value alone can hide balance-sheet fragility.",
                    WhatItSupports = "It supports a profitability reading when it stays coherent with the rest of the operating picture.",
                    WhatItDoesNotProve = "It does not prove that the stock is cheap, timely to buy, or protected from a reversal on its own.",
                    ImplicationWithoutPosition = "Without a position, use it to qualify business quality, not to replace market-timing analysis.",
                    ImplicationWithPosition = "With a position, it can reinforce conviction in the business profile, but it does not replace risk management or invalidation levels.",
                    IsActive = true,
                    IsPublished = true,
                    CreatedAtUtc = seedTimestampUtc
                },
                new ParameterDictionaryEntry
                {
                    ParameterId = "operatingMargin",
                    CategoryCode = "profitability",
                    DisplayLabel = "Operating margin",
                    RoleInCategory = "Show how much operating profit remains after core operating costs.",
                    SimpleDefinition = "Operating margin compares operating income to revenue.",
                    HowToRead = "A higher value generally means the core activity keeps more room after operating expenses.",
                    WhyItMatters = "It helps judge whether profitability comes from the normal business activity rather than from exceptional items.",
                    LimitsOfInterpretation = "It can vary by sector and cycle, so it must be read against the company context and not with a universal threshold.",
                    WhatItSupports = "It supports the reading that the business may have pricing power or cost discipline.",
                    WhatItDoesNotProve = "It does not prove future growth, balance-sheet safety, or a final recommendation on its own.",
                    ImplicationWithoutPosition = "Without a position, it can help you understand business resilience before going further.",
                    ImplicationWithPosition = "With a position, it can help explain why the company may remain robust, but it does not erase timing or trend risk.",
                    IsActive = true,
                    IsPublished = true,
                    CreatedAtUtc = seedTimestampUtc
                },
                new ParameterDictionaryEntry
                {
                    ParameterId = "currentRatio",
                    CategoryCode = "liquidity",
                    DisplayLabel = "Current ratio",
                    RoleInCategory = "Indicate how short-term assets compare to short-term liabilities.",
                    SimpleDefinition = "Current ratio compares current assets to current liabilities.",
                    HowToRead = "A higher value generally suggests more short-term balance-sheet flexibility, but an extreme value can also reflect idle assets.",
                    WhyItMatters = "It helps beginners understand whether the company appears able to absorb near-term obligations.",
                    LimitsOfInterpretation = "It says little about long-term competitiveness and can differ strongly by industry.",
                    WhatItSupports = "It supports a liquidity reading when short-term flexibility matters in the business context.",
                    WhatItDoesNotProve = "It does not prove profitability, attractive valuation, or a good entry point on its own.",
                    ImplicationWithoutPosition = "Without a position, use it as a balance-sheet comfort signal, not as a trigger.",
                    ImplicationWithPosition = "With a position, it can reduce some short-term stress concerns, but it does not replace portfolio discipline.",
                    IsActive = true,
                    IsPublished = true,
                    CreatedAtUtc = seedTimestampUtc
                },
                new ParameterDictionaryEntry
                {
                    ParameterId = "debtToEquity",
                    CategoryCode = "debt",
                    DisplayLabel = "Debt to equity",
                    RoleInCategory = "Show how much debt stands against shareholder equity.",
                    SimpleDefinition = "Debt to equity compares the debt burden to the equity base.",
                    HowToRead = "A lower value generally indicates a lighter leverage profile, though normal levels differ by sector.",
                    WhyItMatters = "It helps evaluate whether the company could become fragile if conditions deteriorate.",
                    LimitsOfInterpretation = "A single ratio cannot describe debt quality, refinancing profile, or cash generation by itself.",
                    WhatItSupports = "It supports a leverage-risk reading when combined with profitability and liquidity data.",
                    WhatItDoesNotProve = "It does not prove that the company is safe in all scenarios or that the share is attractive now.",
                    ImplicationWithoutPosition = "Without a position, it can help filter out balance-sheet profiles that do not match your caution level.",
                    ImplicationWithPosition = "With a position, it can frame how much leverage risk you are carrying, but it does not dictate the action by itself.",
                    IsActive = true,
                    IsPublished = true,
                    CreatedAtUtc = seedTimestampUtc
                },
                new ParameterDictionaryEntry
                {
                    ParameterId = "trailingPe",
                    CategoryCode = "valuation",
                    DisplayLabel = "Trailing P/E",
                    RoleInCategory = "Compare the share price to trailing earnings per share.",
                    SimpleDefinition = "Trailing P/E expresses how many times the market values the last reported earnings.",
                    HowToRead = "A lower value can indicate a cheaper valuation, but only relative to the company quality, growth profile, and sector norms.",
                    WhyItMatters = "It helps beginners see that a stock price should be read against earnings, not in isolation.",
                    LimitsOfInterpretation = "It becomes weak when earnings are cyclical, temporarily distorted, or negative.",
                    WhatItSupports = "It supports a valuation reading when earnings remain meaningful and comparable.",
                    WhatItDoesNotProve = "It does not prove that the stock is undervalued, safe, or ready to buy on its own.",
                    ImplicationWithoutPosition = "Without a position, it can help you judge whether the market price already embeds optimism.",
                    ImplicationWithPosition = "With a position, it can help you reassess valuation stretch, but it does not replace the technical and portfolio context.",
                    IsActive = true,
                    IsPublished = true,
                    CreatedAtUtc = seedTimestampUtc
                },
                new ParameterDictionaryEntry
                {
                    ParameterId = "dividendYield",
                    CategoryCode = "dividend",
                    DisplayLabel = "Dividend yield",
                    RoleInCategory = "Relate the dividend distribution to the current share price.",
                    SimpleDefinition = "Dividend yield compares the annual dividend to the current market price.",
                    HowToRead = "A higher value can mean stronger income potential, but it can also reflect a falling share price or an unsustainable payout.",
                    WhyItMatters = "It helps beginners distinguish income characteristics from pure price expectations.",
                    LimitsOfInterpretation = "It must be read with payout sustainability and business quality, not as a standalone quality label.",
                    WhatItSupports = "It supports an income-oriented reading when the payout looks sustainable.",
                    WhatItDoesNotProve = "It does not prove valuation attractiveness, price support, or future dividend safety on its own.",
                    ImplicationWithoutPosition = "Without a position, it can clarify whether the stock fits an income objective, but not whether the timing is right.",
                    ImplicationWithPosition = "With a position, it can explain part of the holding case, but it does not replace monitoring of trend, risk, and fundamentals.",
                    IsActive = true,
                    IsPublished = true,
                    CreatedAtUtc = seedTimestampUtc
                }
            ];
        }

        private static RecommendationWordingVersion[] BuildWordingVersionSeedEntries()
        {
            var seedTimestampUtc = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            return
            [
                new RecommendationWordingVersion
                {
                    WordingVersionId = "REC_WORDING_V1",
                    DisplayName = "PredictFinance V1 recommendation wording",
                    IsActive = true,
                    ActivatedAtUtc = seedTimestampUtc,
                    RecommendationPolicyVersion = "analysis-v1-policy@prompt3",
                    ExplanationPolicyVersion = "analysis-v1-explanation@prompt5",
                    AffectedDomains = "recommendation,explanation",
                    CreatedAtUtc = seedTimestampUtc
                }
            ];
        }

        private static RecommendationWordingScenario[] BuildWordingScenarioSeedEntries()
        {
            var seedTimestampUtc = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);

            return
            [
                BuildWordingScenario("NOT_HELD_BUY", RecommendationKind.Buy, HoldingStatusEnum.NotHeld, "BUY", seedTimestampUtc, "Acheter avec suffixe de force fort, moyen, ou faible pour une lecture d'entree pedagogique."),
                BuildWordingScenario("NOT_HELD_MONITOR", RecommendationKind.Monitor, HoldingStatusEnum.NotHeld, "MONITOR", seedTimestampUtc, "Surveiller avec suffixe de force fort, moyen, ou faible tant que le signal n'est pas confirme."),
                BuildWordingScenario("NOT_HELD_WAIT", RecommendationKind.Wait, HoldingStatusEnum.NotHeld, "WAIT", seedTimestampUtc, "Attendre avec une formulation prudente qui refuse une prise de position immediate."),
                BuildWordingScenario("HELD_HOLD", RecommendationKind.Hold, HoldingStatusEnum.Held, "HOLD", seedTimestampUtc, "Conserver avec suffixe de force fort, moyen, ou faible pour une position deja detenue."),
                BuildWordingScenario("HELD_REINFORCE", RecommendationKind.Reinforce, HoldingStatusEnum.Held, "REINFORCE", seedTimestampUtc, "Renforcer avec suffixe de force fort, moyen, ou faible pour une position deja detenue."),
                BuildWordingScenario("HELD_SELL", RecommendationKind.Sell, HoldingStatusEnum.Held, "SELL", seedTimestampUtc, "Vendre avec suffixe de force fort, moyen, ou faible pour une sortie pedagogique de position."),
                BuildWordingScenario("HELD_WAIT", RecommendationKind.Wait, HoldingStatusEnum.Held, "WAIT", seedTimestampUtc, "Attendre avec une formulation prudente qui ne force pas de changement de posture sur la position actuelle.")
            ];
        }

        private static RecommendationWordingScenario BuildWordingScenario(
            string scenarioCode,
            RecommendationKind recommendationKind,
            HoldingStatusEnum holdingStatus,
            string actionVerbFamilyCode,
            DateTime seedTimestampUtc,
            string templateSummary)
        {
            return new RecommendationWordingScenario
            {
                Id = $"SCN_{scenarioCode}",
                WordingVersionId = "REC_WORDING_V1",
                ScenarioCode = scenarioCode,
                RecommendationKind = recommendationKind,
                HoldingStatus = holdingStatus,
                ActionVerbFamilyCode = actionVerbFamilyCode,
                RecommendationStrengthFamily = "Low,Medium,High",
                TemplateSummary = templateSummary,
                CreatedAtUtc = seedTimestampUtc
            };
        }

        private static void ConfigureEducationDomain(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EducationArticle>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Slug).HasMaxLength(128).IsRequired();
                entity.Property(x => x.ProductType).HasConversion<string>().HasMaxLength(32).IsRequired();
                entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
                entity.Property(x => x.Summary).HasMaxLength(1024).IsRequired();
                entity.Property(x => x.BodyMarkdown).HasMaxLength(16000).IsRequired();
                entity.HasIndex(x => x.Slug).IsUnique();
                entity.HasIndex(x => new { x.IsActive, x.IsPublished, x.DisplayOrder });
                entity.HasIndex(x => x.IsDeleted);

                entity.HasData(BuildEducationArticleSeedEntries());
            });

            modelBuilder.Entity<GlossaryTerm>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Term).HasMaxLength(256).IsRequired();
                entity.Property(x => x.NormalizedTerm).HasMaxLength(256).IsRequired();
                entity.Property(x => x.Definition).HasMaxLength(2048).IsRequired();
                entity.Property(x => x.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
                entity.HasIndex(x => x.NormalizedTerm);
                entity.HasIndex(x => new { x.IsActive, x.IsPublished, x.Category });
                entity.HasIndex(x => x.IsDeleted);

                entity.HasData(BuildGlossaryTermSeedEntries());
            });
        }

        private static EducationArticle[] BuildEducationArticleSeedEntries()
        {
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return
            [
                new EducationArticle
                {
                    Id = "3f1a2b4c-0001-0000-0000-000000000001",
                    Slug = "assurance-vie",
                    ProductType = EducationProductTypeEnum.LifeInsurance,
                    Title = "L'assurance vie",
                    Summary = "Un placement souple et fiscalement avantageux, adapté à l'épargne à moyen ou long terme.",
                    DisplayOrder = 1,
                    IsActive = true,
                    IsPublished = true,
                    CreatedAtUtc = seedDate,
                    BodyMarkdown = """
## Définition

L'assurance vie est un contrat d'épargne entre un souscripteur et une compagnie d'assurance. Elle permet de faire fructifier une épargne sur le long terme tout en bénéficiant d'une fiscalité allégée et d'une grande souplesse en cas de transmission de patrimoine.

## À quoi ça sert ?

- Constituer une épargne progressive
- Préparer un projet à moyen ou long terme (retraite, achat immobilier, transmission)
- Transmettre un capital à un bénéficiaire désigné hors succession

## Comment ça fonctionne ?

Vous versez des primes (libres ou programmées) sur un contrat. Ces sommes sont investies sur des supports financiers :
- **Fonds en euros** : capital garanti, rendement modéré
- **Unités de compte (UC)** : investis sur des marchés financiers, potentiel plus élevé mais sans garantie du capital

## Règles clés

- **Plafonds de versement** : aucun plafond légal sur les versements
- **Fiscalité** : après 8 ans de détention, abattement annuel de 4 600 € (personne seule) ou 9 200 € (couple) sur les gains, puis prélèvement forfaitaire de 7,5 % au-delà
- **Disponibilité** : capital disponible à tout moment via un rachat partiel ou total
- **Transmission** : hors succession jusqu'à 152 500 € par bénéficiaire pour les versements effectués avant 70 ans

## Bonnes pratiques

- Privilégier les contrats multisupports pour diversifier le risque
- Vérifier les frais d'entrée, de gestion et d'arbitrage
- Adapter la part fonds euros / UC à votre horizon et à votre profil de risque
- Ne pas confondre assurance vie et assurance décès

---
*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*
"""
                },
                new EducationArticle
                {
                    Id = "3f1a2b4c-0001-0000-0000-000000000002",
                    Slug = "pea",
                    ProductType = EducationProductTypeEnum.PEA,
                    Title = "Le Plan d'Épargne en Actions (PEA)",
                    Summary = "Une enveloppe fiscale pour investir en actions européennes avec une exonération d'impôt après 5 ans.",
                    DisplayOrder = 2,
                    IsActive = true,
                    IsPublished = true,
                    CreatedAtUtc = seedDate,
                    BodyMarkdown = """
## Définition

Le Plan d'Épargne en Actions (PEA) est une enveloppe fiscale permettant d'investir en actions d'entreprises européennes tout en bénéficiant d'une exonération d'impôt sur les plus-values après 5 ans de détention.

## À quoi ça sert ?

- Investir en actions françaises et européennes avec un avantage fiscal
- Constituer une épargne boursière à long terme
- Percevoir des dividendes exonérés d'impôt sur le revenu après 5 ans

## Comment ça fonctionne ?

Vous alimentez le PEA par des versements en numéraire. Les sommes sont investies dans des titres éligibles (actions d'entreprises de l'Espace Économique Européen, OPCVM éligibles). Les gains réalisés restent dans l'enveloppe sans imposition tant qu'ils ne sont pas retirés.

## Règles clés

- **Plafond de versement** : 150 000 € pour un PEA classique, 75 000 € pour un PEA-PME
- **Fiscalité** : après 5 ans, les gains sont exonérés d'impôt sur le revenu (hors prélèvements sociaux de 17,2 %)
- **Disponibilité** : tout retrait avant 5 ans entraîne la clôture du PEA et une imposition des gains ; après 5 ans, les retraits partiels sont possibles sans clôture
- **Titres éligibles** : actions cotées ou non d'entreprises de l'EEE, certains OPCVM, fonds indiciels (ETF) éligibles

## Bonnes pratiques

- Ouvrir le PEA dès que possible pour faire courir le délai de 5 ans
- Ne pas retirer avant 5 ans sauf nécessité absolue
- Utiliser des ETF éligibles pour diversifier à moindre coût
- Vérifier l'éligibilité PEA de chaque titre avant achat

---
*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*
"""
                },
                new EducationArticle
                {
                    Id = "3f1a2b4c-0001-0000-0000-000000000003",
                    Slug = "pel",
                    ProductType = EducationProductTypeEnum.PEL,
                    Title = "Le Plan d'Épargne Logement (PEL)",
                    Summary = "Un compte d'épargne réglementé orienté vers le financement d'un projet immobilier.",
                    DisplayOrder = 3,
                    IsActive = true,
                    IsPublished = true,
                    CreatedAtUtc = seedDate,
                    BodyMarkdown = """
## Définition

Le Plan d'Épargne Logement (PEL) est un produit d'épargne réglementé proposé par les banques. Il permet d'accumuler une épargne sur une durée minimale de 4 ans en vue d'obtenir un prêt immobilier à taux préférentiel.

## À quoi ça sert ?

- Préparer un achat immobilier
- Obtenir un droit à prêt à taux avantageux (sous conditions)
- Bénéficier d'une épargne à taux fixe garanti

## Comment ça fonctionne ?

Vous effectuez des versements réguliers obligatoires sur le PEL. Le taux d'intérêt est fixé à l'ouverture et garanti pendant toute la durée du plan. Au terme de la phase d'épargne, vous pouvez demander un prêt immobilier lié au PEL.

## Règles clés

- **Plafond de versement** : 61 200 €
- **Versements** : minimum 540 € par an, libre répartition
- **Durée minimale** : 4 ans pour obtenir le droit à prêt ; au-delà de 10 ans le PEL cesse de produire des droits à prêt supplémentaires
- **Fiscalité** : les intérêts sont soumis au prélèvement forfaitaire unique (PFU 30 %) depuis 2018 pour les PEL ouverts après cette date
- **Disponibilité** : clôture possible à tout moment mais perte des droits à prêt et pénalités sur intérêts si clôture avant 2 ans

## Bonnes pratiques

- Vérifier que le taux en vigueur à l'ouverture est compétitif par rapport aux taux du marché
- Ne pas dépasser 10 ans sans l'utiliser (le prêt PEL perd de l'intérêt au-delà)
- Comparer avec d'autres placements sécurisés si l'objectif immobilier est incertain

---
*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*
"""
                },
                new EducationArticle
                {
                    Id = "3f1a2b4c-0001-0000-0000-000000000004",
                    Slug = "per",
                    ProductType = EducationProductTypeEnum.PER,
                    Title = "Le Plan d'Épargne Retraite (PER)",
                    Summary = "Une enveloppe d'épargne retraite flexible avec déduction fiscale à l'entrée.",
                    DisplayOrder = 4,
                    IsActive = true,
                    IsPublished = true,
                    CreatedAtUtc = seedDate,
                    BodyMarkdown = """
## Définition

Le Plan d'Épargne Retraite (PER) est un produit d'épargne long terme créé par la loi PACTE (2019). Il vise à préparer la retraite en permettant une déduction fiscale des versements du revenu imposable, tout en offrant une grande souplesse de gestion.

## À quoi ça sert ?

- Préparer financièrement la retraite
- Réduire son impôt sur le revenu via la déduction des versements volontaires
- Investir sur des supports diversifiés (fonds euros, unités de compte)

## Comment ça fonctionne ?

Vous versez librement sur le PER individuel (PERin). Les sommes sont investies selon votre profil de risque. À la retraite, vous pouvez sortir en capital, en rente viagère, ou une combinaison des deux.

## Règles clés

- **Plafond de déduction** : 10 % des revenus professionnels de l'année précédente (dans certaines limites), reportable sur 3 ans
- **Fiscalité à la sortie** : si les versements ont été déduits, le capital et les gains sont imposables à la sortie (impôt sur le revenu + prélèvements sociaux sur les gains)
- **Disponibilité** : les fonds sont bloqués jusqu'à la retraite, sauf cas de déblocage anticipé (achat résidence principale, invalidité, décès du conjoint, surendettement, expiration des droits chômage)
- **Sortie en capital** : possible en totalité depuis la loi PACTE

## Bonnes pratiques

- Verser dans le PER est surtout avantageux si vous êtes dans une tranche marginale d'imposition élevée
- Prévoir la fiscalité à la sortie : un fort capital entraînera une imposition significative
- Ne pas mobiliser toute son épargne dans le PER si vous avez des besoins de liquidité à moyen terme
- Comparer les frais de gestion des contrats PER, ils varient sensiblement

---
*Ce contenu est fourni à titre pédagogique général. Il ne constitue pas un conseil en investissement personnalisé.*
"""
                }
            ];
        }

        private static void ConfigureContentDomain(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FaqEntry>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Category).HasMaxLength(128).IsRequired();
                entity.Property(x => x.Question).HasMaxLength(512).IsRequired();
                entity.Property(x => x.Answer).HasMaxLength(2048).IsRequired();
                entity.HasIndex(x => new { x.IsActive, x.IsPublished, x.DisplayOrder });
                entity.HasIndex(x => x.IsDeleted);

                entity.HasData(BuildFaqEntrySeedEntries());
            });

            modelBuilder.Entity<LegalCard>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Key).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Icon).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(1024).IsRequired();
                entity.Property(x => x.TargetRoute).HasMaxLength(256);
                entity.HasIndex(x => x.Key).IsUnique();
                entity.HasIndex(x => new { x.IsActive, x.IsPublished, x.DisplayOrder });
                entity.HasIndex(x => x.IsDeleted);

                entity.HasData(BuildLegalCardSeedEntries());
            });

            modelBuilder.Entity<LearnTopic>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasMaxLength(64).IsRequired();
                entity.Property(x => x.TopicId).HasMaxLength(128).IsRequired();
                entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
                entity.Property(x => x.Summary).HasMaxLength(1024).IsRequired();
                entity.Property(x => x.RoutePath).HasMaxLength(256).IsRequired();
                entity.HasIndex(x => x.TopicId).IsUnique();
                entity.HasIndex(x => new { x.IsActive, x.IsPublished, x.DisplayOrder });
                entity.HasIndex(x => x.IsDeleted);

                entity.HasData(BuildLearnTopicSeedEntries());
            });
        }

        private static FaqEntry[] BuildFaqEntrySeedEntries()
        {
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return
            [
                new FaqEntry { Id = "5c3d4e6f-0003-0000-0000-000000000001", Category = "Comprendre les analyses", Question = "Que signifie le score de confiance ?", Answer = "Le score agrège plusieurs dimensions techniques. Plus il est élevé, plus les signaux sont convergents. Il ne constitue pas un conseil en investissement et ne garantit pas de résultat.", DisplayOrder = 1, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new FaqEntry { Id = "5c3d4e6f-0003-0000-0000-000000000002", Category = "Comprendre les analyses", Question = "Pourquoi mon instrument est-il non supporté ?", Answer = "Seuls les instruments du périmètre V1 (marchés européens principaux) sont analysables. Consultez votre watchlist pour vérifier l'éligibilité de vos instruments.", DisplayOrder = 2, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new FaqEntry { Id = "5c3d4e6f-0003-0000-0000-000000000003", Category = "Comprendre les analyses", Question = "Les résultats expirent-ils ?", Answer = "Une analyse est un snapshot à un instant T. Elle ne se met pas à jour automatiquement. Relancez une analyse pour obtenir une lecture actualisée du marché.", DisplayOrder = 3, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new FaqEntry { Id = "5c3d4e6f-0003-0000-0000-000000000004", Category = "Compte et données", Question = "Comment exporter mes données ?", Answer = "Rendez-vous dans Compte > Export des données. Votre export est disponible sous 72 heures par email. Il contient l'ensemble de vos données personnelles (RGPD Art. 20).", DisplayOrder = 4, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new FaqEntry { Id = "5c3d4e6f-0003-0000-0000-000000000005", Category = "Compte et données", Question = "Comment supprimer mon compte ?", Answer = "Compte > Supprimer le compte. L'opération est irréversible. Toutes vos données seront effacées définitivement. Une confirmation par mot de passe est requise.", DisplayOrder = 5, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new FaqEntry { Id = "5c3d4e6f-0003-0000-0000-000000000006", Category = "Compte et données", Question = "Où voir mes consentements RGPD ?", Answer = "Compte > Confidentialité et consentements. Vous pouvez modifier vos préférences à tout moment. Les emails transactionnels liés au compte ne peuvent pas être désactivés.", DisplayOrder = 6, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new FaqEntry { Id = "5c3d4e6f-0003-0000-0000-000000000007", Category = "Limites V1", Question = "PredictFinance exécute-t-il des ordres ?", Answer = "Non. PredictFinance est un outil d'analyse pédagogique, pas un broker. Aucune connexion à un compte de courtage n'est possible en V1.", DisplayOrder = 7, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new FaqEntry { Id = "5c3d4e6f-0003-0000-0000-000000000008", Category = "Limites V1", Question = "Les simulations sont-elles fiables ?", Answer = "Les simulations sont déterministes et basées sur les données historiques disponibles. Elles ne projettent pas le futur et ne constituent pas une garantie de rendement.", DisplayOrder = 8, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new FaqEntry { Id = "5c3d4e6f-0003-0000-0000-000000000009", Category = "Limites V1", Question = "Quels marchés sont couverts en V1 ?", Answer = "La V1 couvre les principaux marchés européens. Les actions US, les crypto-monnaies et les produits dérivés ne sont pas supportés dans cette version.", DisplayOrder = 9, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate }
            ];
        }

        private static LegalCard[] BuildLegalCardSeedEntries()
        {
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var effectiveDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return
            [
                new LegalCard { Id = "6d4e5f7a-0004-0000-0000-000000000001", Key = "cgu", Icon = "bi-file-text", Title = "Conditions Générales d'Utilisation", Description = "Définit les règles d'utilisation de PredictFinance, les droits et obligations des utilisateurs.", EffectiveDate = effectiveDate, TargetRoute = null, DisplayOrder = 1, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new LegalCard { Id = "6d4e5f7a-0004-0000-0000-000000000002", Key = "confidentialite", Icon = "bi-shield-lock", Title = "Politique de confidentialité", Description = "Comment vos données sont collectées, utilisées et protégées conformément au RGPD.", EffectiveDate = effectiveDate, TargetRoute = null, DisplayOrder = 2, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new LegalCard { Id = "6d4e5f7a-0004-0000-0000-000000000003", Key = "avertissement-risques", Icon = "bi-exclamation-triangle", Title = "Avertissement sur les risques", Description = "PredictFinance n'est pas un conseiller en investissement. Toute décision d'investissement reste de votre entière responsabilité.", EffectiveDate = effectiveDate, TargetRoute = null, DisplayOrder = 3, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new LegalCard { Id = "6d4e5f7a-0004-0000-0000-000000000004", Key = "export-donnees", Icon = "bi-download", Title = "Exporter mes données", Description = "Télécharger une copie de vos données personnelles (RGPD Art. 20 — Droit à la portabilité).", EffectiveDate = null, TargetRoute = "client/account/data-export", DisplayOrder = 4, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new LegalCard { Id = "6d4e5f7a-0004-0000-0000-000000000005", Key = "suppression-compte", Icon = "bi-trash", Title = "Supprimer mon compte", Description = "Exercer votre droit à l'effacement de vos données personnelles (RGPD Art. 17).", EffectiveDate = null, TargetRoute = "client/account/delete", DisplayOrder = 5, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new LegalCard { Id = "6d4e5f7a-0004-0000-0000-000000000006", Key = "contact-reclamations", Icon = "bi-envelope", Title = "Contact & Réclamations", Description = "Pour toute question relative à vos droits ou pour déposer une réclamation : contact@predictfinance.fr", EffectiveDate = null, TargetRoute = null, DisplayOrder = 6, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate }
            ];
        }

        private static LearnTopic[] BuildLearnTopicSeedEntries()
        {
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return
            [
                new LearnTopic { Id = "7e5f6a8b-0005-0000-0000-000000000001", TopicId = "comprendre-analyse", Title = "Comprendre une analyse", Summary = "Comment lire le résultat d'une analyse et ses dimensions de confiance.", RoutePath = "client/analysis", DisplayOrder = 1, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new LearnTopic { Id = "7e5f6a8b-0005-0000-0000-000000000002", TopicId = "scoring-parametres", Title = "Scoring et paramètres", Summary = "Les paramètres contribuent au score final. Chacun est explicatif, pas décisionnel.", RoutePath = "client/parameters", DisplayOrder = 2, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new LearnTopic { Id = "7e5f6a8b-0005-0000-0000-000000000003", TopicId = "pea-eligibilite", Title = "Éligibilité PEA", Summary = "Comprendre pourquoi une valeur est éligible ou non au PEA.", RoutePath = "client/instruments", DisplayOrder = 3, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new LearnTopic { Id = "7e5f6a8b-0005-0000-0000-000000000004", TopicId = "gestion-risque", Title = "Gestion du risque", Summary = "PredictFinance ne conseille pas. Il informe. Comprendre la différence.", RoutePath = "client/learn", DisplayOrder = 4, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate },
                new LearnTopic { Id = "7e5f6a8b-0005-0000-0000-000000000005", TopicId = "historique-analyses", Title = "Historique des analyses", Summary = "Comment exploiter l'historique pour comprendre l'évolution d'un signal.", RoutePath = "client/history", DisplayOrder = 5, IsActive = true, IsPublished = true, IsDeleted = false, CreatedAtUtc = seedDate }
            ];
        }

        private static GlossaryTerm[] BuildGlossaryTermSeedEntries()
        {
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return
            [
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000001", Term = "PEA", NormalizedTerm = "pea", Definition = "Plan d'Épargne en Actions : enveloppe fiscale permettant d'investir en actions européennes avec exonération d'impôt après 5 ans de détention.", Category = GlossaryTermEnum.PEA, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000002", Term = "PER", NormalizedTerm = "per", Definition = "Plan d'Épargne Retraite : produit d'épargne long terme permettant de déduire les versements du revenu imposable et de préparer la retraite.", Category = GlossaryTermEnum.PER, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000003", Term = "PEL", NormalizedTerm = "pel", Definition = "Plan d'Épargne Logement : produit d'épargne réglementé orienté vers l'acquisition immobilière, ouvrant droit à un prêt à taux préférentiel.", Category = GlossaryTermEnum.PEL, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000004", Term = "Assurance vie", NormalizedTerm = "assurance vie", Definition = "Contrat d'épargne souple permettant de faire fructifier un capital sur le long terme, avec des avantages fiscaux et successoraux.", Category = GlossaryTermEnum.AssuranceVie, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000005", Term = "Unités de compte (UC)", NormalizedTerm = "unites de compte uc", Definition = "Supports d'investissement non garantis en capital au sein d'une assurance vie ou d'un PER, investis sur des marchés financiers (actions, obligations, immobilier).", Category = GlossaryTermEnum.AssuranceVie, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000006", Term = "Fonds en euros", NormalizedTerm = "fonds en euros", Definition = "Support à capital garanti disponible dans les contrats d'assurance vie. Le rendement est généralement plus modéré que les unités de compte.", Category = GlossaryTermEnum.AssuranceVie, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000007", Term = "Abattement", NormalizedTerm = "abattement", Definition = "Réduction appliquée sur la base imposable avant calcul de l'impôt. Exemple : abattement de 4 600 € sur les gains d'une assurance vie après 8 ans.", Category = GlossaryTermEnum.General, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000008", Term = "Plafond de versement", NormalizedTerm = "plafond de versement", Definition = "Montant maximum que l'on peut verser sur un produit d'épargne réglementé. Exemple : 150 000 € pour un PEA classique.", Category = GlossaryTermEnum.General, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000009", Term = "Prélèvements sociaux", NormalizedTerm = "prelevements sociaux", Definition = "Cotisations (CSG, CRDS, etc.) prélevées sur les revenus du capital. Leur taux global est de 17,2 % en France.", Category = GlossaryTermEnum.General, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000010", Term = "Flat tax (PFU)", NormalizedTerm = "flat tax pfu", Definition = "Prélèvement Forfaitaire Unique de 30 % (12,8 % d'impôt + 17,2 % de prélèvements sociaux) applicable aux revenus du capital depuis 2018.", Category = GlossaryTermEnum.General, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000011", Term = "Versement programmé", NormalizedTerm = "versement programme", Definition = "Versement automatique et régulier sur un produit d'épargne. Permet de lisser le coût d'entrée et de discipliner l'épargne.", Category = GlossaryTermEnum.General, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000012", Term = "Arbitrage", NormalizedTerm = "arbitrage", Definition = "Opération consistant à transférer des fonds d'un support à un autre au sein d'un même contrat (assurance vie, PER) sans sortie de l'enveloppe fiscale.", Category = GlossaryTermEnum.AssuranceVie, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000013", Term = "Rachat (assurance vie)", NormalizedTerm = "rachat assurance vie", Definition = "Retrait total ou partiel d'une assurance vie. Un rachat partiel laisse le contrat ouvert ; un rachat total entraîne la clôture du contrat.", Category = GlossaryTermEnum.AssuranceVie, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000014", Term = "Liquidité / Disponibilité", NormalizedTerm = "liquidite disponibilite", Definition = "Capacité à récupérer son capital rapidement sans perte significative. Certains produits (PEL, PER) ont des contraintes de disponibilité.", Category = GlossaryTermEnum.General, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate },
                new GlossaryTerm { Id = "4a2b3c5d-0002-0000-0000-000000000015", Term = "Horizon de placement", NormalizedTerm = "horizon de placement", Definition = "Durée pendant laquelle l'investisseur prévoit de conserver son placement. Un horizon long permet généralement de prendre plus de risque pour viser un rendement supérieur.", Category = GlossaryTermEnum.General, IsActive = true, IsPublished = true, CreatedAtUtc = seedDate }
            ];
        }
    }
}
