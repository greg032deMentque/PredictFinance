using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;

namespace BackPredictFinance.Services.ClientFinanceServices.Analysis
{
    public enum SignalDirectionalHitKind
    {
        None,
        TargetHit,
        InvalidationHit
    }

    public sealed record SignalDirectionalHit
    {
        public required SignalDirectionalHitKind Kind { get; init; }
        public AssetCandleSnapshot? Candle { get; init; }
        public int CandleIndex { get; init; } = -1;
    }

    public static class SignalDirectionalScanEvaluator
    {
        /// <summary>
        /// Parcourt les bougies dans l'ordre chronologique fourni (l'appelant est responsable du tri :
        /// aucun tri n'est fait ici, donc un ordre inversé casserait silencieusement "premier hit") et
        /// retourne le premier événement (invalidation ou target) rencontré. À chaque bougie,
        /// l'invalidation est testée AVANT le target : en cas de bougie qui toucherait les deux niveaux
        /// le même jour (forte volatilité intraday), on retient le scénario le plus défavorable plutôt
        /// que le plus favorable — biais volontairement conservateur côté ex post.
        /// </summary>
        public static SignalDirectionalHit ScanForFirstHit(
            IReadOnlyList<AssetCandleSnapshot> orderedCandles,
            PatternDirectionEnum direction,
            decimal? targetPrice,
            decimal? invalidationPrice)
        {
            ArgumentNullException.ThrowIfNull(orderedCandles);

            for (var candleIndex = 0; candleIndex < orderedCandles.Count; candleIndex++)
            {
                var candle = orderedCandles[candleIndex];

                var invalidationHit = IsInvalidationHit(candle, direction, invalidationPrice);
                if (invalidationHit)
                {
                    return new SignalDirectionalHit { Kind = SignalDirectionalHitKind.InvalidationHit, Candle = candle, CandleIndex = candleIndex };
                }

                var targetHit = IsTargetHit(candle, direction, targetPrice);
                if (targetHit)
                {
                    return new SignalDirectionalHit { Kind = SignalDirectionalHitKind.TargetHit, Candle = candle, CandleIndex = candleIndex };
                }
            }

            return new SignalDirectionalHit { Kind = SignalDirectionalHitKind.None };
        }

        // Un pattern baissier vise un prix plus bas : le target est atteint quand le plus bas de la
        // bougie descend jusqu'au niveau cible (et inversement en haussier, sur le plus haut).
        private static bool IsTargetHit(AssetCandleSnapshot candle, PatternDirectionEnum direction, decimal? targetPrice)
        {
            if (!targetPrice.HasValue)
            {
                return false;
            }

            return direction == PatternDirectionEnum.Bearish
                ? candle.Low <= targetPrice.Value
                : candle.High >= targetPrice.Value;
        }

        // Logique symétrique à IsTargetHit : l'invalidation d'un pattern baissier se déclenche par une
        // remontée du prix (High), celle d'un pattern haussier par une chute (Low).
        private static bool IsInvalidationHit(AssetCandleSnapshot candle, PatternDirectionEnum direction, decimal? invalidationPrice)
        {
            if (!invalidationPrice.HasValue)
            {
                return false;
            }

            return direction == PatternDirectionEnum.Bearish
                ? candle.High >= invalidationPrice.Value
                : candle.Low <= invalidationPrice.Value;
        }
    }
}
