export class MarketAssetOption {
  Symbol = '';
  CompanyName = '';
  Market = '';
  Currency = 'USD';
  LastPrice = 0;
  DayVariationPct = 0;

  constructor(init?: Partial<MarketAssetOption>) {
    Object.assign(this, init);
  }
}
