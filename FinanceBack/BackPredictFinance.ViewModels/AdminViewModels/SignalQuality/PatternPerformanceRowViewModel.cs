namespace BackPredictFinance.ViewModels.AdminViewModels.SignalQuality
{
     public sealed class PatternPerformanceRowViewModel
  {
      public string PatternId { get; set; } = string.Empty;
      public int TotalEvaluated { get; set; }
      public decimal TargetHitRate { get; set; }
      public decimal AvgConfidence { get; set; }
  }

}