export class ClientSimulationRequest {
  Symbol = '';
  Pattern = '';
  InvestmentAmount = 0;
  HorizonDays = 30;

  constructor(init?: Partial<ClientSimulationRequest>) {
    Object.assign(this, init);
  }
}

export class ClientMultiSimulationRequest {
  Symbol = '';
  Patterns: string[] = [];
  InvestmentAmount = 0;
  HorizonDays = 30;

  constructor(init?: Partial<ClientMultiSimulationRequest>) {
    Object.assign(this, init);
  }
}
