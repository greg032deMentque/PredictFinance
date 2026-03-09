export class ClientSimulationRequest {
  Symbol = '';
  Pattern = 'DOUBLE_TOP';
  InvestmentAmount = 0;
  HorizonDays = 30;

  constructor(init?: Partial<ClientSimulationRequest>) {
    Object.assign(this, init);
  }
}
