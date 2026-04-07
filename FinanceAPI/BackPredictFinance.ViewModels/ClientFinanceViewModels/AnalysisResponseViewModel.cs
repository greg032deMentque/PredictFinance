using BackPredictFinance.Common.AnalysisV1;
using BackPredictFinance.Common.enums;
using BackPredictFinance.ViewModels.ClientFinanceViewModels.AnalysisV1;
using System;
using System.Collections.Generic;
using System.Text;

namespace BackPredictFinance.ViewModels.ClientFinanceViewModels
{
    public sealed class AnalysisResponseViewModel
    {
        public string AnalysisId { get; set; } = string.Empty;
        public DateTime GeneratedAtUtc { get; set; }
        public DateOnly AsOfDate { get; set; }
        public AnalysisOutcome Outcome { get; set; }
        public Instrument Instrument { get; set; } = new();
        public List<string> RequestedPatternIds { get; set; } = [];
        public List<string> ExecutedPatternIds { get; set; } = [];
        public PatternAssessment? MainPattern { get; set; }
        public List<PatternAssessment> AlternativePatterns { get; set; } = [];
        public AnalysisRecommendation? Recommendation { get; set; }
        public string PedagogicalSummary { get; set; } = string.Empty;
        public string? NoCrediblePatternReason { get; set; }
        public AnalysisResponseTrace Trace { get; set; } = new();
        public List<string> Warnings { get; set; } = [];
        public ModelStatusEnum ModelStatus { get; set; } = ModelStatusEnum.NoGo;
        public string ModelMessage { get; set; } = string.Empty;
    }
}   