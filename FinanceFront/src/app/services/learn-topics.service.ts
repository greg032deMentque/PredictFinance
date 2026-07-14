import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { LearnTopicAdminItem, LearnTopicUpsertRequest } from '../Models/client-finance-models/learn-topic-admin.model';

@Injectable({ providedIn: 'root' })
export class LearnTopicsService {
  private readonly http = inject(HttpClient);

  getAdminList(): Observable<LearnTopicAdminItem[]> {
    return this.http.get<LearnTopicAdminItem[]>(`${environment.apiUrl}admin/learn-topics`);
  }

  getAdminById(id: number): Observable<LearnTopicAdminItem> {
    return this.http.get<LearnTopicAdminItem>(`${environment.apiUrl}admin/learn-topics/${id}`);
  }

  createAdmin(payload: LearnTopicUpsertRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}admin/learn-topics`, payload);
  }

  updateAdmin(id: number, payload: LearnTopicUpsertRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}admin/learn-topics/${id}`, payload);
  }

  deleteAdmin(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}admin/learn-topics/${id}`);
  }
}
