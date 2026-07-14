export class MarketAssetOption {
  Symbol = '';
  CompanyName = '';
  Market = '';
  Currency = 'USD';
  LastPrice = 0;
  DayVariationPct = 0;
  Isin: string | null = null;
  IsPeaEligible = false;

  constructor(init?: Partial<MarketAssetOption>) {
    Object.assign(this, init);
  }
}
