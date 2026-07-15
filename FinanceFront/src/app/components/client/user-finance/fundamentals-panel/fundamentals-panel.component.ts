import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import type { IInstrumentFundamentals } from '../../../../Models/client-finance-models/instrument-fundamentals.model';
import type { IFundamentalScoreResult } from '../../../../Models/client-finance-models/fundamental-score.model';

interface ScoreCategoryDisplay {
  label: string;
  value: number | null;
}

const RECOMMENDATION_LABELS_FR: Readonly<Record<string, string>> = {
  strong_buy: 'Achat fort',
  buy: 'Achat',
  hold: 'Conserver',
  underperform: 'Sous-performance',
  sell: 'Vente',
  strong_sell: 'Vente forte'
};

@Component({
  selector: 'app-fundamentals-panel',
  standalone: true,
  imports: [CommonModule, DecimalPipe, DatePipe],
  templateUrl: './fundamentals-panel.component.html',
  styleUrl: './fundamentals-panel.component.scss'
})
export class FundamentalsPanelComponent {
  @Input() fundamentals: IInstrumentFundamentals | null = null;
  @Input() loading = false;
  @Input() score: IFundamentalScoreResult | null = null;
  @Input() scoreLoading = false;

  get scoreCategories(): ScoreCategoryDisplay[] {
    const s = this.score;
    if (!s) return [];
    return [
      { label: 'Rentabilité', value: s.ProfitabilityScore },
      { label: 'Liquidité', value: s.LiquidityScore },
      { label: 'Dette', value: s.DebtScore },
      { label: 'Valorisation', value: s.ValuationScore },
      { label: 'Dividende', value: s.DividendScore },
      { label: 'Croissance', value: s.GrowthScore }
    ];
  }

  get scorePercentileContext(): string {
    const s = this.score;
    if (!s) return '';
    if (!s.UsedGlobalUniverseFallback) {
      return `vs secteur ${s.PercentileGroupLabel}`;
    }
    if (s.PercentileGroupLabel) {
      return 'vs univers global (échantillon sectoriel insuffisant)';
    }
    return 'vs univers global (secteur inconnu)';
  }

  scoreBadgeClass(value: number | null): string {
    if (value === null) return 'bg-secondary';
    if (value >= 70) return 'bg-success';
    if (value >= 40) return 'bg-warning text-dark';
    return 'bg-danger';
  }

  get recommendationLabel(): string | null {
    const key = this.fundamentals?.RecommendationKey;
    if (!key) return null;
    return RECOMMENDATION_LABELS_FR[key.toLowerCase()] ?? null;
  }

  get hasAnalystConsensus(): boolean {
    const f = this.fundamentals;
    if (!f) return false;
    return this.recommendationLabel !== null || f.RecommendationMean !== null || f.TargetMeanPrice !== null;
  }
}
