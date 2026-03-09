export class ClientWatchlistItem {
  UserAssetId = '';
  Symbol = '';
  CompanyName = '';
  Market = '';
  LastPrice = 0;
  DayVariationPct = 0;
  HeldQuantity = 0;
  AverageBuyPrice = 0;
  InvestedAmount = 0;
  OutstandingAmount = 0;

  constructor(init?: Partial<ClientWatchlistItem>) {
    Object.assign(this, init);
  }
}
