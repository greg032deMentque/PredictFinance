import { Component, ElementRef, Input, OnChanges, OnDestroy, AfterViewInit, ViewChild } from '@angular/core';
import { Chart, ArcElement, DoughnutController, Tooltip, Legend } from 'chart.js';
import { ClientPortfolioPosition } from '../../../../Models/client-finance-models/read-models/client-portfolio.model';

Chart.register(ArcElement, DoughnutController, Tooltip, Legend);

@Component({
  selector: 'app-portfolio-donut-chart',
  standalone: true,
  template: '<canvas #chartCanvas style="max-height:240px"></canvas>'
})
export class PortfolioDonutChartComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() positions: ClientPortfolioPosition[] = [];
  @ViewChild('chartCanvas') chartCanvas!: ElementRef<HTMLCanvasElement>;

  private chart: Chart<'doughnut'> | null = null;
  private viewReady = false;

  private readonly COLORS = [
    '#0d6efd', '#6f42c1', '#d63384', '#dc3545', '#fd7e14',
    '#ffc107', '#198754', '#20c997', '#0dcaf0', '#6610f2'
  ];

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.buildChart();
  }

  ngOnChanges(): void {
    if (this.viewReady) this.buildChart();
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private buildChart(): void {
    this.chart?.destroy();
    const canvas = this.chartCanvas?.nativeElement;
    if (!canvas || this.positions.length === 0) return;

    this.chart = new Chart(canvas, {
      type: 'doughnut',
      data: {
        labels: this.positions.map(p => p.Instrument.Symbol),
        datasets: [{
          data: this.positions.map(p => p.OutstandingAmount),
          backgroundColor: this.positions.map((_, i) => this.COLORS[i % this.COLORS.length]),
          borderWidth: 2,
          borderColor: '#fff'
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { position: 'bottom', labels: { boxWidth: 12, padding: 8, font: { size: 11 } } },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const data = ctx.dataset.data as number[];
                const total = data.reduce((a, b) => a + b, 0);
                const val = ctx.parsed;
                const pct = total > 0 ? ((val / total) * 100).toFixed(1) : '0.0';
                return ` ${val.toFixed(2)} € (${pct}%)`;
              }
            }
          }
        }
      }
    });
  }
}
