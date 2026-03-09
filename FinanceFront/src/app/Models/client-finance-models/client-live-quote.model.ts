export class ClientLiveQuote {
  Symbol = '';
  LastPrice = 0;
  DayVariationPct = 0;
  AsOfUtc = '';

  constructor(init?: Partial<ClientLiveQuote>) {
    Object.assign(this, init);
  }
}
