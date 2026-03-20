import type {
  ClientModelStatusCode,
  ClientPatternCode,
  ClientRecommendationActionCode,
  ClientRiskLevelCode
} from './client-domain-metadata';

export class ClientAnalysisResult {
  Id = '';
  Symbol = '';
  CompanyName = '';
  Pattern: ClientPatternCode = '';
  Phase = '';
  Probability = 0;
  RecommendationAction: ClientRecommendationActionCode = '';
  RecommendationReason = '';
  RiskLevel: ClientRiskLevelCode = '';
  RecommendationHorizonDays = 0;
  PredictedAt = '';
  IsActionable = false;
  ModelStatus: ClientModelStatusCode = '';
  ModelMessage = '';
  CurrentPrice = 0;
  NecklinePrice: number | null = null;
  TargetPrice: number | null = null;
  InvalidationPrice: number | null = null;

  constructor(init?: Partial<ClientAnalysisResult>) {
    Object.assign(this, init);
  }
}
