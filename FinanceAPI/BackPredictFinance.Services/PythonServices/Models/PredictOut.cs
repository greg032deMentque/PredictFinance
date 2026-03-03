using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackPredictFinance.Services.PythonServices.Models
{
    public class PredictOut
    {
        public string Symbol { get; set; } = "";
        public DateTime PredictedAt { get; set; }
        public string Pattern { get; set; } = "DOUBLE_TOP";
        public decimal LastProbability { get; set; }
        public decimal MeanProbability { get; set; }
        public decimal MaxProbability { get; set; }
        public decimal ProbabilityPct { get; set; }
        public decimal MeanProbabilityPct { get; set; }
        public decimal MaxProbabilityPct { get; set; }
        public int NWindows { get; set; }
        public string SuggestedAction { get; set; } = "hold";
        public decimal ActionConfidence { get; set; }
        public string ActionReason { get; set; } = "";
        public List<PatternPrediction> Patterns { get; set; } = new();
    }
}
