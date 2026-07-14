using BackPredictFinance.Common.enums;
using BackPredictFinance.Datas.Entities;
using BackPredictFinance.ViewModels.AdminViewModels.Instruments;
using Microsoft.EntityFrameworkCore;

namespace BackPredictFinance.Services.AdminGovernance
{
    public interface IAdminInstrumentSeedService
    {
        Task<AdminInstrumentSeedResultViewModel> SeedAsync(CancellationToken ct = default);
    }

    public sealed class AdminInstrumentSeedService : BaseService, IAdminInstrumentSeedService
    {
        private const string PeaUniverseId = "CAC40_FR";
        private const string PeaPolicyVersion = "pea-seed-v1";
        private const string PeaSourceReference = "AMF-EEE-eligibility-seed";

        public AdminInstrumentSeedService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task<AdminInstrumentSeedResultViewModel> SeedAsync(CancellationToken ct = default)
        {
            var catalog = BuildInstrumentCatalog();
            var existingSymbols = await _financeDbContext.Assets
                .AsNoTracking()
                .Select(x => x.Symbol)
                .ToHashSetAsync(ct);

            var assetsToInsert = new List<Asset>();
            var skippedSymbols = new List<string>();

            foreach (var entry in catalog)
            {
                if (existingSymbols.Contains(entry.Symbol))
                {
                    skippedSymbols.Add(entry.Symbol);
                    continue;
                }

                assetsToInsert.Add(new Asset
                {
                    Id = Guid.NewGuid().ToString(),
                    Symbol = entry.Symbol,
                    ProviderSymbol = entry.ProviderSymbol,
                    Name = entry.Name,
                    Isin = entry.Isin,
                    Exchange = entry.Exchange,
                    Currency = entry.Currency,
                    Country = entry.Country,
                    AssetType = AssetTypeEnum.Stock,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            if (assetsToInsert.Count > 0)
            {
                await _financeDbContext.Assets.AddRangeAsync(assetsToInsert, ct);
                await _financeDbContext.SaveChangesAsync(ct);
            }

            var peaAssets = assetsToInsert.Where(a => catalog.Any(c => c.Symbol == a.Symbol && c.IsPeaEligible)).ToList();
            var peaEntries = BuildPeaEntries(peaAssets);

            if (peaEntries.Count > 0)
            {
                await _financeDbContext.AssetPeaEligibilities.AddRangeAsync(peaEntries, ct);
                await _financeDbContext.SaveChangesAsync(ct);
            }

            return new AdminInstrumentSeedResultViewModel
            {
                AssetsCreated = assetsToInsert.Count,
                AssetsSkipped = skippedSymbols.Count,
                PeaEntriesCreated = peaEntries.Count,
                SkippedSymbols = skippedSymbols
            };
        }

        private static List<AssetPeaEligibility> BuildPeaEntries(List<Asset> peaAssets)
        {
            var now = DateTime.UtcNow;
            return peaAssets.Select(asset => new AssetPeaEligibility
            {
                Id = Guid.NewGuid().ToString(),
                AssetId = asset.Id,
                UniverseId = PeaUniverseId,
                EligibilityStatus = PeaEligibilityStatusEnum.ConfirmedEligible,
                SourceType = PeaEligibilitySourceTypeEnum.ExchangeReference,
                SourceReference = PeaSourceReference,
                CheckedUtc = now,
                PolicyVersion = PeaPolicyVersion,
                ReviewerNote = "Seeded — action française listée sur Euronext Paris, éligible PEA selon critères EEE.",
                CreatedAtUtc = now
            }).ToList();
        }

        private static List<InstrumentSeedEntry> BuildInstrumentCatalog()
        {
            return
            [
                // ── CAC 40 (ISIN exacts FR — éligibles PEA) ──────────────────────────────
                new("AI.PA",  "AI.PA",  "Air Liquide",                     "FR0000120073", "XPAR", "EUR", "FR", true),
                new("AIR.PA", "AIR.PA", "Airbus",                          "NL0000235190", "XPAR", "EUR", "FR", true),
                new("ALO.PA", "ALO.PA", "Alstom",                          "FR0010220475", "XPAR", "EUR", "FR", true),
                new("ATO.PA", "ATO.PA", "Atos",                            "FR0000051732", "XPAR", "EUR", "FR", true),
                new("BN.PA",  "BN.PA",  "Danone",                          "FR0000120644", "XPAR", "EUR", "FR", true),
                new("BNP.PA", "BNP.PA", "BNP Paribas",                     "FR0000131104", "XPAR", "EUR", "FR", true),
                new("CA.PA",  "CA.PA",  "Carrefour",                       "FR0000120172", "XPAR", "EUR", "FR", true),
                new("CAP.PA", "CAP.PA", "Capgemini",                       "FR0000125338", "XPAR", "EUR", "FR", true),
                new("CS.PA",  "CS.PA",  "AXA",                             "FR0000120628", "XPAR", "EUR", "FR", true),
                new("DG.PA",  "DG.PA",  "Vinci",                           "FR0000125486", "XPAR", "EUR", "FR", true),
                new("DSY.PA", "DSY.PA", "Dassault Systèmes",               "FR0014003TT8", "XPAR", "EUR", "FR", true),
                new("EL.PA",  "EL.PA",  "EssilorLuxottica",                "FR0000121667", "XPAR", "EUR", "FR", true),
                new("EN.PA",  "EN.PA",  "Bouygues",                        "FR0000120503", "XPAR", "EUR", "FR", true),
                new("ENGI.PA","ENGI.PA","Engie",                           "FR0010208488", "XPAR", "EUR", "FR", true),
                new("ERF.PA", "ERF.PA", "Eurofins Scientific",             "FR0014000MR3", "XPAR", "EUR", "FR", true),
                new("GLE.PA", "GLE.PA", "Société Générale",                "FR0000130809", "XPAR", "EUR", "FR", true),
                new("HO.PA",  "HO.PA",  "Thales",                          "FR0000121329", "XPAR", "EUR", "FR", true),
                new("KER.PA", "KER.PA", "Kering",                          "FR0000121485", "XPAR", "EUR", "FR", true),
                new("LR.PA",  "LR.PA",  "Legrand",                         "FR0010307819", "XPAR", "EUR", "FR", true),
                new("MC.PA",  "MC.PA",  "LVMH",                            "FR0000121014", "XPAR", "EUR", "FR", true),
                new("ML.PA",  "ML.PA",  "Michelin",                        "FR0000131906", "XPAR", "EUR", "FR", true),
                new("MT.AS",  "MT.AS",  "ArcelorMittal",                   "LU1598757687", "XPAR", "EUR", "FR", true),
                new("ORA.PA", "ORA.PA", "Orange",                          "FR0000133308", "XPAR", "EUR", "FR", true),
                new("PUB.PA", "PUB.PA", "Publicis Groupe",                 "FR0000130577", "XPAR", "EUR", "FR", true),
                new("RI.PA",  "RI.PA",  "Pernod Ricard",                   "FR0000120693", "XPAR", "EUR", "FR", true),
                new("RMS.PA", "RMS.PA", "Hermès International",            "FR0000052292", "XPAR", "EUR", "FR", true),
                new("RNO.PA", "RNO.PA", "Renault",                         "FR0000131963", "XPAR", "EUR", "FR", true),
                new("SAF.PA", "SAF.PA", "Safran",                          "FR0000073272", "XPAR", "EUR", "FR", true),
                new("SAN.PA", "SAN.PA", "Sanofi",                          "FR0000120578", "XPAR", "EUR", "FR", true),
                new("SGO.PA", "SGO.PA", "Saint-Gobain",                    "FR0000125007", "XPAR", "EUR", "FR", true),
                new("SK.PA",  "SK.PA",  "SEB",                             "FR0000121709", "XPAR", "EUR", "FR", true),
                new("STLAM.MI","STLAM.MI","Stellantis",                    "NL00150001Q9", "XPAR", "EUR", "FR", true),
                new("STM.PA", "STM.PA", "STMicroelectronics",              "NL0000226223", "XPAR", "EUR", "FR", true),
                new("SU.PA",  "SU.PA",  "Schneider Electric",              "FR0000121972", "XPAR", "EUR", "FR", true),
                new("TEC.PA", "TEC.PA", "Technip Energies",                "FR0014000WR5", "XPAR", "EUR", "FR", true),
                new("TTE.PA", "TTE.PA", "TotalEnergies",                   "FR0014000RC4", "XPAR", "EUR", "FR", true),
                new("URW.AS", "URW.AS", "Unibail-Rodamco-Westfield",       "FR0013326246", "XPAR", "EUR", "FR", true),
                new("VIE.PA", "VIE.PA", "Veolia Environnement",            "FR0000124141", "XPAR", "EUR", "FR", true),
                new("VIV.PA", "VIV.PA", "Vivendi",                         "FR0000127771", "XPAR", "EUR", "FR", true),
                new("WLN.PA", "WLN.PA", "Worldline",                       "FR0011981968", "XPAR", "EUR", "FR", true),

                // ── US Large Caps (ISIN = null — non éligibles PEA) ─────────────────────
                new("AAPL",  "AAPL",  "Apple",                             null, "NASDAQ", "USD", "US", false),
                new("MSFT",  "MSFT",  "Microsoft",                         null, "NASDAQ", "USD", "US", false),
                new("NVDA",  "NVDA",  "NVIDIA",                            null, "NASDAQ", "USD", "US", false),
                new("AMZN",  "AMZN",  "Amazon",                            null, "NASDAQ", "USD", "US", false),
                new("GOOGL", "GOOGL", "Alphabet (Class A)",                null, "NASDAQ", "USD", "US", false),
                new("META",  "META",  "Meta Platforms",                    null, "NASDAQ", "USD", "US", false),
                new("TSLA",  "TSLA",  "Tesla",                             null, "NASDAQ", "USD", "US", false),
                new("BRK-B", "BRK-B", "Berkshire Hathaway B",             null, "NYSE",   "USD", "US", false),
                new("JPM",   "JPM",   "JPMorgan Chase",                    null, "NYSE",   "USD", "US", false),
                new("V",     "V",     "Visa",                              null, "NYSE",   "USD", "US", false),
                new("XOM",   "XOM",   "ExxonMobil",                        null, "NYSE",   "USD", "US", false),
                new("UNH",   "UNH",   "UnitedHealth Group",                null, "NYSE",   "USD", "US", false),
                new("JNJ",   "JNJ",   "Johnson & Johnson",                 null, "NYSE",   "USD", "US", false),
                new("WMT",   "WMT",   "Walmart",                           null, "NYSE",   "USD", "US", false),
                new("MA",    "MA",    "Mastercard",                        null, "NYSE",   "USD", "US", false),
                new("PG",    "PG",    "Procter & Gamble",                  null, "NYSE",   "USD", "US", false),
                new("LLY",   "LLY",   "Eli Lilly",                         null, "NYSE",   "USD", "US", false),
                new("CVX",   "CVX",   "Chevron",                           null, "NYSE",   "USD", "US", false),
                new("HD",    "HD",    "Home Depot",                        null, "NYSE",   "USD", "US", false),
                new("ABBV",  "ABBV",  "AbbVie",                            null, "NYSE",   "USD", "US", false),
                new("MRK",   "MRK",   "Merck",                             null, "NYSE",   "USD", "US", false),
                new("AVGO",  "AVGO",  "Broadcom",                          null, "NASDAQ", "USD", "US", false),
                new("COST",  "COST",  "Costco Wholesale",                  null, "NASDAQ", "USD", "US", false),
                new("PEP",   "PEP",   "PepsiCo",                           null, "NASDAQ", "USD", "US", false),
                new("KO",    "KO",    "Coca-Cola",                         null, "NYSE",   "USD", "US", false),
                new("BAC",   "BAC",   "Bank of America",                   null, "NYSE",   "USD", "US", false),
                new("ORCL",  "ORCL",  "Oracle",                            null, "NYSE",   "USD", "US", false),
                new("CRM",   "CRM",   "Salesforce",                        null, "NYSE",   "USD", "US", false),
                new("TMO",   "TMO",   "Thermo Fisher Scientific",          null, "NYSE",   "USD", "US", false),
                new("CSCO",  "CSCO",  "Cisco Systems",                     null, "NASDAQ", "USD", "US", false),
                new("AMD",   "AMD",   "Advanced Micro Devices",            null, "NASDAQ", "USD", "US", false),
                new("ACN",   "ACN",   "Accenture",                         null, "NYSE",   "USD", "US", false),
                new("MCD",   "MCD",   "McDonald's",                        null, "NYSE",   "USD", "US", false),
                new("ABT",   "ABT",   "Abbott Laboratories",               null, "NYSE",   "USD", "US", false),
                new("NKE",   "NKE",   "Nike",                              null, "NYSE",   "USD", "US", false),
                new("INTC",  "INTC",  "Intel",                             null, "NASDAQ", "USD", "US", false),
                new("DIS",   "DIS",   "Walt Disney",                       null, "NYSE",   "USD", "US", false),
                new("TXN",   "TXN",   "Texas Instruments",                 null, "NASDAQ", "USD", "US", false),
                new("QCOM",  "QCOM",  "QUALCOMM",                          null, "NASDAQ", "USD", "US", false),
                new("NEE",   "NEE",   "NextEra Energy",                    null, "NYSE",   "USD", "US", false),
                new("PM",    "PM",    "Philip Morris International",       null, "NYSE",   "USD", "US", false),
                new("LOW",   "LOW",   "Lowe's Companies",                  null, "NYSE",   "USD", "US", false),
                new("UPS",   "UPS",   "United Parcel Service",             null, "NYSE",   "USD", "US", false),
                new("RTX",   "RTX",   "RTX Corporation",                   null, "NYSE",   "USD", "US", false),
                new("CAT",   "CAT",   "Caterpillar",                       null, "NYSE",   "USD", "US", false),
                new("INTU",  "INTU",  "Intuit",                            null, "NASDAQ", "USD", "US", false),
                new("GS",    "GS",    "Goldman Sachs",                     null, "NYSE",   "USD", "US", false),
                new("AXP",   "AXP",   "American Express",                  null, "NYSE",   "USD", "US", false),
                new("BLK",   "BLK",   "BlackRock",                         null, "NYSE",   "USD", "US", false),
                new("SPGI",  "SPGI",  "S&P Global",                        null, "NYSE",   "USD", "US", false),
                new("ADI",   "ADI",   "Analog Devices",                    null, "NASDAQ", "USD", "US", false),
            ];
        }

        private sealed record InstrumentSeedEntry(
            string Symbol,
            string ProviderSymbol,
            string Name,
            string? Isin,
            string Exchange,
            string Currency,
            string Country,
            bool IsPeaEligible);
    }
}
