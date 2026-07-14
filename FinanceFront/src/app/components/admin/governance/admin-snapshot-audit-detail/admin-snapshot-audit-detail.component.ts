import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { environment } from '../../../../../environments/environment';

interface SnapshotAuditDetail {
  AnalysisRunId: string;
  UserId: string;
  AssetId: string;
  Symbol: string;
  Status: string;
  StartedAtUtc: string;
  CompletedAtUtc?: string | null;
  ErrorMessage?: string | null;
  RawPayload: string;
  TraceId: string;
  RequestedPatternIds: string[];
  ExecutedPatternIds: string[];
  PrimaryPatternId?: string | null;
  RecommendationAction?: string | null;
  RecommendationPolicyVersion?: string | null;
  ExplanationPolicyVersion?: string | null;
  AnalysisEngineVersion?: string | null;
  ModelStatus?: string | number | null;
  ModelMessage?: string | null;
  DecisionAction?: string | null;
  DecisionSummary?: string | null;
}

@Component({
  selector: 'app-admin-snapshot-audit-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './admin-snapshot-audit-detail.component.html',
  styleUrl: './admin-snapshot-audit-detail.component.scss'
})
export class AdminSnapshotAuditDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);

  readonly adminPaths = AdminPaths;
  readonly toCommands = toCommands;
  detail: SnapshotAuditDetail | null = null;
  loading = false;
  error: string | null = null;

  ngOnInit(): void {
    const analysisRunId = (this.route.snapshot.paramMap.get('analysisRunId') ?? '').trim();
    if (!analysisRunId) {
      this.error = 'Identifiant de run manquant.';
      return;
    }

    this.load(analysisRunId);
  }

  private load(analysisRunId: string): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<SnapshotAuditDetail>(`${environment.apiUrl}admin/snapshot-audit/${encodeURIComponent(analysisRunId)}`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.detail = payload;
        },
        error: () => {
          this.detail = null;
          this.error = 'Impossible de charger le détail du snapshot.';
        }
      });
  }
}
