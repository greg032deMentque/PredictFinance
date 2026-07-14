import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { LegalCardAdminItem, LegalCardItem, LegalCardUpsertRequest } from '../Models/client-finance-models/legal-card.model';

@Injectable({ providedIn: 'root' })
export class LegalService {
  private readonly http = inject(HttpClient);

  getList(): Observable<LegalCardItem[]> {
    return this.http.get<LegalCardItem[]>(`${environment.apiUrl}clientfinance/legal-cards`);
  }

  getAdminList(): Observable<LegalCardAdminItem[]> {
    return this.http.get<LegalCardAdminItem[]>(`${environment.apiUrl}admin/legal-cards`);
  }

  getAdminById(id: number): Observable<LegalCardAdminItem> {
    return this.http.get<LegalCardAdminItem>(`${environment.apiUrl}admin/legal-cards/${id}`);
  }

  createAdmin(payload: LegalCardUpsertRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}admin/legal-cards`, payload);
  }

  updateAdmin(id: number, payload: LegalCardUpsertRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}admin/legal-cards/${id}`, payload);
  }

  deleteAdmin(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}admin/legal-cards/${id}`);
  }
}
