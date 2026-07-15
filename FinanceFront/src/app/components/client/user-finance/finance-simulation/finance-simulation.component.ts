import {
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  inject,
  signal
} from '@angular/core';
import { CommonModule, CurrencyPipe, PercentPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  ClientMultiSimulationRequest,
  getPhaseLabel,
  getRecommendationBadgeClass,
  getRecommendationLabel,
  getRiskLevelLabel
} from '../../../../Models/client-finance-models/client-finance-models';
import type { MultiSimulationDossier, SimulationDossier } from '../../../../Models/client-finance-models/client-simulation-dossier.model';
import type { PatternCatalogItem } from '../../../../Models/client-finance-models/pattern-catalog.model';
import type { AnalysisConcept } from '../../../../Models/client-finance-models/client-finance-models';
import { PatternCatalogStore } from '../../../../services/pattern-catalog.store';
import { ClientFinanceService } from '../../../../services/client-finance.service';

@Component({
  selector: 'app-finance-simulation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CurrencyPipe, PercentPipe],
  templateUrl: './finance-simulation.component.html',
  styleUrl: './finance-simulation.component.scss'
})
export class FinanceSimulationComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly patternCatalogStore = inject(PatternCatalogStore);
  private readonly clientFinanceService = inject(ClientFinanceService);

  readonly availablePatterns = this.patternCatalogStore.items;
  readonly concepts = signal<AnalysisConcept[]>([]);

  @Input() selectedSymbol = '';
  @Input() loading = false;
  @Input() result: MultiSimulationDossier | null = null;

  @Output() launch = new EventEmitter<ClientMultiSimulationRequest>();

  /** Ids des patterns cochés — géré manuellement (les checkboxes ne bindent pas directement un string[]). */
  selectedPatternIds: string[] = [];

  readonly activePatternInfo = signal<PatternCatalogItem | null>(null);

  /** Message d'avertissement affiché lorsqu'une simulation est lancée sans valeur ou sans pattern. */
  readonly submitWarning = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    investmentAmount: this.fb.nonNullable.control(1000, [Validators.required, Validators.min(1)]),
    horizonDays: this.fb.nonNullable.control(20, [Validators.required, Validators.min(1), Validators.max(365)])
  });

  ngOnInit(): void {
    this.patternCatalogStore
      .ensureLoaded()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((patterns) => {
        // Pré-cocher le premier pattern si aucune sélection
        if (this.selectedPatternIds.length === 0 && patterns.length > 0) {
          this.selectedPatternIds = [patterns[0].Id];
        }
      });

    this.clientFinanceService
      .getAnalysisConcepts()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (concepts) => this.concepts.set(concepts), error: () => { /* glossaire optionnel */ } });
  }

  /** Libellé FR d'un pattern (depuis le catalogue), sinon l'identifiant brut. */
  patternLabel(patternId: string): string {
    return this.availablePatterns().find((pattern) => pattern.Id === patternId)?.Label ?? patternId;
  }

  /** Métadonnée catalogue d'un pattern par identifiant. */
  catalogFor(patternId: string): PatternCatalogItem | undefined {
    return this.availablePatterns().find((pattern) => pattern.Id === patternId);
  }

  /** Explication pédagogique d'un concept d'analyse (direction, fiabilité…). */
  conceptByCode(code: string): AnalysisConcept | undefined {
    return this.concepts().find((concept) => concept.Code === code.toLowerCase());
  }

  // ─── Sélection des patterns ──────────────────────────────────────────────────

  isPatternSelected(id: string): boolean {
    return this.selectedPatternIds.includes(id);
  }

  openPatternInfo(pattern: PatternCatalogItem): void {
    this.activePatternInfo.set(pattern);
  }

  togglePattern(id: string): void {
    if (this.isPatternSelected(id)) {
      this.selectedPatternIds = this.selectedPatternIds.filter((p) => p !== id);
    } else {
      this.selectedPatternIds = [...this.selectedPatternIds, id];
    }
    if (this.selectedPatternIds.length > 0) {
      this.submitWarning.set(null);
    }
  }

  // ─── Helpers affichage par résultat de pattern ───────────────────────────────

  getRecommendationLabel(r: SimulationDossier): string {
    return getRecommendationLabel(r.RecommendationAction as Parameters<typeof getRecommendationLabel>[0]);
  }

  getRecommendationClass(r: SimulationDossier): string {
    return getRecommendationBadgeClass(r.RecommendationAction as Parameters<typeof getRecommendationBadgeClass>[0]);
  }

  getRiskLevelLabel(r: SimulationDossier): string {
    return getRiskLevelLabel(r.RiskLevel as Parameters<typeof getRiskLevelLabel>[0]);
  }

  getPhaseLabel(r: SimulationDossier): string {
    return getPhaseLabel(r.Phase);
  }

  getActionableSummary(r: SimulationDossier): string {
    if (r.IsActionable) {
      return `Le conseil produit retient actuellement une posture ${this.getRecommendationLabel(r).toLowerCase()}.`;
    }
    return "Le scénario reste informatif pour l'instant. Aucune action immédiate n'est retenue.";
  }

  getPerformanceToneClass(r: SimulationDossier): string {
    if (r.EstimatedReturnAmount > 0) return 'text-success';
    if (r.EstimatedReturnAmount < 0) return 'text-danger';
    return 'text-body';
  }

  getScenarioClass(label: string): string {
    switch (label) {
      case 'Cible': return 'scenario-card--target';
      case 'Invalidation': return 'scenario-card--invalidation';
      default: return 'scenario-card--neutral';
    }
  }

  getScenarioIcon(label: string): string {
    switch (label) {
      case 'Cible': return 'bi-arrow-up-circle-fill';
      case 'Invalidation': return 'bi-arrow-down-circle-fill';
      default: return 'bi-dash-circle';
    }
  }

  formatAriaAmount(amount: number): string {
    return new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'USD' }).format(amount);
  }

  // ─── Formulaire ─────────────────────────────────────────────────────────────

  submit(): void {
    if (this.selectedSymbol.trim().length === 0) {
      this.submitWarning.set('Sélectionne d’abord une valeur avant de lancer la simulation.');
      return;
    }
    if (this.selectedPatternIds.length === 0) {
      this.submitWarning.set('Sélectionne au moins un pattern à simuler.');
      return;
    }
    if (this.form.invalid || this.loading) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitWarning.set(null);
    const payload = this.form.getRawValue();
    this.launch.emit(
      new ClientMultiSimulationRequest({
        Symbol: this.selectedSymbol,
        Patterns: [...this.selectedPatternIds],
        InvestmentAmount: payload.investmentAmount,
        HorizonDays: payload.horizonDays
      })
    );
  }
}
