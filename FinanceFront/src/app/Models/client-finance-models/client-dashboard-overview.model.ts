export class ClientDashboardOverview {
  TotalPortfolioValue = 0;
  DayProfitLoss = 0;
  OpenPositions = 0;
  AnalysesThisWeek = 0;
  WatchlistCount = 0;
  RecommendationWinRate = 0;
  NextMarketOpenAt = '';
  TotalInvested = 0;
  TotalOutstanding = 0;

  constructor(init?: Partial<ClientDashboardOverview>) {
    Object.assign(this, init);
  }
}
