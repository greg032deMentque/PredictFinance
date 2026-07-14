namespace BackPredictFinance.ViewModels.UserViewModels
{
    public sealed class DataExportResponseViewModel
    {
        public string Status { get; set; } = "Pending";
        public int EstimatedDeliveryHours { get; set; } = 72;
        public string Message { get; set; } = string.Empty;
    }
}
