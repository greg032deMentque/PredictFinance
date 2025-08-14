using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Common.enums
{
    /// <summary>
    /// Liste des patterns techniques que votre IA reconnaît
    /// </summary>
    public enum TradingPatternEnum
    {
        HeadAndShoulders,
        DoubleTop,
        DoubleBottom,
        CupAndHandle,
        Triangle,
        // … ajoutez vos autres patterns
    }

    /// <summary>
    /// Action recommandée par l’IA
    /// </summary>
    public enum RecommendationActionEnum
    {
        Buy,
        Sell,
        Hold
    }
}
