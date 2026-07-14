import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { GlossaryTerm } from '../Models/client-finance-models/glossary-term.model';

@Injectable({ providedIn: 'root' })
export class GlossaryService {

  private readonly http = inject(HttpClient);

  private readonly glossary$: Observable<GlossaryTerm[]> = this.http
    .get<GlossaryTerm[]>(`${environment.apiUrl}ClientFinance/glossary`)
    .pipe(
      tap((terms) => this.buildLookupMap(terms)),
      shareReplay(1)
    );

  private lookupMap: Map<string, GlossaryTerm> | null = null;

  getGlossary(): Observable<GlossaryTerm[]> {
    return this.glossary$;
  }

  lookup(key: string): GlossaryTerm | undefined {
    if (!this.lookupMap) {
      return undefined;
    }
    const normalizedKey = this.normalize(key);
    return this.lookupMap.get(normalizedKey);
  }

  private buildLookupMap(terms: GlossaryTerm[]): void {
    this.lookupMap = new Map<string, GlossaryTerm>();
    for (const term of terms) {
      this.lookupMap.set(this.normalize(term.parameterId), term);
      this.lookupMap.set(this.normalize(term.label), term);
    }
  }

  private normalize(value: string): string {
    return value
      .trim()
      .toLowerCase()
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '');
  }
}
