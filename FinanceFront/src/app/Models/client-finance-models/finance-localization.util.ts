/** Traductions secteurs GICS Yahoo Finance → FR. Fallback = valeur brute. */
const SECTOR_MAP: Record<string, string> = {
  'Technology': 'Technologie',
  'Consumer Cyclical': 'Consommation cyclique',
  'Consumer Defensive': 'Consommation de base',
  'Industrials': 'Industrie',
  'Healthcare': 'Santé',
  'Financial Services': 'Services financiers',
  'Energy': 'Énergie',
  'Utilities': 'Services aux collectivités',
  'Real Estate': 'Immobilier',
  'Basic Materials': 'Matériaux de base',
  'Communication Services': 'Services de communication'
};

/** Traductions pays courants → FR. Fallback = valeur brute. */
const COUNTRY_MAP: Record<string, string> = {
  'United States': 'États-Unis',
  'France': 'France',
  'Germany': 'Allemagne',
  'United Kingdom': 'Royaume-Uni',
  'Netherlands': 'Pays-Bas',
  'Switzerland': 'Suisse',
  'Spain': 'Espagne',
  'Italy': 'Italie',
  'Belgium': 'Belgique',
  'Canada': 'Canada',
  'Japan': 'Japon',
  'China': 'Chine',
  'Ireland': 'Irlande',
  'Luxembourg': 'Luxembourg'
};

export function translateSector(raw: string | null | undefined): string {
  if (!raw) return '';
  return SECTOR_MAP[raw] ?? raw;
}

export function translateCountry(raw: string | null | undefined): string {
  if (!raw) return '';
  return COUNTRY_MAP[raw] ?? raw;
}
