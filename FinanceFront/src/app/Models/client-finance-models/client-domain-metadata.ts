export type ClientRecommendationActionCode = 'Buy' | 'Sell' | 'Hold' | 'NonActionable' | '';
export type ClientRiskLevelCode = 'Information' | 'Low' | 'Moderate' | 'High' | '';
export type ClientModelStatusCode = 'Go' | 'NoGo' | '';
export type ClientPatternCode = string;

export function getRecommendationLabel(code: ClientRecommendationActionCode): string {
  switch (code) {
    case 'Buy':
      return 'Acheter';
    case 'Sell':
      return 'Vendre';
    case 'Hold':
      return 'Attendre';
    case 'NonActionable':
      return 'Inactif';
    default:
      return 'Attendre';
  }
}

export function getRecommendationBadgeClass(code: ClientRecommendationActionCode): string {
  switch (code) {
    case 'Buy':
      return 'text-bg-success';
    case 'Sell':
      return 'text-bg-danger';
    case 'Hold':
      return 'text-bg-secondary';
    case 'NonActionable':
      return 'text-bg-dark';
    default:
      return 'text-bg-secondary';
  }
}

export function getRiskLevelLabel(code: ClientRiskLevelCode): string {
  switch (code) {
    case 'Low':
      return 'Faible';
    case 'Moderate':
      return 'Modere';
    case 'High':
      return 'Eleve';
    case 'Information':
    default:
      return 'Information';
  }
}

export function getPhaseLabel(code: string): string {
  const normalized = code.trim().toLowerCase();

  switch (normalized) {
    case 'neckline_break_confirmed':
      return 'Cassure de neckline confirmee';
    case 'pullback_after_break':
      return 'Pullback apres cassure';
    case 'second_peak_candidate':
      return 'Deuxieme sommet potentiel';
    case 'invalidated':
      return 'Scenario invalide';
    default:
      return code.trim() || 'Phase indisponible';
  }
}

export function getModelStatusLabel(code: ClientModelStatusCode): string {
  switch (code) {
    case 'Go':
      return 'Go';
    case 'NoGo':
      return 'No-Go';
    default:
      return 'Inconnu';
  }
}
