export class ClientAnalysisResult {
  Id = '';
  Symbol = '';
  CompanyName = '';
  Pattern = '';
  Confidence = 0;
  Recommendation = '';
  Reason = '';
  RiskLevel = '';
  HorizonDays = 0;
  PredictedAt = '';

  constructor(init?: Partial<ClientAnalysisResult>) {
    Object.assign(this, init);
  }
}
