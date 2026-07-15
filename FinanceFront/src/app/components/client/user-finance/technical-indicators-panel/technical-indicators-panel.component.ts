import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { Component, Input, computed, signal } from '@angular/core';
import type { TechnicalIndicators } from '../../../../Models/client-finance-models/technical-indicators.model';

@Component({
  selector: 'app-technical-indicators-panel',
  standalone: true,
  imports: [CommonModule, DecimalPipe, DatePipe],
  templateUrl: './technical-indicators-panel.component.html',
  styleUrl: './technical-indicators-panel.component.scss'
})
export class TechnicalIndicatorsPanelComponent {
  @Input() indicators: TechnicalIndicators | null = null;
  @Input() loading = false;

  readonly expandedKey = signal<string | null>(null);

  readonly synthesisBadgeClass = computed(() => {
    switch (this.indicators?.Synthesis?.Label) {
      case 'Fortement haussier': return 'badge bg-success text-white';
      case 'Haussier': return 'badge bg-success-subtle text-success-emphasis';
      case 'Légèrement haussier': return 'badge bg-success-subtle text-success-emphasis';
      case 'Mixte': return 'badge bg-secondary-subtle text-secondary-emphasis';
      case 'Légèrement baissier': return 'badge bg-danger-subtle text-danger-emphasis';
      case 'Baissier': return 'badge bg-danger-subtle text-danger-emphasis';
      case 'Fortement baissier': return 'badge bg-danger text-white';
      default: return 'badge bg-secondary-subtle text-secondary-emphasis';
    }
  });

  readonly rsiSignalClass = computed(() => {
    switch (this.indicators?.Rsi?.Signal) {
      case 'Surachat': return 'badge bg-danger-subtle text-danger-emphasis';
      case 'Survente': return 'badge bg-success-subtle text-success-emphasis';
      default: return 'badge bg-secondary-subtle text-secondary-emphasis';
    }
  });

  readonly macdTrendClass = computed(() =>
    this.indicators?.Macd?.Trend === 'Haussier'
      ? 'badge bg-success-subtle text-success-emphasis'
      : 'badge bg-danger-subtle text-danger-emphasis'
  );

  readonly bbPositionClass = computed(() => {
    switch (this.indicators?.BollingerBands?.Position) {
      case 'Au-dessus de la bande haute': return 'badge bg-danger-subtle text-danger-emphasis';
      case 'En dessous de la bande basse': return 'badge bg-success-subtle text-success-emphasis';
      default: return 'badge bg-secondary-subtle text-secondary-emphasis';
    }
  });

  maCompareClass(maValue: number | null | undefined): string {
    const current = this.indicators?.MovingAverages?.CurrentPrice;
    if (!maValue || !current) return 'text-muted';
    return current > maValue ? 'text-success' : 'text-danger';
  }

  maCompareLabel(maValue: number | null | undefined): string {
    const current = this.indicators?.MovingAverages?.CurrentPrice;
    if (!maValue || !current) return '—';
    return current > maValue ? '↑ Au-dessus' : '↓ En dessous';
  }

  toggleExplain(key: string): void {
    this.expandedKey.update(current => current === key ? null : key);
  }
}
