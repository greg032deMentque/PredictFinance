import { CommonModule, DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { finalize } from 'rxjs';
import { environment } from '../../../../../environments/environment';

interface ConfidenceCalibrationRow {
  Label: string;
  TotalSignals: number;
  TargetHits: number;
  HitRate: number;
}

interface PatternPerformanceRow {
  PatternId: string;
  TotalEvaluated: number;
  TargetHitRate: number;
  AvgConfidence: number;
}

interface ModelPerformanceRow {
  ModelVersion: string;
  TotalEvaluated: number;
  TargetHitRate: number;
}

interface SignalQualityKpi {
  Window: string;
  OverallTargetHitRate: number;
  TotalEvaluated: number;
  OpenSignals: number;
  NotEvaluable: number;
  ConfidenceCalibration: ConfidenceCalibrationRow[];
  PatternPerformance: PatternPerformanceRow[];
  ModelPerformance: ModelPerformanceRow[];
}

@Component({
  selector: 'app-admin-kpi-signal-quality',
  standalone: true,
  imports: [CommonModule, DecimalPipe],
  templateUrl: './admin-kpi-signal-quality.html',
  styleUrl: './admin-kpi-signal-quality.scss',
})
export class AdminKpiSignalQualityComponent implements OnInit {
  private readonly http = inject(HttpClient);

  kpi: SignalQualityKpi | null = null;
  loading = false;
  exporting = false;
  error: string | null = null;
  selectedWindow: 'D30' | 'D90' = 'D30';

  ngOnInit(): void {
    this.loadKpi();
  }

  selectWindow(window: 'D30' | 'D90'): void {
    if (this.selectedWindow === window) return;
    this.selectedWindow = window;
    this.loadKpi();
  }

  loadKpi(): void {
    this.loading = true;
    this.error = null;

    this.http
      .get<SignalQualityKpi>(`${environment.apiUrl}admin/kpi/signal-quality?window=${this.selectedWindow}`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (payload) => {
          this.kpi = payload;
        },
        error: () => {
          this.kpi = null;
          this.error = 'Impossible de charger les KPI qualité des signaux.';
        }
      });
  }

  exportCsv(): void {
    if (this.exporting) return;
    this.exporting = true;

    this.http
      .get(`${environment.apiUrl}admin/kpi/signal-quality/export?window=${this.selectedWindow}`, {
        responseType: 'blob'
      })
      .pipe(finalize(() => (this.exporting = false)))
      .subscribe({
        next: (blob) => this.downloadBlob(blob, `kpi-signal-quality-${this.selectedWindow}.csv`),
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

  formatRate(rate: number): string {
    return (rate * 100).toFixed(1) + ' %';
  }
}
