import { CLIENT_DEFAULT_PATTERN, type ClientSupportedPattern } from './client-patterns.constants';

export class ClientSimulationRequest {
  Symbol = '';
  Pattern: ClientSupportedPattern = CLIENT_DEFAULT_PATTERN;
  InvestmentAmount = 0;
  HorizonDays = 30;

  constructor(init?: Partial<ClientSimulationRequest>) {
    Object.assign(this, init);
  }
}
