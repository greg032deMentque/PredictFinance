import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { FaqAdminItem, FaqItem, FaqUpsertRequest } from '../Models/client-finance-models/faq.model';

@Injectable({ providedIn: 'root' })
export class FaqService {
  private readonly http = inject(HttpClient);

  getList(): Observable<FaqItem[]> {
    return this.http.get<FaqItem[]>(`${environment.apiUrl}clientfinance/faq`);
  }

  getAdminList(): Observable<FaqAdminItem[]> {
    return this.http.get<FaqAdminItem[]>(`${environment.apiUrl}admin/faq`);
  }

  getAdminById(id: number): Observable<FaqAdminItem> {
    return this.http.get<FaqAdminItem>(`${environment.apiUrl}admin/faq/${id}`);
  }

  createAdmin(payload: FaqUpsertRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}admin/faq`, payload);
  }

  updateAdmin(id: number, payload: FaqUpsertRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}admin/faq/${id}`, payload);
  }

  deleteAdmin(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}admin/faq/${id}`);
  }
}
