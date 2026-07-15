import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AnalysisConceptAdminItem,
  AnalysisConceptCreateRequest,
  AnalysisConceptUpdateRequest,
  PatternDefinitionAdminItem,
  PatternDefinitionUpdateRequest
} from '../Models/client-finance-models/analysis-content-admin.model';

@Injectable({ providedIn: 'root' })
export class AnalysisContentService {
  private readonly http = inject(HttpClient);

  getPatterns(): Observable<PatternDefinitionAdminItem[]> {
    return this.http.get<PatternDefinitionAdminItem[]>(`${environment.apiUrl}admin/pattern-definitions`);
  }

  getPatternById(id: string): Observable<PatternDefinitionAdminItem> {
    return this.http.get<PatternDefinitionAdminItem>(`${environment.apiUrl}admin/pattern-definitions/${id}`);
  }

  updatePattern(id: string, payload: PatternDefinitionUpdateRequest): Observable<PatternDefinitionAdminItem> {
    return this.http.put<PatternDefinitionAdminItem>(`${environment.apiUrl}admin/pattern-definitions/${id}`, payload);
  }

  getConcepts(): Observable<AnalysisConceptAdminItem[]> {
    return this.http.get<AnalysisConceptAdminItem[]>(`${environment.apiUrl}admin/analysis-concepts`);
  }

  getConceptByCode(code: string): Observable<AnalysisConceptAdminItem> {
    return this.http.get<AnalysisConceptAdminItem>(`${environment.apiUrl}admin/analysis-concepts/${code}`);
  }

  createConcept(payload: AnalysisConceptCreateRequest): Observable<AnalysisConceptAdminItem> {
    return this.http.post<AnalysisConceptAdminItem>(`${environment.apiUrl}admin/analysis-concepts`, payload);
  }

  updateConcept(code: string, payload: AnalysisConceptUpdateRequest): Observable<AnalysisConceptAdminItem> {
    return this.http.put<AnalysisConceptAdminItem>(`${environment.apiUrl}admin/analysis-concepts/${code}`, payload);
  }

  deleteConcept(code: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}admin/analysis-concepts/${code}`);
  }
}
