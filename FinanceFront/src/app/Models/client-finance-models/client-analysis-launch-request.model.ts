export class ClientAnalysisLaunchRequest {
  Symbol = '';
  RequestedPatternIds: string[] = [];

  constructor(init?: Partial<ClientAnalysisLaunchRequest>) {
    Object.assign(this, init);
  }
}
