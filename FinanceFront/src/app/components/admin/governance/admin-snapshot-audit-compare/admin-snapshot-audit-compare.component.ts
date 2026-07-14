import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { environment } from '../../../../../environments/environment';

interface SnapshotAuditComparison {
  LeftAnalysisRunId: string;
  RightAnalysisRunId: string;
  SameUser: boolean;
  SameAsset: boolean;
  SamePrimaryPattern: boolean;
  SameRecommendationAction: boolean;
  LeftPrimaryPatternId?: string | null;
  RightPrimaryPatternId?: string | null;
  LeftRecommendationAction?: string | null;
  RightRecommendationAction?: string | null;
  LeftAnalysisEngineVersion?: string | null;
  RightAnalysisEngineVersion?: string | null;
  LeftModelStatus?: string | number | null;
  RightModelStatus?: string | number | null;
}

@Component({
  selector: 'app-admin-snapshot-audit-compare',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-snapshot-audit-compare.component.html',
  styleUrl: './admin-snapshot-audit-compare.component.scss'
})
export class AdminSnapshotAuditCompareComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);

  readonly adminPaths = AdminPaths;
  readonly toCommands = toCommands;
  comparison: SnapshotAuditComparison | null = null;
  loading = false;
  error: string | null = null;
  leftAnalysisRunId = '';
  rightAnalysisRunId = '';

  ngOnInit(): void {
    this.leftAnalysisRunId = (this.route.snapshot.queryParamMap.get('left') ?? '').trim();
    this.rightAnalysisRunId = (this.route.snapshot.queryParamMap.get('right') ?? '').trim();

    if (!this.leftAnalysisRunId || !this.rightAnalysisRunId) {
      this.error = 'Deux identifiants de run sont requis pour la comparaison.';
      return;
    }

    this.load();
  }

  private load(): void {
    this.loading = true;
    this.error = null;

    const query = `leftId=${encodeURIComponent(this.leftAnalysisRunId)}&rightId=${encodeURIComponent(this.rightAnalysisRunId)}`;

    this.http
      .get<SnapshotAuditComparison>(`${environment.apiUrl}admin/snapshot-audit/compare?${query}`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.comparison = payload;
        },
        error: () => {
          this.comparison = null;
          this.error = 'Impossible de comparer les deux snapshots.';
        }
      });
  }
}
