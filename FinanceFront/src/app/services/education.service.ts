import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { EducationArticleAdmin, EducationArticleContent, EducationArticleSummary, EducationUpsertRequest } from '../Models/client-finance-models/education-article.model';

@Injectable({ providedIn: 'root' })
export class EducationService {
  private readonly http = inject(HttpClient);

  getList(): Observable<EducationArticleSummary[]> {
    return this.http.get<EducationArticleSummary[]>(`${environment.apiUrl}clientfinance/education`);
  }

  getBySlug(slug: string): Observable<EducationArticleContent> {
    return this.http.get<EducationArticleContent>(`${environment.apiUrl}clientfinance/education/${encodeURIComponent(slug)}`);
  }

  getAdminList(): Observable<EducationArticleAdmin[]> {
    return this.http.get<EducationArticleAdmin[]>(`${environment.apiUrl}admin/education`);
  }

  getAdminById(id: string): Observable<EducationArticleAdmin> {
    return this.http.get<EducationArticleAdmin>(`${environment.apiUrl}admin/education/${id}`);
  }

  createAdmin(payload: EducationUpsertRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}admin/education`, payload);
  }

  updateAdmin(id: string, payload: EducationUpsertRequest): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}admin/education/${id}`, payload);
  }

  deleteAdmin(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}admin/education/${id}`);
  }
}
