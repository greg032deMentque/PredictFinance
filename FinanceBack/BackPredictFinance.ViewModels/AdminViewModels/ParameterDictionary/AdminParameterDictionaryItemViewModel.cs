namespace BackPredictFinance.ViewModels.AdminViewModels.ParameterDictionary
{
    public sealed class AdminParameterDictionaryItemViewModel
    {
        public string ParameterId { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public string DisplayLabel { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
    }
}
