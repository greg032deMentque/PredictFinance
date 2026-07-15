import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import type { IInstrumentFundamentals } from '../Models/client-finance-models/instrument-fundamentals.model';
import type { IFundamentalScoreRequest, IFundamentalScoreResponse } from '../Models/client-finance-models/fundamental-score.model';

const FUNDAMENTAL_SCORE_UNIVERSE_ID = 'PEA_FR_EQUITIES';

@Injectable({ providedIn: 'root' })
export class FundamentalsService {
  private readonly http = inject(HttpClient);

  getFundamentals(symbol: string): Observable<IInstrumentFundamentals> {
    return this.http.get<IInstrumentFundamentals>(`${environment.apiUrl}ClientFinance/instruments/${encodeURIComponent(symbol)}/fundamentals`);
  }

  getScore(symbols: string[]): Observable<IFundamentalScoreResponse> {
    const body: IFundamentalScoreRequest = {
      UniverseId: FUNDAMENTAL_SCORE_UNIVERSE_ID,
      Symbols: symbols,
      IncludeRankPosition: true
    };
    return this.http.post<IFundamentalScoreResponse>(`${environment.apiUrl}ClientFinance/fundamentals/score`, body);
  }
}
