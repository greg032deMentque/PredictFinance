export interface TaxSummary {
  Year: number;
  PortfolioId: string;
  PortfolioName: string;
  PortfolioTypeLabel: string;
  TaxRatePct: number;
  PeaAncienneteYears: number | null;
  TotalRealizedPnl: number;
  EstimatedTax: number;
  Positions: RealizedPosition[];
}

export interface RealizedPosition {
  Symbol: string;
  DisplayName: string;
  RealizedPnl: number;
  Sales: RealizedSale[];
}

export interface RealizedSale {
  SaleDate: string;
  Quantity: number;
  SellPrice: number;
  AvgCostAtSale: number;
  RealizedPnl: number;
}
