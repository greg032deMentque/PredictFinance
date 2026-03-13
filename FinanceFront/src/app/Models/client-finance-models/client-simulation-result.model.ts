export class ClientSimulationResult {
  Symbol = '';
  Phase = '';
  InvestmentAmount = 0;
  HorizonDays = 0;
  EstimatedReturnAmount = 0;
  EstimatedReturnPct = 0;
  EstimatedFinalAmount = 0;
  Recommendation = '';
  Assumption = '';
  CurrentPrice = 0;
  TargetPrice: number | null = null;
  InvalidationPrice: number | null = null;
  IsActionable = false;

  constructor(init?: Partial<ClientSimulationResult>) {
    Object.assign(this, init);
  }
}
