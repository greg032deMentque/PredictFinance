using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.Common.MarketData;
using BackPredictFinance.Patterns.Abstractions;
using BackPredictFinance.Patterns.Common;

namespace BackPredictFinance.Patterns.Definitions
{
    /// <summary>
    /// Détecte un triangle symétrique : compression bilatérale où les plus-hauts décroissent et les
    /// plus-bas croissent (régression linéaire sur chaque borne). Comme le rectangle, la direction
    /// vient de la tendance préalable, pas de la géométrie du triangle elle-même — seul un breakout
    /// dans le sens de cette tendance confirme la continuation.
    /// </summary>
    public sealed class SymmetricalTriangleContinuationAnalysisPatternDefinition : ContinuationPatternDefinitionBase
    {
        public SymmetricalTriangleContinuationAnalysisPatternDefinition(IPatternMarketDataProvider marketDataProvider)
            : base(marketDataProvider)
        {
        }

        public override string PatternId => PatternIds.SymmetricalTriangleContinuation;
        public override string ModelVersion => "analysis-v1-symmetrical-triangle-continuation@lot3-atr";
        public override int HistoryLookbackMonths => 6;
        protected override int MinimumRequiredCandles => 48;
        protected override string DisplayName => "Symmetrical triangle continuation";
        protected override string PedagogicalDescription => "Compression bilaterale apres tendance, directionnelle seulement apres breakout confirme dans le sens de la tendance prealable.";
        protected override decimal HistoricalReliability => BulkowskiReliability.SymmetricalTriangleContinuation;

        protected override ContinuationPatternAnalysisState Analyze(AnalysisRequest request, IReadOnlyList<TickerCandle> candles)
        {
            if (candles.Count < MinimumRequiredCandles)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "insufficient_history",
                    PhaseLabel = "Historique insuffisant",
                    Status = PatternStatus.Forming,
                    IsCompatible = false,
                    StatusReason = "Le moteur n'a pas reçu assez de bougies pour évaluer ce pattern de manière déterministe.",
                    ValidationReason = "Une profondeur historique minimale est requise avant toute validation.",
                    InvalidationReason = "Aucune invalidation n'est interpretable avec un historique insuffisant.",
                    Confidence = 0m,
                    CurrentPrice = candles[^1].Close,
                    ScoreReasons = ["Le nombre minimal de bougies n'est pas atteint pour ce pattern."]
                };
            }
            var triangleWindow = PatternTechnicals.Tail(candles, PatternThresholds.PatternRangeWindowCandleCount).ToList();
            var priorWindow = candles.Take(Math.Max(candles.Count - triangleWindow.Count, 0)).TakeLast(PatternThresholds.PriorTrendWindowCandleCount).ToList();
            var currentPrice = candles[^1].Close;
            var highs = triangleWindow.Select(candle => candle.High).ToList();
            var lows = triangleWindow.Select(candle => candle.Low).ToList();
            var (upperSlope, upperIntercept) = PatternTechnicals.ComputeLinearFit(highs);
            var (lowerSlope, lowerIntercept) = PatternTechnicals.ComputeLinearFit(lows);
            var firstHalfHigh = triangleWindow.Take(triangleWindow.Count / 2).Max(candle => candle.High);
            var secondHalfHigh = triangleWindow.Skip(triangleWindow.Count / 2).Max(candle => candle.High);
            var firstHalfLow = triangleWindow.Take(triangleWindow.Count / 2).Min(candle => candle.Low);
            var secondHalfLow = triangleWindow.Skip(triangleWindow.Count / 2).Min(candle => candle.Low);
            var startingHeight = firstHalfHigh - firstHalfLow;
            var endingHeight = secondHalfHigh - secondHalfLow;
            // Bornes projetees a la derniere bougie via la droite de regression (ordonnee a
            // l'origine + pente x index), et non ancrees sur le plus-haut/plus-bas de la premiere
            // bougie, qui pouvait etre une meche extreme et fausser la detection du breakout.
            var lastIndex = triangleWindow.Count - 1;
            var upperBoundary = upperIntercept + (upperSlope * lastIndex);
            var lowerBoundary = lowerIntercept + (lowerSlope * lastIndex);
            var atr = PatternTechnicals.VolatilityUnit(triangleWindow, currentPrice);
            var priorTrend = PatternTechnicals.ResolveDirectionalTrend(priorWindow);
            // Compression exigée sur DEUX preuves indépendantes et cohérentes entre elles : la pente
            // de régression (upperSlope négative, lowerSlope positive) ET la comparaison brute entre
            // première et seconde moitié de la fenêtre (secondHalfHigh < firstHalfHigh, secondHalfLow
            // > firstHalfLow). Exiger les deux évite qu'une régression bruitée par quelques mèches ne
            // valide seule une compression qui n'est pas visible sur les extrêmes réels des bougies.
            var hasCompression = startingHeight > 0m
                && endingHeight > 0m
                && endingHeight < startingHeight
                && upperSlope < 0m
                && lowerSlope > 0m
                && secondHalfHigh < firstHalfHigh
                && secondHalfLow > firstHalfLow;
            var breakoutUp = currentPrice > upperBoundary + (PatternThresholds.BreakoutAtrMultiple * atr);
            var breakoutDown = currentPrice < lowerBoundary - (PatternThresholds.BreakoutAtrMultiple * atr);
            var targetUp = currentPrice + startingHeight;
            var targetDown = currentPrice - startingHeight;
            var confidence = BuildConfidence(hasCompression, priorTrend, breakoutUp, breakoutDown);

            if (!hasCompression)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "compression_not_confirmed",
                    PhaseLabel = "Compression triangulaire insuffisante",
                    Status = PatternStatus.Forming,
                    IsCompatible = false,
                    StatusReason = "Les bornes convergentes ne sont pas suffisamment coherentes pour un triangle symetrique.",
                    ValidationReason = "Le breakout ne peut pas etre valide tant que la compression n'est pas lisible.",
                    InvalidationReason = "Aucune invalidation directionnelle exploitable tant que la structure n'est pas convergente.",
                    Confidence = confidence,
                    CurrentPrice = currentPrice,
                    ReferencePrice = upperBoundary,
                    InvalidationPrice = lowerBoundary,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(triangleWindow[^1].Date, ("upper_boundary", upperBoundary), ("lower_boundary", lowerBoundary)),
                    ScoreReasons = ["Le triangle symetrique exige des plus hauts decroissants et des plus bas croissants."]
                };
            }

            if (priorTrend == DirectionalTrend.None)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "bilateral_triangle_without_prior_trend",
                    PhaseLabel = "Triangle bilateral sans tendance prealable exploitable",
                    Status = PatternStatus.Monitoring,
                    IsCompatible = false,
                    StatusReason = "La compression existe, mais sans tendance prealable significative la lecture stricte de continuation reste trop faible.",
                    ValidationReason = "Une sortie de triangle seule n'est pas suffisante pour qualifier une continuation sans tendance prealable.",
                    InvalidationReason = "Le triangle garde une lecture bilaterale tant que le contexte de continuation n'est pas etabli.",
                    Confidence = confidence,
                    CurrentPrice = currentPrice,
                    ReferencePrice = upperBoundary,
                    InvalidationPrice = lowerBoundary,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(triangleWindow[^1].Date, ("upper_boundary", upperBoundary), ("lower_boundary", lowerBoundary)),
                    ScoreReasons = ["La structure est bilaterale avant breakout.", "Le contexte de continuation reste insuffisant sans tendance prealable."]
                };
            }

            if (priorTrend == DirectionalTrend.Up && breakoutUp)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "bullish_breakout_confirmed",
                    PhaseLabel = "Breakout haussier confirme",
                    Status = PatternStatus.Confirmed,
                    IsCompatible = true,
                    StatusReason = "Le prix cloture au-dessus de la borne haute du triangle dans le sens de la tendance haussiere prealable.",
                    IsValidated = true,
                    ValidationReason = "Le breakout valide la continuation haussiere du triangle symetrique.",
                    ValidationRuleCode = "SYMMETRICAL_TRIANGLE_UPSIDE_BREAKOUT_CLOSE",
                    InvalidationReason = "Le scenario reste actif tant que le prix ne retombe pas durablement dans le triangle.",
                    Confidence = Math.Max(confidence, 0.74m),
                    CurrentPrice = currentPrice,
                    ReferencePrice = upperBoundary,
                    TargetPrice = targetUp,
                    InvalidationPrice = upperBoundary,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(triangleWindow[^1].Date, ("upper_boundary", upperBoundary), ("lower_boundary", lowerBoundary), ("breakout", currentPrice)),
                    ScoreReasons = ["La compression etait convergente.", "Le breakout suit la tendance haussiere prealable.", "La cible pedagogique projette la hauteur du triangle depuis le breakout."]
                };
            }

            if (priorTrend == DirectionalTrend.Down && breakoutDown)
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "bearish_breakout_confirmed",
                    PhaseLabel = "Breakout baissier confirme",
                    Status = PatternStatus.Confirmed,
                    IsCompatible = true,
                    StatusReason = "Le prix cloture sous la borne basse du triangle dans le sens de la tendance baissiere prealable.",
                    IsValidated = true,
                    ValidationReason = "Le breakout valide la continuation baissiere du triangle symetrique.",
                    ValidationRuleCode = "SYMMETRICAL_TRIANGLE_DOWNSIDE_BREAKOUT_CLOSE",
                    InvalidationReason = "Le scenario reste actif tant que le prix ne remonte pas durablement dans le triangle.",
                    Confidence = Math.Max(confidence, 0.74m),
                    CurrentPrice = currentPrice,
                    ReferencePrice = lowerBoundary,
                    TargetPrice = targetDown,
                    InvalidationPrice = lowerBoundary,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(triangleWindow[^1].Date, ("upper_boundary", upperBoundary), ("lower_boundary", lowerBoundary), ("breakout", currentPrice)),
                    ScoreReasons = ["La compression etait convergente.", "Le breakout suit la tendance baissiere prealable.", "La cible pedagogique projette la hauteur du triangle depuis le breakout."]
                };
            }

            if ((priorTrend == DirectionalTrend.Up && breakoutDown) || (priorTrend == DirectionalTrend.Down && breakoutUp))
            {
                return new ContinuationPatternAnalysisState
                {
                    PhaseCode = "opposite_breakout_invalidated",
                    PhaseLabel = "Breakout oppose a la continuation",
                    Status = PatternStatus.Invalidated,
                    IsCompatible = false,
                    StatusReason = "Le triangle se resolout a l'oppose du scenario de continuation attendu.",
                    ValidationReason = "La direction de sortie ne confirme pas la these de continuation attendue.",
                    IsInvalidated = true,
                    InvalidationReason = "La sortie opposee invalide la lecture stricte de continuation du triangle.",
                    InvalidationRuleCode = "SYMMETRICAL_TRIANGLE_OPPOSITE_BREAKOUT",
                    Confidence = 0.20m,
                    CurrentPrice = currentPrice,
                    ReferencePrice = priorTrend == DirectionalTrend.Up ? lowerBoundary : upperBoundary,
                    InvalidationPrice = priorTrend == DirectionalTrend.Up ? lowerBoundary : upperBoundary,
                    StructuralPoints = PatternTechnicals.BuildBoundaryPoints(triangleWindow[^1].Date, ("upper_boundary", upperBoundary), ("lower_boundary", lowerBoundary), ("breakout", currentPrice)),
                    ScoreReasons = ["La structure existe mais le breakout est oppose a la these de continuation."]
                };
            }

            return new ContinuationPatternAnalysisState
            {
                PhaseCode = priorTrend == DirectionalTrend.Up ? "bullish_triangle_compressing" : "bearish_triangle_compressing",
                PhaseLabel = priorTrend == DirectionalTrend.Up ? "Triangle de continuation haussier en compression" : "Triangle de continuation baissier en compression",
                Status = PatternStatus.Monitoring,
                IsCompatible = true,
                StatusReason = "La compression est visible, mais la direction reste non confirmee tant que le prix reste dans le triangle.",
                ValidationReason = "Une cloture hors borne dans le sens de la tendance prealable reste necessaire.",
                InvalidationReason = "Une sortie opposee ou une reintegration apres breakout affaiblirait la these de continuation.",
                Confidence = Math.Max(confidence, 0.50m),
                CurrentPrice = currentPrice,
                ReferencePrice = priorTrend == DirectionalTrend.Up ? upperBoundary : lowerBoundary,
                InvalidationPrice = priorTrend == DirectionalTrend.Up ? lowerBoundary : upperBoundary,
                StructuralPoints = PatternTechnicals.BuildBoundaryPoints(triangleWindow[^1].Date, ("upper_boundary", upperBoundary), ("lower_boundary", lowerBoundary)),
                ScoreReasons = ["Le triangle reste bilateral tant qu'il n'est pas casse.", "La tendance prealable oriente la lecture de continuation mais ne vaut pas confirmation."]
            };
        }

        // Pondération similaire au rectangle (structure + tendance préalable + breakout), sans le
        // bonus de "touches" puisqu'un triangle n'a pas de bornes horizontales répétées à compter.
        private static decimal BuildConfidence(bool hasCompression, DirectionalTrend priorTrend, bool breakoutUp, bool breakoutDown)
        {
            var confidence = 0.15m;
            if (hasCompression)
            {
                confidence += 0.25m;
            }

            if (priorTrend != DirectionalTrend.None)
            {
                confidence += 0.15m;
            }

            if (breakoutUp || breakoutDown)
            {
                confidence += 0.20m;
            }

            return PatternTechnicals.Clamp01(confidence);
        }
    }
}
