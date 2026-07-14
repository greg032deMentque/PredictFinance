import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { environment } from '../../../../../environments/environment';

interface SnapshotAuditItem {
  AnalysisRunId: string;
  Symbol: string;
  Status: string;
  StartedAtUtc: string;
  CompletedAtUtc?: string | null;
  TraceId: string;
  PrimaryPatternId?: string | null;
  ExecutedPatternIds: string[];
  RecommendationAction?: string | null;
  ModelStatus?: string | number | null;
  AnalysisEngineVersion?: string | null;
}

@Component({
  selector: 'app-admin-snapshot-audit',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink],
  templateUrl: './admin-snapshot-audit.component.html',
  styleUrl: './admin-snapshot-audit.component.scss'
})
export class AdminSnapshotAuditComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly adminPaths = AdminPaths;
  readonly toCommands = toCommands;
  items: SnapshotAuditItem[] = [];
  loading = false;
  error: string | null = null;
  readonly selectedAnalysisRunIds = new Set<string>();

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.error = null;
    this.selectedAnalysisRunIds.clear();

    this.http
      .get<SnapshotAuditItem[]>(`${environment.apiUrl}admin/snapshot-audit?take=20`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.items = payload ?? [];
        },
        error: () => {
          this.items = [];
          this.error = 'Impossible de charger l’audit des snapshots.';
        }
      });
  }

  isSelected(analysisRunId: string): boolean {
    return this.selectedAnalysisRunIds.has(analysisRunId);
  }

  toggleSelection(analysisRunId: string): void {
    if (this.selectedAnalysisRunIds.has(analysisRunId)) {
      this.selectedAnalysisRunIds.delete(analysisRunId);
      return;
    }

    if (this.selectedAnalysisRunIds.size >= 2) {
      const firstSelection = this.selectedAnalysisRunIds.values().next().value;
      if (firstSelection) {
        this.selectedAnalysisRunIds.delete(firstSelection);
      }
    }

    this.selectedAnalysisRunIds.add(analysisRunId);
  }

  goToCompare(): void {
    if (this.selectedAnalysisRunIds.size !== 2) {
      return;
    }

    const [leftAnalysisRunId, rightAnalysisRunId] = Array.from(this.selectedAnalysisRunIds);
    void this.router.navigate(toCommands(this.adminPaths.SnapshotAuditCompare), {
      queryParams: {
        left: leftAnalysisRunId,
        right: rightAnalysisRunId
      }
    });
  }
}
