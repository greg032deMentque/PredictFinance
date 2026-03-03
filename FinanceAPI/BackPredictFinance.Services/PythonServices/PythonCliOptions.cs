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
    }
}
