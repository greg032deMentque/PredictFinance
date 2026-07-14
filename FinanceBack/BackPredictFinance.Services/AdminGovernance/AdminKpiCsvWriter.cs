using System.Globalization;
using System.Text;
using BackPredictFinance.ViewModels.AdminViewModels.Kpi;
using BackPredictFinance.ViewModels.AdminViewModels.SignalQuality;

namespace BackPredictFinance.Services.AdminGovernance
{
    /// <summary>
    /// Sérialise les ViewModels KPI admin en CSV multi-sections (séparateur ';', UTF-8 BOM).
    /// Les taux sont émis en décimal brut (InvariantCulture) pour rester recalculables à la réimportation.
    /// </summary>
    public static class AdminKpiCsvWriter
    {
        private const char Separator = ';';

        public static byte[] BuildEngagementCsv(AdminEngagementKpiViewModel kpi)
        {
            var builder = new StringBuilder();

            AppendLine(builder, "Synthèse");
            AppendRow(builder, "Indicateur", "Valeur");
            AppendRow(builder, "Window", kpi.Window);
            AppendRow(builder, "DAU", Num(kpi.Dau));
            AppendRow(builder, "WAU", Num(kpi.Wau));
            AppendRow(builder, "MAU", Num(kpi.Mau));
            AppendRow(builder, "Stickiness", Num(kpi.Stickiness));
            AppendRow(builder, "ActiveUsers", Num(kpi.ActiveUsers));
            AppendRow(builder, "NotificationReadRate", Num(kpi.NotificationReadRate));
            AppendRow(builder, "OpsSuccessRate", Num(kpi.OpsSuccessRate));
            AppendRow(builder, "OpsAvgDurationMs", Num(kpi.OpsAvgDurationMs));
            AppendRow(builder, "StaleAssets", Num(kpi.StaleAssets));

            builder.AppendLine();
            AppendLine(builder, "Funnel d'activation");
            AppendRow(builder, "Step", "Label", "Count", "Rate");
            foreach (var step in kpi.ActivationFunnel)
                AppendRow(builder, Num(step.Step), step.Label, Num(step.Count), Num(step.Rate));

            builder.AppendLine();
            AppendLine(builder, "Cohortes de rétention");
            AppendRow(builder, "Label", "Rate", "SampleSize");
            foreach (var cohort in kpi.RetentionCohorts)
                AppendRow(builder, cohort.Label, Num(cohort.Rate), Num(cohort.SampleSize));

            return Encode(builder);
        }

        public static byte[] BuildSignalQualityCsv(AdminSignalQualityKpiViewModel kpi)
        {
            var builder = new StringBuilder();

            AppendLine(builder, "Synthèse");
            AppendRow(builder, "Indicateur", "Valeur");
            AppendRow(builder, "Window", kpi.Window);
            AppendRow(builder, "OverallTargetHitRate", Num(kpi.OverallTargetHitRate));
            AppendRow(builder, "TotalEvaluated", Num(kpi.TotalEvaluated));
            AppendRow(builder, "OpenSignals", Num(kpi.OpenSignals));
            AppendRow(builder, "NotEvaluable", Num(kpi.NotEvaluable));

            builder.AppendLine();
            AppendLine(builder, "Calibration par niveau de confiance");
            AppendRow(builder, "Label", "TotalSignals", "TargetHits", "HitRate");
            foreach (var row in kpi.ConfidenceCalibration)
                AppendRow(builder, row.Label, Num(row.TotalSignals), Num(row.TargetHits), Num(row.HitRate));

            builder.AppendLine();
            AppendLine(builder, "Performance par pattern");
            AppendRow(builder, "PatternId", "TotalEvaluated", "TargetHitRate", "AvgConfidence");
            foreach (var row in kpi.PatternPerformance)
                AppendRow(builder, row.PatternId, Num(row.TotalEvaluated), Num(row.TargetHitRate), Num(row.AvgConfidence));

            builder.AppendLine();
            AppendLine(builder, "Performance par version de modèle");
            AppendRow(builder, "ModelVersion", "TotalEvaluated", "TargetHitRate");
            foreach (var row in kpi.ModelPerformance)
                AppendRow(builder, row.ModelVersion, Num(row.TotalEvaluated), Num(row.TargetHitRate));

            return Encode(builder);
        }

        private static void AppendLine(StringBuilder builder, string title) =>
            builder.AppendLine(Escape(title));

        private static void AppendRow(StringBuilder builder, params string[] cells) =>
            builder.AppendLine(string.Join(Separator, cells.Select(Escape)));

        private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Num(decimal value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var mustQuote = value.IndexOf(Separator) >= 0
                || value.IndexOf('"') >= 0
                || value.IndexOf('\n') >= 0
                || value.IndexOf('\r') >= 0;

            if (!mustQuote)
                return value;

            return string.Concat("\"", value.Replace("\"", "\"\""), "\"");
        }

        private static byte[] Encode(StringBuilder builder)
        {
            var preamble = Encoding.UTF8.GetPreamble();
            var content = Encoding.UTF8.GetBytes(builder.ToString());
            var result = new byte[preamble.Length + content.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
            return result;
        }
    }
}
