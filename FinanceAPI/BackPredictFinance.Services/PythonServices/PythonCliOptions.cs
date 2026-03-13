namespace BackPredictFinance.Services.PythonServices
{
    public class PythonCliOptions
    {
        public string RuntimeBaseUrl { get; set; } = "http://127.0.0.1:8000";
        public string PythonExe { get; set; } = @"..\..\FinanceIA\.venv\Scripts\python.exe";
        public string WorkingDirectory { get; set; } = @"..\..\FinanceIA";
        public string ModelDir { get; set; } = "artifacts/double_top";
        public string ModelVersion { get; set; } = "double_top@v1";
        public string DefaultPattern { get; set; } = "DOUBLE_TOP";
        public Dictionary<string, PythonCliPatternOptions> Patterns { get; set; } = new()
        {
            ["DOUBLE_TOP"] = new()
            {
                Enabled = true,
                ModelDir = "artifacts/double_top",
                ModelVersion = "double_top@v1"
            }
        };
        public string Period { get; set; } = "6mo";
        public int TimeoutSeconds { get; set; } = 30;
        public decimal SellThreshold { get; set; } = 0.65m;
        public decimal BuyThreshold { get; set; } = 0.20m;
        public decimal MinPrecision { get; set; } = 0.55m;
        public decimal MinF1 { get; set; } = 0.45m;
        public decimal MinRocAuc { get; set; } = 0.60m;
        public int MinPositives { get; set; } = 20;
    }

    public sealed class PythonCliPatternOptions
    {
        public bool Enabled { get; set; } = true;
        public string ModelDir { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
    }
}
