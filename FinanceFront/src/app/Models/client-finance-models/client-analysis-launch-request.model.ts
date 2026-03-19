export class ClientAnalysisLaunchRequest {
  Symbol = '';
  RequestedPattern = 'DOUBLE_TOP';

  constructor(init?: Partial<ClientAnalysisLaunchRequest>) {
    Object.assign(this, init);
  }
}
