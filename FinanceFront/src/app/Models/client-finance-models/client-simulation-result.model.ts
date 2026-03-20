import type {
  ClientPatternCode,
  ClientRecommendationActionCode,
  ClientRiskLevelCode
} from './client-domain-metadata';

export class ClientSimulationResult {
  Symbol = '';
  Pattern: ClientPatternCode = '';
  Phase = '';
  InvestmentAmount = 0;
  HorizonDays = 0;
  EstimatedReturnAmount = 0;
  EstimatedReturnPct = 0;
  EstimatedFinalAmount = 0;
  Assumption = '';
  CurrentPrice = 0;
  Probability = 0;
  RecommendationAction: ClientRecommendationActionCode = '';
  RecommendationReason = '';
  RiskLevel: ClientRiskLevelCode = '';
  IsActionable = false;
  TargetPrice: number | null = null;
  InvalidationPrice: number | null = null;

  constructor(init?: Partial<ClientSimulationResult>) {
    Object.assign(this, init);
  }
}
