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
      return 'Modéré';
    case 'High':
      return 'Élevé';
    case 'Information':
    default:
      return 'Information';
  }
}

export function getPhaseLabel(code: string): string {
  const normalized = code.trim().toLowerCase();

  switch (normalized) {
    // Communs
    case 'insufficient_history':       return 'Historique insuffisant';
    case 'invalidated':                return 'Scénario invalidé';
    case 'legacy_pattern_not_enabled': return 'Pattern désactivé';

    // Rectangle / Triangle
    case 'structure_not_confirmed':               return 'Structure non confirmée';
    case 'neutral_rectangle_without_prior_trend': return 'Rectangle sans tendance préalable';
    case 'compression_not_confirmed':             return 'Compression triangulaire insuffisante';
    case 'bilateral_triangle_without_prior_trend':return 'Triangle bilatéral sans tendance préalable';
    case 'opposite_breakout_invalidated':         return 'Breakout opposé à la continuation';

    // Bull Flag / Bear Flag
    case 'flag_structure_not_confirmed': return 'Structure de flag insuffisante';
    case 'flag_support_broken':          return 'Support du flag rompu';
    case 'flag_resistance_broken':       return 'Résistance du flag rompue';
    case 'bull_flag_forming':            return 'Bull flag en formation';
    case 'bear_flag_forming':            return 'Bear flag en formation';

    // Confirmés
    case 'bullish_breakout_confirmed': return 'Breakout haussier confirmé';
    case 'bearish_breakout_confirmed': return 'Breakout baissier confirmé';
    case 'bullish_triangle_compressing':  return 'Triangle de continuation haussier en compression';
    case 'bearish_triangle_compressing':  return 'Triangle de continuation baissier en compression';
    case 'bullish_rectangle_forming':     return 'Rectangle de continuation haussier en formation';
    case 'bearish_rectangle_forming':     return 'Rectangle de continuation baissier en formation';

    // Double Bottom
    case 'double_bottom_not_confirmed':      return 'Double creux non identifié';
    case 'double_bottom_forming':            return 'Double creux en formation';
    case 'double_bottom_breakout_confirmed': return 'Double creux — breakout haussier confirmé';

    // Double Top
    case 'double_top_not_confirmed':         return 'Double sommet non identifié';
    case 'double_top_forming':               return 'Double sommet en formation';
    case 'double_top_breakout_confirmed':    return 'Double sommet — cassure baissière confirmée';

    // Inverse Head and Shoulders
    case 'inverse_hs_not_confirmed':         return 'Tête-Épaules Inversé non identifié';
    case 'inverse_hs_forming':               return 'Tête-Épaules Inversé en formation';
    case 'inverse_hs_breakout_confirmed':    return 'Tête-Épaules Inversé — breakout haussier confirmé';

    // Head and Shoulders
    case 'hs_not_confirmed':                 return 'Tête-Épaules non identifié';
    case 'hs_forming':                       return 'Tête-Épaules en formation';
    case 'hs_breakdown_confirmed':           return 'Tête-Épaules — cassure baissière confirmée';

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
