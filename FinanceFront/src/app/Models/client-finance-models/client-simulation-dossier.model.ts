/** Contrat exact :
 *  POST /api/ClientFinance/simulation/run       → SimulationResultViewModel enrichi
 *  POST /api/ClientFinance/simulation/run-multi → MultiSimulationResultViewModel
 */

import type { PriceCandle, StructuralPoint } from './client-analysis-dossier.model';

export type SimulationScenarioLabel = 'Cible' | 'Neutre' | 'Invalidation';

export interface SimulationScenario {
  Label: SimulationScenarioLabel;
  TargetPrice: number | null;
  EstimatedReturnPct: number;
  EstimatedReturnAmount: number;
  EstimatedFinalAmount: number;
  Probability: number | null;
}

export interface SimulationDossier {
  Symbol: string;
  Pattern: string;
  Phase: string;
  InvestmentAmount: number;
  HorizonDays: number;
  EstimatedReturnAmount: number;
  EstimatedReturnPct: number;
  EstimatedFinalAmount: number;
  Assumption: string;
  CurrentPrice: number;
  Probability: number;
  RecommendationAction: string;
  RecommendationReason: string;
  RiskLevel: string;
  IsActionable: boolean;
  TargetPrice: number | null;
  InvalidationPrice: number | null;
  Scenarios: SimulationScenario[];
  PriceSeries: PriceCandle[];
  StructuralPoints: StructuralPoint[];
}

/** Contrat POST /api/ClientFinance/simulation/run-multi */
export interface MultiSimulationDossier {
  Symbol: string;
  InvestmentAmount: number;
  HorizonDays: number;
  CurrentPrice: number;
  Currency: string;
  GlobalMessage: string;
  PatternResults: SimulationDossier[];
}
