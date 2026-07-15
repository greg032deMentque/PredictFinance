using BackPredictFinance.Common.enums;

namespace BackPredictFinance.Patterns.Common
{
    public static class PatternDirectionResolver
    {
        /// <summary>
        /// Deduit la direction du pattern (haussier/baissier) a partir de la position relative
        /// de la cible et du niveau d'invalidation, sans dependre du statut de detection.
        /// Cible au-dessus de l'invalidation => haussier (on vise le haut, on coupe en dessous) ;
        /// cible en dessous => baissier. Retourne Unknown si l'une des deux valeurs manque encore
        /// (pattern pas assez forme pour projeter cible/invalidation) ou si elles sont egales.
        /// </summary>
        public static PatternDirectionEnum Resolve(decimal? targetPrice, decimal? invalidationPrice)
        {
            if (!targetPrice.HasValue || !invalidationPrice.HasValue)
            {
                return PatternDirectionEnum.Unknown;
            }

            if (targetPrice.Value > invalidationPrice.Value)
            {
                return PatternDirectionEnum.Bullish;
            }

            if (targetPrice.Value < invalidationPrice.Value)
            {
                return PatternDirectionEnum.Bearish;
            }

            return PatternDirectionEnum.Unknown;
        }
    }
}
