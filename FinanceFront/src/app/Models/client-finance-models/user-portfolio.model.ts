export type PortfolioType = 'CompteTitres' | 'Pea' | 'AssuranceVie' | 'Per' | 'Autre';

export const PORTFOLIO_TYPE_LABELS: Record<PortfolioType, string> = {
  CompteTitres: 'Compte-titres',
  Pea: 'PEA',
  AssuranceVie: 'Assurance vie',
  Per: 'PER',
  Autre: 'Autre'
};

export function getPortfolioTypeLabel(type: PortfolioType | string): string {
  return PORTFOLIO_TYPE_LABELS[type as PortfolioType] ?? type;
}

export type PortfolioStatus = 'Active' | 'Archived';

export class UserPortfolioViewModel {
  Id = '';
  Name = '';
  PortfolioType: PortfolioType = 'Autre';
  Status: PortfolioStatus = 'Active';

  constructor(init?: Partial<UserPortfolioViewModel>) {
    Object.assign(this, init);
  }
}

export class UserPortfolioCreateRequest {
  Name = '';
  PortfolioType: PortfolioType = 'Autre';

  constructor(init?: Partial<UserPortfolioCreateRequest>) {
    Object.assign(this, init);
  }
}

export class UserPortfolioRenameRequest {
  Name = '';

  constructor(init?: Partial<UserPortfolioRenameRequest>) {
    Object.assign(this, init);
  }
}
