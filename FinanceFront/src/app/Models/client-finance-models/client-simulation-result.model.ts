export class ClientSimulationResult {
  Symbol = '';
  InvestmentAmount = 0;
  HorizonDays = 0;
  EstimatedReturnAmount = 0;
  EstimatedReturnPct = 0;
  EstimatedFinalAmount = 0;
  Recommendation = '';
  Assumption = '';

  constructor(init?: Partial<ClientSimulationResult>) {
    Object.assign(this, init);
  }
}
