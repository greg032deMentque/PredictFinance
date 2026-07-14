namespace BackPredictFinance.ViewModels.AdminViewModels.Instruments
{
    public sealed class AdminInstrumentSeedResultViewModel
    {
        public int AssetsCreated { get; set; }
        public int AssetsSkipped { get; set; }
        public int PeaEntriesCreated { get; set; }
        public List<string> SkippedSymbols { get; set; } = [];
    }
}
