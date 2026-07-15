import { CommonModule, CurrencyPipe, DatePipe, PercentPipe } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges, signal, computed } from '@angular/core';
import type { AnalysisDossier, AnalysisPattern } from '../../../../Models/client-finance-models/client-analysis-dossier.model';
import { AnalysisPriceChartComponent } from '../analysis-price-chart/analysis-price-chart.component';
import { PatternListComponent } from '../pattern-list/pattern-list.component';
import { PatternExplanationPanelComponent } from '../pattern-explanation-panel/pattern-explanation-panel.component';

@Component({
  selector: 'app-finance-analysis-result',
  standalone: true,
  imports: [
    CommonModule,
    CurrencyPipe,
    PercentPipe,
    DatePipe,
    AnalysisPriceChartComponent,
    PatternListComponent,
    PatternExplanationPanelComponent
  ],
  templateUrl: './finance-analysis-result.component.html',
  styleUrl: './finance-analysis-result.component.scss'
})
export class FinanceAnalysisResultComponent implements OnChanges {
  @Input() loading = false;
  @Input() result: AnalysisDossier | null = null;

  readonly selectedPattern = signal<AnalysisPattern | null>(null);
  readonly alternativesExpanded = signal(false);

  readonly allPatterns = computed<AnalysisPattern[]>(() => {
    if (!this.result) return [];
    const main = this.result.MainPattern ? [this.result.MainPattern] : [];
    return [...main, ...this.result.AlternativePatterns];
  });

  readonly chartPattern = computed<AnalysisPattern | null>(() => {
    return this.selectedPattern() ?? this.result?.MainPattern ?? null;
  });

  readonly chartPriceSeries = computed(() => {
    return this.result?.PriceSeries ?? [];
  });

  readonly hasFullDossier = computed(() => {
    const outcome = this.result?.Outcome;
    return outcome === 'CrediblePatternFound' || outcome === 'MultipleCompatiblePatterns';
  });

  readonly showGraphOnly = computed(() => {
    const outcome = this.result?.Outcome;
    return outcome === 'NoCrediblePattern' || outcome === 'InsufficientData';
  });

  readonly isUnsupported = computed(() => {
    const outcome = this.result?.Outcome;
    return outcome === 'UnsupportedInstrument' || outcome === 'UnsupportedContext';
  });

  readonly alternativeCount = computed(() => this.result?.AlternativePatterns.length ?? 0);

  /** True si un warning earnings est actif sur une recommandation actionnable (BUY/SELL/HOLD). */
  readonly hasEarningsRisk = computed(() => {
    const isEarningsWarning = this.result?.RiskContext?.EarningsWithinHorizonWarning === true;
    const recommendationAction = this.result?.MainPattern?.RecommendationAction;
    return isEarningsWarning && !!recommendationAction && recommendationAction !== 'NonActionable';
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['result']) {
      // Sélectionner le pattern principal par défaut à chaque nouveau résultat
      this.selectedPattern.set(this.result?.MainPattern ?? null);
      this.alternativesExpanded.set(false);
    }
  }

  onPatternSelected(pattern: AnalysisPattern): void {
    this.selectedPattern.set(pattern);
  }

  toggleAlternatives(): void {
    this.alternativesExpanded.update((v) => !v);
  }

  getOutcomeIcon(): string {
    switch (this.result?.Outcome) {
      case 'CrediblePatternFound': return 'bi-check-circle-fill text-success';
      case 'MultipleCompatiblePatterns': return 'bi-layers-fill text-primary';
      case 'NoCrediblePattern': return 'bi-dash-circle text-warning';
      case 'InsufficientData': return 'bi-database-x text-warning';
      case 'UnsupportedInstrument': return 'bi-slash-circle text-secondary';
      case 'UnsupportedContext': return 'bi-slash-circle text-secondary';
      default: return 'bi-circle text-muted';
    }
  }

  getOutcomeLabel(): string {
    switch (this.result?.Outcome) {
      case 'CrediblePatternFound': return 'Figure crédible identifiée';
      case 'MultipleCompatiblePatterns': return 'Plusieurs figures compatibles';
      case 'NoCrediblePattern': return 'Aucune figure crédible';
      case 'InsufficientData': return 'Données insuffisantes';
      case 'UnsupportedInstrument': return 'Instrument non supporté';
      case 'UnsupportedContext': return 'Contexte non supporté';
      default: return 'Résultat inconnu';
    }
  }

  getRecommendationBadgeClass(action: string): string {
    switch (action) {
      case 'Buy': return 'chip chip-success';
      case 'Sell': return 'chip chip-danger';
      case 'Hold': return 'chip chip-navy';
      case 'NonActionable': return 'chip chip-dark';
      default: return 'chip chip-navy';
    }
  }

  getRecommendationLabel(action: string): string {
    switch (action) {
      case 'Buy': return 'Acheter';
      case 'Sell': return 'Vendre';
      case 'Hold': return 'Attendre';
      case 'NonActionable': return 'Inactif';
      default: return 'Attendre';
    }
  }

  getRecommendationIcon(action: string): string {
    switch (action) {
      case 'Buy': return 'bi-arrow-up-circle-fill';
      case 'Sell': return 'bi-arrow-down-circle-fill';
      case 'Hold': return 'bi-pause-circle-fill';
      case 'NonActionable': return 'bi-dash-circle-fill';
      default: return 'bi-circle';
    }
  }
}
