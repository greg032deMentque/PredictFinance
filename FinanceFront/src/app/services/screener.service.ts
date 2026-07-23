import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ScreenerMeta, ScreenerPage, ScreenerPreset, ScreenerPresetCreate, ScreenerQueryOptions } from '../Models/client-finance-models/screener.model';

@Injectable({ providedIn: 'root' })
export class ScreenerService {
  private readonly http = inject(HttpClient);

  getScreener(options: ScreenerQueryOptions = {}): Observable<ScreenerPage> {
    const params = this.buildFilterParams(options)
      .set('Page', options.Page ?? 1)
      .set('PageSize', options.PageSize ?? 20);

    return this.http.get<ScreenerPage>(`${environment.apiUrl}ClientFinance/screener`, { params });
  }

  getMeta(): Observable<ScreenerMeta> {
    return this.http.get<ScreenerMeta>(`${environment.apiUrl}ClientFinance/screener/meta`);
  }

  exportCsv(options: ScreenerQueryOptions = {}): Observable<Blob> {
    const params = this.buildFilterParams(options);
    return this.http.get(`${environment.apiUrl}ClientFinance/screener/export`, { params, responseType: 'blob' });
  }

  getPresets(): Observable<ScreenerPreset[]> {
    return this.http.get<ScreenerPreset[]>(`${environment.apiUrl}ClientFinance/screener/presets`);
  }

  savePreset(preset: ScreenerPresetCreate): Observable<ScreenerPreset> {
    return this.http.post<ScreenerPreset>(`${environment.apiUrl}ClientFinance/screener/presets`, preset);
  }

  deletePreset(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}ClientFinance/screener/presets/${id}`);
  }

  private buildFilterParams(options: ScreenerQueryOptions): HttpParams {
    let params = new HttpParams()
      .set('SortBy', options.SortBy ?? 'Symbol')
      .set('SortDirection', options.SortDirection ?? 'asc');

    // .append() (et non .set()) : Sectors/Countries sont des filtres multi-valeurs côté back (model binding
    // ASP.NET d'un tableau de query params sous la même clé) — .set() écraserait toutes les valeurs sauf la dernière.
    if (options.Sectors?.length) {
      for (const sector of options.Sectors) {
        params = params.append('Sectors', sector);
      }
    }

    if (options.Countries?.length) {
      for (const country of options.Countries) {
        params = params.append('Countries', country);
      }
    }

    // Filtre volontairement omis quand false/absent plutôt que forcé à 'false' : le back traite l'absence du
    // paramètre comme "pas de filtre PEA", ce qui garde les URLs courtes et évite une requête toujours "verbeuse".
    if (options.PeaOnly === true) {
      params = params.set('PeaOnly', 'true');
    }

    // Comparaison explicite à null/undefined (et non un simple truthy check) : AssetType/MinPE/MaxPE/... sont
    // des filtres numériques où 0 est une valeur légitime (ex. MinMarketCap: 0) et doit être envoyé au back,
    // contrairement à `if (options.MinPE)` qui l'aurait silencieusement filtré.
    if (options.AssetType !== null && options.AssetType !== undefined) {
      params = params.set('AssetType', options.AssetType);
    }

    if (options.Search?.trim()) {
      params = params.set('Search', options.Search.trim());
    }

    if (options.MinPE !== null && options.MinPE !== undefined) {
      params = params.set('MinPE', options.MinPE);
    }

    if (options.MaxPE !== null && options.MaxPE !== undefined) {
      params = params.set('MaxPE', options.MaxPE);
    }

    if (options.MinDividendYield !== null && options.MinDividendYield !== undefined) {
      params = params.set('MinDividendYield', options.MinDividendYield);
    }

    if (options.MinMarketCap !== null && options.MinMarketCap !== undefined) {
      params = params.set('MinMarketCap', options.MinMarketCap);
    }

    if (options.MinScore !== null && options.MinScore !== undefined) {
      params = params.set('MinScore', options.MinScore);
    }

    return params;
  }
}
