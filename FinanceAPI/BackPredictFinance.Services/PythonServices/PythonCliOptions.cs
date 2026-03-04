namespace BackPredictFinance.Services.PythonServices
{
    public class PythonCliOptions
    {
        public string PythonExe { get; set; } = @"..\..\FinanceIA\.venv\Scripts\python.exe";
        public string WorkingDirectory { get; set; } = @"..\..\FinanceIA";
        public string ModelDir { get; set; } = "artifacts/double_top";
        public string Period { get; set; } = "6mo";
        public int TimeoutSeconds { get; set; } = 30;
        public decimal SellThreshold { get; set; } = 0.65m;
        public decimal BuyThreshold { get; set; } = 0.20m;
        public decimal MinPrecision { get; set; } = 0.55m;
        public decimal MinF1 { get; set; } = 0.45m;
        public decimal MinRocAuc { get; set; } = 0.60m;
        public int MinPositives { get; set; } = 20;
    }
}
