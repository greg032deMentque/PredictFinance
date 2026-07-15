export class MarketAssetOption {
  Symbol = '';
  CompanyName = '';
  Market = '';
  Currency = 'USD';
  LastPrice = 0;
  DayVariationPct = 0;
  Isin: string | null = null;
  IsPeaEligible = false;
  AssetType: string | null = null;
  Sector: string | null = null;
  Country: string | null = null;
  Summary: string | null = null;

  constructor(init?: Partial<MarketAssetOption>) {
    Object.assign(this, init);
  }
}
