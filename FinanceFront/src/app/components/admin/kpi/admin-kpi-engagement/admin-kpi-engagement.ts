import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { finalize } from 'rxjs';
import { environment } from '../../../../../environments/environment';

interface RetentionCohort {
  Label: string;
  Rate: number;
  SampleSize: number;
}

interface ActivationFunnelStep {
  Step: number;
  Label: string;
  Count: number;
  Rate: number;
}

interface EngagementKpi {
  Window: string;
  Dau: number;
  Wau: number;
  Mau: number;
  Stickiness: number;
  ActiveUsers: number;
  RetentionCohorts: RetentionCohort[];
  ActivationFunnel: ActivationFunnelStep[];
  NotificationReadRate: number;
  OpsSuccessRate: number;
  OpsAvgDurationMs: number;
  StaleAssets: number;
}

@Component({
  selector: 'app-admin-kpi-engagement',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-kpi-engagement.html',
  styleUrl: './admin-kpi-engagement.scss',
})
export class AdminKpiEngagementComponent implements OnInit {
  private readonly http = inject(HttpClient);

  kpi: EngagementKpi | null = null;
  loading = false;
  exporting = false;
  error: string | null = null;
  selectedWindow: 'D7' | 'D30' | 'D90' = 'D30';

  ngOnInit(): void {
    this.loadKpi();
  }

  selectWindow(window: 'D7' | 'D30' | 'D90'): void {
    if (this.selectedWindow === window) return;
    this.selectedWindow = window;
    this.loadKpi();
  }

  loadKpi(): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<EngagementKpi>(`${environment.apiUrl}admin/kpi/engagement?window=${this.selectedWindow}`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.kpi = payload;
        },
        error: () => {
          this.kpi = null;
          this.error = 'Impossible de charger les KPI d\'engagement.';
        }
      });
  }

  exportCsv(): void {
    if (this.exporting) return;
    this.exporting = true;

    this.http
      .get(`${environment.apiUrl}admin/kpi/engagement/export?window=${this.selectedWindow}`, {
        responseType: 'blob'
      })
      .pipe(finalize(() => (this.exporting = false)))
      .subscribe({
        next: (blob) => this.downloadBlob(blob, `kpi-engagement-${this.selectedWindow}.csv`),
        error: () => (this.error = 'Export CSV impossible.')
      });
  }

  private downloadBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }

  // ratios 0-1 → pourcentage
  formatRate(rate: number): string {
    return (rate * 100).toFixed(1) + ' %';
  }

  // OpsSuccessRate est déjà multiplié par 100 côté backend
  formatOpsSuccessRate(rate: number): string {
    return rate.toFixed(1) + ' %';
  }

  formatDuration(ms: number): string {
    if (ms < 1000) return ms.toFixed(0) + ' ms';
    return (ms / 1000).toFixed(1) + ' s';
  }
}
