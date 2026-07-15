import {
  AfterViewInit,
  Component,
  DestroyRef,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CategoryScale,
  Chart,
  Filler,
  LineController,
  LineElement,
  LinearScale,
  PointElement,
  ScatterController,
  Tooltip
} from 'chart.js';
import type { AnalysisPattern, PriceCandle } from '../../../../Models/client-finance-models/client-analysis-dossier.model';

Chart.register(
  LineController,
  ScatterController,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Filler,
  Tooltip
);

export interface PriceLevel {
  label: string;
  value: number;
  /** 'solid' | 'dashed' | 'dotted' */
  dash: number[];
  color: string;
}

@Component({
  selector: 'app-analysis-price-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './analysis-price-chart.component.html',
  styleUrl: './analysis-price-chart.component.scss'
})
export class AnalysisPriceChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('chartCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  @Input() priceSeries: PriceCandle[] = [];
  @Input() pattern: AnalysisPattern | null = null;
  @Input() chartTitle = '';

  private readonly destroyRef = inject(DestroyRef);
  private chart: Chart | null = null;
  private viewReady = false;

  get accessibleSummary(): string {
    if (!this.pattern || this.priceSeries.length === 0) {
      return 'Aucune donnée de prix disponible.';
    }
    const last = this.priceSeries[this.priceSeries.length - 1];
    const first = this.priceSeries[0];
    const parts: string[] = [
      `${this.priceSeries.length} bougies du ${first.Timestamp.substring(0, 10)} au ${last.Timestamp.substring(0, 10)}.`,
      `Dernier cours : ${last.Close.toFixed(2)}.`
    ];
    if (this.pattern.SuggestedTakeProfit !== null) {
      parts.push(`Cible : ${this.pattern.SuggestedTakeProfit.toFixed(2)}.`);
    }
    if (this.pattern.SuggestedStopLoss !== null) {
      parts.push(`Stop-loss : ${this.pattern.SuggestedStopLoss.toFixed(2)}.`);
    }
    if (this.pattern.InvalidationLevel !== null) {
      parts.push(`Niveau d'invalidation : ${this.pattern.InvalidationLevel.toFixed(2)}.`);
    }
    if (this.pattern.ValidationLevel !== null) {
      parts.push(`Niveau de validation : ${this.pattern.ValidationLevel.toFixed(2)}.`);
    }
    return parts.join(' ');
  }

  get structuralPointsSummary(): string {
    if (!this.pattern || this.pattern.StructuralPoints.length === 0) return '';
    return this.pattern.StructuralPoints
      .map((p) => `${p.PointType} à ${p.Price.toFixed(2)} (${p.Timestamp.substring(0, 10)})`)
      .join(', ');
  }

  private buildLevels(): PriceLevel[] {
    if (!this.pattern) return [];
    const levels: PriceLevel[] = [];

    if (this.pattern.SuggestedTakeProfit !== null) {
      levels.push({ label: 'Cible', value: this.pattern.SuggestedTakeProfit, dash: [], color: '#127a5a' });
    }
    if (this.pattern.ValidationLevel !== null) {
      levels.push({ label: 'Validation', value: this.pattern.ValidationLevel, dash: [8, 4], color: '#2f6ea7' });
    }
    if (this.pattern.InvalidationLevel !== null) {
      levels.push({ label: 'Invalidation', value: this.pattern.InvalidationLevel, dash: [4, 4], color: '#b13a3a' });
    }
    if (this.pattern.SuggestedStopLoss !== null) {
      levels.push({ label: 'Stop-loss', value: this.pattern.SuggestedStopLoss, dash: [2, 2], color: '#a8741a' });
    }
    return levels;
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.buildChart();
  }

  ngOnChanges(_changes: SimpleChanges): void {
    if (this.viewReady) {
      this.buildChart();
    }
  }

  ngOnDestroy(): void {
    this.destroyChart();
  }

  private destroyChart(): void {
    if (this.chart) {
      this.chart.destroy();
      this.chart = null;
    }
  }

  private buildChart(): void {
    if (!this.canvasRef || this.priceSeries.length === 0) return;

    this.destroyChart();

    const labels = this.priceSeries.map((c) => {
      const d = new Date(c.Timestamp);
      return `${d.getDate().toString().padStart(2, '0')}/${(d.getMonth() + 1).toString().padStart(2, '0')}`;
    });
    const closes = this.priceSeries.map((c) => c.Close);
    const levels = this.buildLevels();

    // Dataset principal : ligne de clôture
    const datasets: Chart['data']['datasets'] = [
      {
        label: 'Cours de clôture',
        data: closes,
        borderColor: '#0f2742',
        borderWidth: 2,
        pointRadius: 0,
        tension: 0.2,
        fill: false,
        type: 'line'
      }
    ];

    // Niveaux horizontaux (un dataset ligne par niveau)
    for (const level of levels) {
      datasets.push({
        label: level.label,
        data: new Array(closes.length).fill(level.value) as number[],
        borderColor: level.color,
        borderWidth: 1.5,
        borderDash: level.dash,
        pointRadius: 0,
        tension: 0,
        fill: false,
        type: 'line'
      });
    }

    // Points structurels — scatter positionné sur les indices correspondants
    if (this.pattern && this.pattern.StructuralPoints.length > 0) {
      const scatterData = this.pattern.StructuralPoints.map((sp) => {
        const idx = this.priceSeries.findIndex((c) => c.Timestamp === sp.Timestamp);
        return { x: idx >= 0 ? idx : closes.length - 1, y: sp.Price };
      });

      datasets.push({
        label: 'Points structurels',
        data: scatterData as { x: number; y: number }[],
        type: 'scatter',
        backgroundColor: '#a8741a',
        borderColor: '#0f2742',
        borderWidth: 1,
        pointRadius: 6,
        pointStyle: 'triangle'
      } as never);
    }

    this.chart = new Chart(this.canvasRef.nativeElement, {
      type: 'line',
      data: { labels, datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const val = ctx.parsed.y;
                return `${ctx.dataset.label}: ${typeof val === 'number' ? val.toFixed(2) : val}`;
              }
            }
          },
          legend: { display: false }
        },
        scales: {
          x: {
            ticks: { maxTicksLimit: 10, font: { size: 11 } },
            grid: { color: 'rgba(0,0,0,0.05)' }
          },
          y: {
            ticks: { font: { size: 11 } },
            grid: { color: 'rgba(0,0,0,0.05)' }
          }
        }
      }
    });
  }
}
