import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import type { PortfolioRiskMetrics } from '../Models/client-finance-models/portfolio-risk-metrics.model';

@Injectable({ providedIn: 'root' })
export class PortfolioRiskMetricsService {
  private readonly http = inject(HttpClient);

  getMetrics(portfolioId: string): Observable<PortfolioRiskMetrics> {
    return this.http.get<PortfolioRiskMetrics>(
      `${environment.apiUrl}ClientFinance/portfolio/${encodeURIComponent(portfolioId)}/risk-metrics`
    );
  }
}
