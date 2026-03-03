export class MarketAssetOption {
  symbol = '';
  companyName = '';
  market = '';
  currency = 'USD';
  lastPrice = 0;
  dayVariationPct = 0;

  constructor(init?: Partial<MarketAssetOption>) {
    Object.assign(this, init);
  }
}

export class ClientDashboardOverview {
  totalPortfolioValue = 0;
  dayProfitLoss = 0;
  openPositions = 0;
  analysesThisWeek = 0;
  watchlistCount = 0;
  recommendationWinRate = 0;
  nextMarketOpenAt = '';
  totalInvested = 0;
  totalOutstanding = 0;

  constructor(init?: Partial<ClientDashboardOverview>) {
    Object.assign(this, init);
  }
}

export class ClientWatchlistItem {
  userAssetId = '';
  symbol = '';
  companyName = '';
  market = '';
  lastPrice = 0;
  dayVariationPct = 0;
  heldQuantity = 0;
  averageBuyPrice = 0;
  investedAmount = 0;
  outstandingAmount = 0;

  constructor(init?: Partial<ClientWatchlistItem>) {
    Object.assign(this, init);
  }
}

export class ClientLiveQuote {
  symbol = '';
  lastPrice = 0;
  dayVariationPct = 0;
  asOfUtc = '';

  constructor(init?: Partial<ClientLiveQuote>) {
    Object.assign(this, init);
  }
}

export class ClientTransactionCreateRequest {
  symbol = '';
  transactionType: 'Buy' | 'Sell' = 'Buy';
  quantity = 0;
  unitPrice = 0;
  fees = 0;
  timestampUtc = '';

  constructor(init?: Partial<ClientTransactionCreateRequest>) {
    Object.assign(this, init);
  }
}

export class ClientTransactionItem {
  id = '';
  symbol = '';
  companyName = '';
  transactionType = '';
  quantity = 0;
  unitPrice = 0;
  fees = 0;
  grossAmount = 0;
  netAmount = 0;
  timestampUtc = '';

  constructor(init?: Partial<ClientTransactionItem>) {
    Object.assign(this, init);
  }
}

export class ClientAnalysisLaunchRequest {
  symbol = '';

  constructor(init?: Partial<ClientAnalysisLaunchRequest>) {
    Object.assign(this, init);
  }
}

export class ClientAnalysisResult {
  id = '';
  symbol = '';
  companyName = '';
  pattern = '';
  confidence = 0;
  recommendation = '';
  reason = '';
  riskLevel = '';
  horizonDays = 0;
  predictedAt = '';

  constructor(init?: Partial<ClientAnalysisResult>) {
    Object.assign(this, init);
  }
}

export class ClientSimulationRequest {
  symbol = '';
  investmentAmount = 0;
  horizonDays = 30;

  constructor(init?: Partial<ClientSimulationRequest>) {
    Object.assign(this, init);
  }
}

export class ClientSimulationResult {
  symbol = '';
  investmentAmount = 0;
  horizonDays = 0;
  estimatedReturnAmount = 0;
  estimatedReturnPct = 0;
  estimatedFinalAmount = 0;
  recommendation = '';
  assumption = '';

  constructor(init?: Partial<ClientSimulationResult>) {
    Object.assign(this, init);
  }
}
