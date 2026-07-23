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

/** Traductions pays (code ISO-2, cf. CountryCodeNormalizer.cs côté back) → FR. Fallback = valeur brute. */
const COUNTRY_MAP: Record<string, string> = {
  'FR': 'France',
  'US': 'États-Unis',
  'NL': 'Pays-Bas',
  'DE': 'Allemagne',
  'GB': 'Royaume-Uni',
  'CH': 'Suisse',
  'BE': 'Belgique',
  'ES': 'Espagne',
  'IT': 'Italie',
  'SE': 'Suède',
  'LU': 'Luxembourg',
  'IE': 'Irlande',
  'DK': 'Danemark',
  'FI': 'Finlande',
  'NO': 'Norvège',
  'PT': 'Portugal',
  'AT': 'Autriche',
  'JP': 'Japon',
  'CN': 'Chine',
  'CA': 'Canada'
};

export function translateSector(raw: string | null | undefined): string {
  if (!raw) return '';
  return SECTOR_MAP[raw] ?? raw;
}

export function translateCountry(raw: string | null | undefined): string {
  if (!raw) return '';
  return COUNTRY_MAP[raw] ?? raw;
}
