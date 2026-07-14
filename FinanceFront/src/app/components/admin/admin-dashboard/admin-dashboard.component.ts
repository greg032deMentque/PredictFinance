import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, toCommands } from '../../../Routes/app.routes.constants';
import { environment } from '../../../../environments/environment';

interface AdminOverview {
  TotalUsers: number;
  ActiveUsers: number;
  TotalAssets: number;
  TotalAnalysisRuns: number;
  CompletedAnalysisRuns: number;
  FailedAnalysisRuns: number;
  ConfirmedEligiblePeaEntries: number;
  UnknownPeaEntries: number;
  PublishedParameterEntries: number;
  LatestCompletedAnalysisUtc?: string | null;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  readonly adminPaths = AdminPaths;
  readonly toCommands = toCommands;

  overview: AdminOverview | null = null;
  loading = false;
  error: string | null = null;
  private readonly http = inject(HttpClient);

  ngOnInit(): void {
    this.loadOverview();
  }

  loadOverview(): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<AdminOverview>(`${environment.apiUrl}admin/overview`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.overview = payload;
        },
        error: () => {
          this.overview = null;
          this.error = 'Impossible de charger la vue d’ensemble admin.';
        }
      });
  }
}
