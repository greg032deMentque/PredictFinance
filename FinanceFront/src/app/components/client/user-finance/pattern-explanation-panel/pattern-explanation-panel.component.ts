import { Component, Input } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import type { AnalysisPattern, AnalysisWindow } from '../../../../Models/client-finance-models/client-analysis-dossier.model';

@Component({
  selector: 'app-pattern-explanation-panel',
  standalone: true,
  imports: [CommonModule, CurrencyPipe],
  templateUrl: './pattern-explanation-panel.component.html',
  styleUrl: './pattern-explanation-panel.component.scss'
})
export class PatternExplanationPanelComponent {
  @Input() pattern: AnalysisPattern | null = null;
  @Input() analysisWindow: AnalysisWindow | null = null;

  get dataQualityPct(): number {
    if (!this.analysisWindow || this.analysisWindow.RequiredCandles === 0) return 0;
    return Math.min(100, Math.round((this.analysisWindow.ActualCandles / this.analysisWindow.RequiredCandles) * 100));
  }

  get dataQualityLabel(): string {
    const pct = this.dataQualityPct;
    if (pct >= 90) return 'Excellente';
    if (pct >= 70) return 'Bonne';
    if (pct >= 50) return 'Partielle';
    return 'Insuffisante';
  }

  get dataQualityBarClass(): string {
    const pct = this.dataQualityPct;
    if (pct >= 90) return 'quality-bar__fill--good';
    if (pct >= 70) return 'quality-bar__fill--partial';
    return 'quality-bar__fill--low';
  }

  get riskRewardLabel(): string {
    const rr = this.pattern?.RiskRewardRatio;
    if (rr === null || rr === undefined) return '—';
    return `1 : ${rr.toFixed(2)}`;
  }
}
