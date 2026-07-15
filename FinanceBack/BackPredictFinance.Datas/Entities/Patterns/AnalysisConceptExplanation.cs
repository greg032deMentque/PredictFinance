namespace BackPredictFinance.Datas.Entities
{
    /// <summary>
    /// Explication pédagogique d'un concept d'analyse technique (support, résistance, touches,
    /// force, continuation, retournement…). Donnée de référence, éditable en base.
    /// </summary>
    public sealed class AnalysisConceptExplanation
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }
}
