import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import type { TechnicalIndicators } from '../Models/client-finance-models/technical-indicators.model';

@Injectable({ providedIn: 'root' })
export class TechnicalIndicatorsService {
  private readonly http = inject(HttpClient);

  getIndicators(symbol: string): Observable<TechnicalIndicators> {
    return this.http.get<TechnicalIndicators>(
      `${environment.apiUrl}ClientFinance/indicators/${encodeURIComponent(symbol)}`
    );
  }
}
