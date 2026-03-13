export class ClientAnalysisResult {
  Id = '';
  Symbol = '';
  CompanyName = '';
  Pattern = '';
  Phase = '';
  Confidence = 0;
  Recommendation = '';
  Reason = '';
  RiskLevel = '';
  HorizonDays = 0;
  PredictedAt = '';
  IsActionable = false;
  ModelStatus = '';
  ModelMessage = '';
  CurrentPrice = 0;
  TargetPrice: number | null = null;
  InvalidationPrice: number | null = null;

  constructor(init?: Partial<ClientAnalysisResult>) {
    Object.assign(this, init);
  }
}
