export class ClientTransactionCreateRequest {
  Symbol = '';
  TransactionType: 'Buy' | 'Sell' = 'Buy';
  Quantity = 0;
  UnitPrice = 0;
  Fees = 0;
  TimestampUtc = '';
  PortfolioId = '';

  constructor(init?: Partial<ClientTransactionCreateRequest>) {
    Object.assign(this, init);
  }
}
