import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { environment } from '../../../../../environments/environment';

interface WordingPublicationState {
  IsActive: boolean;
  ActivatedAtUtc?: string | null;
  RecommendationPolicyVersion: string;
  ExplanationPolicyVersion: string;
  AffectedDomains: string[];
}

interface WordingScenarioTemplateSummary {
  ScenarioCode: string;
  RecommendationKind: string | number;
  HoldingStatus: string | number;
  ActionVerbFamilyCode: string;
  SupportedStrengths: (string | number)[];
  TemplateSummary: string;
}

interface AdminWordingVersionVersionDetail {
  WordingVersionId: string;
  DisplayName: string;
  PublicationState: WordingPublicationState;
  Scenarios: WordingScenarioTemplateSummary[];
}

@Component({
  selector: 'app-admin-wording-version-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './admin-wording-version-detail.component.html',
  styleUrl: './admin-wording-version-detail.component.scss'
})
export class AdminWordingVersionDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);

  readonly adminPaths = AdminPaths;
  readonly toCommands = toCommands;
  detail: AdminWordingVersionVersionDetail | null = null;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    const wordingVersionId = (this.route.snapshot.paramMap.get('wordingVersionId') ?? '').trim();
    if (!wordingVersionId) {
      this.error = 'Identifiant de version manquant.';
      return;
    }

    this.load(wordingVersionId);
  }

  private load(wordingVersionId: string): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<AdminWordingVersionVersionDetail>(`${environment.apiUrl}admin/wording-versions/${encodeURIComponent(wordingVersionId)}`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.detail = payload;
        },
        error: () => {
          this.detail = null;
          this.error = 'Impossible de charger le détail de la version de wording.';
        }
      });
  }
}
