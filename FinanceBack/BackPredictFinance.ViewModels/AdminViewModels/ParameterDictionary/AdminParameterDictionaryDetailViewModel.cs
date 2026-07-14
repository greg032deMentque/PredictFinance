namespace BackPredictFinance.ViewModels.AdminViewModels.ParameterDictionary
{
    public sealed class AdminParameterDictionaryDetailViewModel
    {
        public string ParameterId { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public string DisplayLabel { get; set; } = string.Empty;
        public string RoleInCategory { get; set; } = string.Empty;
        public string SimpleDefinition { get; set; } = string.Empty;
        public string HowToRead { get; set; } = string.Empty;
        public string WhyItMatters { get; set; } = string.Empty;
        public string LimitsOfInterpretation { get; set; } = string.Empty;
        public string WhatItSupports { get; set; } = string.Empty;
        public string WhatItDoesNotProve { get; set; } = string.Empty;
        public string ImplicationWithoutPosition { get; set; } = string.Empty;
        public string ImplicationWithPosition { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsPublished { get; set; }
    }
}
