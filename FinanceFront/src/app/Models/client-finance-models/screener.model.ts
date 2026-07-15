export interface ScreenerItem {
  Symbol: string;
  Name: string;
  Exchange: string;
  Country: string | null;
  Sector: string | null;
  AssetType: number;
  IsPeaEligible: boolean;
  LastPrice: number | null;
  DayVariationPct: number | null;
  QuoteAsOfUtc: string | null;
  TrailingPE: number | null;
  DividendYield: number | null;
  MarketCap: number | null;
}

export interface ScreenerPage {
  Items: ScreenerItem[];
  Total: number;
  Page: number;
  PageSize: number;
}

export interface ScreenerMeta {
  Sectors: string[];
  Countries: string[];
}

export interface ScreenerQueryOptions {
  Page?: number;
  PageSize?: number;
  SortBy?: string;
  SortDirection?: 'asc' | 'desc';
  Sectors?: string[];
  Countries?: string[];
  PeaOnly?: boolean;
  AssetType?: number | null;
  Search?: string;
  MinPE?: number | null;
  MaxPE?: number | null;
  MinDividendYield?: number | null;
  MinMarketCap?: number | null;
}

export const AssetTypeOptions = [
  { value: 0, label: 'Action' },
  { value: 1, label: 'ETF' },
  { value: 2, label: 'Crypto' }
] as const;

export interface ScreenerPreset {
  Id: string;
  Name: string;
  Query: ScreenerQueryOptions;
}

export interface ScreenerPresetCreate {
  Name: string;
  Query: ScreenerQueryOptions;
}
