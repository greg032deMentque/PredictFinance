namespace BackPredictFinance.Patterns.Common
{
    /// <summary>
    /// Fiabilité historique par figure, source : Bulkowski "Encyclopedia of Chart Patterns".
    /// Valeur = taux de réussite observé sur échantillon historique large (a posteriori, pas proba live).
    /// </summary>
    internal static class BulkowskiReliability
    {
        // Continuation
        public const decimal RectangleContinuation = 0.68m;
        public const decimal SymmetricalTriangleContinuation = 0.54m;
        public const decimal BullFlag = 0.67m;
        public const decimal BearFlag = 0.67m;

        // Retournements
        public const decimal HeadAndShoulders = 0.51m;
        public const decimal InverseHeadAndShoulders = 0.71m;
        public const decimal DoubleTop = 0.64m;
        public const decimal DoubleBottom = 0.65m;

        public static string BuildLabel(decimal reliability)
        {
            if (reliability >= 0.70m) return "FIABLE";
            if (reliability >= 0.55m) return "MODERE";
            return "FAIBLE";
        }
    }
}
