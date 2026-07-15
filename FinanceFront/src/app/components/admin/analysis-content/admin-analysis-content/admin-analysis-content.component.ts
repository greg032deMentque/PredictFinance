import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  AnalysisConceptAdminItem,
  AnalysisConceptCreateRequest,
  AnalysisConceptUpdateRequest,
  PatternDefinitionAdminItem,
  PatternDefinitionUpdateRequest
} from '../../../../Models/client-finance-models/analysis-content-admin.model';
import { AnalysisContentService } from '../../../../services/analysis-content.service';
import { ToastService } from '../../../../services/toastr.service';

type ConceptFormMode = 'none' | 'add' | 'edit';

@Component({
  selector: 'app-admin-analysis-content',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-analysis-content.component.html',
  styleUrl: './admin-analysis-content.component.scss'
})
export class AdminAnalysisContentComponent implements OnInit {
  private readonly service = inject(AnalysisContentService);
  private readonly toastService = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly activeTab = signal<'patterns' | 'concepts'>('patterns');

  readonly patterns = signal<PatternDefinitionAdminItem[]>([]);
  readonly loadingPatterns = signal(false);
  readonly errorPatterns = signal<string | null>(null);
  readonly patternFormOpen = signal(false);
  readonly submittingPattern = signal(false);
  editPatternId = '';

  readonly concepts = signal<AnalysisConceptAdminItem[]>([]);
  readonly loadingConcepts = signal(false);
  readonly errorConcepts = signal<string | null>(null);
  readonly conceptFormMode = signal<ConceptFormMode>('none');
  readonly submittingConcept = signal(false);
  readonly deletingCode = signal<string | null>(null);
  editConceptCode = '';

  readonly patternForm = this.fb.nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(200)]],
    family: ['', [Validators.required, Validators.maxLength(100)]],
    familyLabel: ['', [Validators.required, Validators.maxLength(200)]],
    direction: ['', [Validators.required, Validators.maxLength(100)]],
    directionLabel: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    analysisNarrative: ['', [Validators.required, Validators.maxLength(2000)]],
    reliability: [0, [Validators.required, Validators.min(0), Validators.max(1)]],
    reliabilityLabel: ['', [Validators.required, Validators.maxLength(100)]]
  });

  readonly conceptForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(100)]],
    label: ['', [Validators.required, Validators.maxLength(200)]],
    explanation: ['', [Validators.required, Validators.maxLength(4000)]]
  });

  get isConceptEditing(): boolean { return this.conceptFormMode() === 'edit'; }
  get isConceptPanelOpen(): boolean { return this.conceptFormMode() !== 'none'; }

  ngOnInit(): void {
    this.loadPatterns();
    this.loadConcepts();
  }

  setTab(tab: 'patterns' | 'concepts'): void {
    this.activeTab.set(tab);
    this.patternFormOpen.set(false);
    this.conceptFormMode.set('none');
  }

  loadPatterns(): void {
    this.loadingPatterns.set(true);
    this.errorPatterns.set(null);
    this.service.getPatterns()
      .pipe(finalize(() => this.loadingPatterns.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => this.patterns.set(data ?? []),
        error: () => {
          this.patterns.set([]);
          this.errorPatterns.set('Impossible de charger les patterns.');
        }
      });
  }

  openEditPattern(item: PatternDefinitionAdminItem): void {
    this.editPatternId = item.PatternId;
    this.patternForm.patchValue({
      displayName: item.DisplayName,
      family: item.Family,
      familyLabel: item.FamilyLabel,
      direction: item.Direction,
      directionLabel: item.DirectionLabel,
      description: item.Description,
      analysisNarrative: item.AnalysisNarrative,
      reliability: item.Reliability,
      reliabilityLabel: item.ReliabilityLabel
    });
    this.patternFormOpen.set(true);
  }

  closePatternForm(): void {
    this.patternFormOpen.set(false);
    this.patternForm.reset();
    this.editPatternId = '';
  }

  submitPattern(): void {
    if (this.patternForm.invalid || this.submittingPattern()) {
      this.patternForm.markAllAsTouched();
      return;
    }
    const v = this.patternForm.getRawValue();
    const payload: PatternDefinitionUpdateRequest = {
      displayName: v.displayName.trim(),
      family: v.family.trim(),
      familyLabel: v.familyLabel.trim(),
      direction: v.direction.trim(),
      directionLabel: v.directionLabel.trim(),
      description: v.description.trim(),
      analysisNarrative: v.analysisNarrative.trim(),
      reliability: v.reliability,
      reliabilityLabel: v.reliabilityLabel.trim()
    };
    this.submittingPattern.set(true);
    this.service.updatePattern(this.editPatternId, payload)
      .pipe(finalize(() => this.submittingPattern.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.success('Pattern mis à jour.');
          this.closePatternForm();
          this.loadPatterns();
        },
        error: () => this.toastService.error('Échec de la mise à jour.')
      });
  }

  loadConcepts(): void {
    this.loadingConcepts.set(true);
    this.errorConcepts.set(null);
    this.service.getConcepts()
      .pipe(finalize(() => this.loadingConcepts.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => this.concepts.set(data ?? []),
        error: () => {
          this.concepts.set([]);
          this.errorConcepts.set('Impossible de charger les concepts.');
        }
      });
  }

  openAddConcept(): void {
    this.conceptForm.reset({ code: '', label: '', explanation: '' });
    this.conceptForm.controls.code.enable();
    this.editConceptCode = '';
    this.conceptFormMode.set('add');
  }

  openEditConcept(item: AnalysisConceptAdminItem): void {
    this.editConceptCode = item.Code;
    this.conceptForm.patchValue({ code: item.Code, label: item.Label, explanation: item.Explanation });
    this.conceptForm.controls.code.disable();
    this.conceptFormMode.set('edit');
  }

  closeConceptForm(): void {
    this.conceptFormMode.set('none');
    this.conceptForm.reset();
    this.conceptForm.controls.code.enable();
    this.editConceptCode = '';
  }

  submitConcept(): void {
    if (this.conceptForm.invalid || this.submittingConcept()) {
      this.conceptForm.markAllAsTouched();
      return;
    }
    const v = this.conceptForm.getRawValue();
    this.submittingConcept.set(true);

    const request$ = this.isConceptEditing
      ? this.service.updateConcept(this.editConceptCode, { label: v.label.trim(), explanation: v.explanation.trim() } as AnalysisConceptUpdateRequest)
      : this.service.createConcept({ code: v.code.trim().toUpperCase(), label: v.label.trim(), explanation: v.explanation.trim() } as AnalysisConceptCreateRequest);

    request$
      .pipe(finalize(() => this.submittingConcept.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.success(this.isConceptEditing ? 'Concept mis à jour.' : 'Concept ajouté.');
          this.closeConceptForm();
          this.loadConcepts();
        },
        error: () => this.toastService.error(this.isConceptEditing ? 'Échec de la mise à jour.' : 'Échec de la création.')
      });
  }

  deleteConcept(item: AnalysisConceptAdminItem): void {
    if (this.deletingCode() !== null) return;
    if (!confirm(`Supprimer le concept « ${item.Code} » ?`)) return;

    this.deletingCode.set(item.Code);
    this.service.deleteConcept(item.Code)
      .pipe(finalize(() => this.deletingCode.set(null)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toastService.success('Concept supprimé.');
          this.loadConcepts();
        },
        error: () => this.toastService.error('Suppression impossible.')
      });
  }
}
