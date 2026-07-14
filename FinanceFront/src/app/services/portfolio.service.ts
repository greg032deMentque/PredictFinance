import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserPortfolioCreateRequest, UserPortfolioRenameRequest, UserPortfolioViewModel } from '../Models/client-finance-models/user-portfolio.model';

@Injectable({ providedIn: 'root' })
export class PortfolioService {
  private readonly http = inject(HttpClient);

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

  deletePortfolio(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}ClientFinance/portfolios/${encodeURIComponent(id)}`);
  }

  private mapPortfolio(source: Record<string, unknown>): UserPortfolioViewModel {
    return new UserPortfolioViewModel({
      Id: this.readString(source, ['id', 'Id']) ?? '',
      Name: this.readString(source, ['name', 'Name']) ?? '',
      PortfolioType: (this.readString(source, ['portfolioType', 'PortfolioType']) ?? 'Autre') as UserPortfolioViewModel['PortfolioType']
    });
  }

  private readString(source: Record<string, unknown>, keys: string[]): string | null {
    for (const key of keys) {
      const value = source[key];
      if (typeof value === 'string' && value.trim().length > 0) {
        return value.trim();
      }
    }
    return null;
  }
}
