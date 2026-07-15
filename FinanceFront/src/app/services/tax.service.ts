import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import type { TaxSummary } from '../Models/client-finance-models/tax-summary.model';

@Injectable({ providedIn: 'root' })
export class TaxService {
  private readonly http = inject(HttpClient);

  getSummary(year: number): Observable<TaxSummary[]> {
    return this.http.get<TaxSummary[]>(
      `${environment.apiUrl}ClientFinance/tax-summary?year=${year}`
    );
  }
}
