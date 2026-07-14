export class ClientTransactionItem {
  Id = '';
  Symbol = '';
  CompanyName = '';
  TransactionType = '';
  Quantity = 0;
  UnitPrice = 0;
  Fees = 0;
  GrossAmount = 0;
  NetAmount = 0;
  TimestampUtc = '';
  PortfolioId = '';
  PortfolioName = '';

  constructor(init?: Partial<ClientTransactionItem>) {
    Object.assign(this, init);
  }
}
