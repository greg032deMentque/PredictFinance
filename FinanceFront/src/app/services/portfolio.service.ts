import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserPortfolioCreateRequest, UserPortfolioRenameRequest, UserPortfolioViewModel } from '../Models/client-finance-models/user-portfolio.model';
import { ClientFinanceMapper } from './client-finance.mapper';

@Injectable({ providedIn: 'root' })
export class PortfolioService {
  private readonly http = inject(HttpClient);
  private readonly mapper = inject(ClientFinanceMapper);

  getPortfolios(): Observable<UserPortfolioViewModel[]> {
    return this.http
      .get<Record<string, unknown>[]>(`${environment.apiUrl}ClientFinance/portfolios`)
      .pipe(map((items) => items.map((item) => this.mapPortfolio(item))));
  }

  createPortfolio(request: UserPortfolioCreateRequest): Observable<UserPortfolioViewModel> {
    return this.http
      .post<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/portfolios`, {
        Name: request.Name,
        PortfolioType: request.PortfolioType
      })
      .pipe(map((payload) => this.mapPortfolio(payload)));
  }

  renamePortfolio(id: string, request: UserPortfolioRenameRequest): Observable<UserPortfolioViewModel> {
    return this.http
      .put<Record<string, unknown>>(`${environment.apiUrl}ClientFinance/portfolios/${encodeURIComponent(id)}`, {
        Name: request.Name
      })
      .pipe(map((payload) => this.mapPortfolio(payload)));
  }

  archivePortfolio(id: string): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}ClientFinance/portfolios/${encodeURIComponent(id)}/archive`, {});
  }

  restorePortfolio(id: string): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}ClientFinance/portfolios/${encodeURIComponent(id)}/restore`, {});
  }

  private mapPortfolio(source: Record<string, unknown>): UserPortfolioViewModel {
    return new UserPortfolioViewModel({
      Id: this.mapper.readString(source, ['id', 'Id']) ?? '',
      Name: this.mapper.readString(source, ['name', 'Name']) ?? '',
      PortfolioType: (this.mapper.readString(source, ['portfolioType', 'PortfolioType']) ?? 'Autre') as UserPortfolioViewModel['PortfolioType'],
      Status: (this.mapper.readString(source, ['status', 'Status']) ?? 'Active') as UserPortfolioViewModel['Status']
    });
  }
}
