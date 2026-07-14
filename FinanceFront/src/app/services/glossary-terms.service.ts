import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { GlossaryTerm, GlossaryTermAdmin, GlossaryTermUpsertRequest } from '../Models/client-finance-models/glossary-product-term.model';

@Injectable({ providedIn: 'root' })
export class GlossaryTermsService {
  private readonly http = inject(HttpClient);

  search(query: string): Observable<GlossaryTerm[]> {
    const params = new HttpParams().set('search', query);
    return this.http.get<GlossaryTerm[]>(`${environment.apiUrl}clientfinance/glossary-terms`, { params });
  }

  searchAdmin(query: string): Observable<GlossaryTermAdmin[]> {
    const params = new HttpParams().set('search', query);
    return this.http.get<GlossaryTermAdmin[]>(`${environment.apiUrl}admin/glossary-terms`, { params });
  }

  getAdminById(id: string): Observable<GlossaryTermAdmin> {
    return this.http.get<GlossaryTermAdmin>(`${environment.apiUrl}admin/glossary-terms/${id}`);
  }

  createAdmin(payload: GlossaryTermUpsertRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}admin/glossary-terms`, payload);
  }

  updateAdmin(id: string, payload: GlossaryTermUpsertRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}admin/glossary-terms/${id}`, payload);
  }

  deleteAdmin(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}admin/glossary-terms/${id}`);
  }
}
