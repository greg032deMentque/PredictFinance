import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { GlossaryTermAdmin, GlossaryTermUpsertRequest } from '../../../../Models/client-finance-models/glossary-product-term.model';
import { GlossaryTermsService } from '../../../../services/glossary-terms.service';
import { ToastService } from '../../../../services/toastr.service';

type FormMode = 'none' | 'add' | 'edit';

@Component({
  selector: 'app-admin-glossary',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-glossary.component.html',
  styleUrl: './admin-glossary.component.scss'
})
export class AdminGlossaryComponent implements OnInit {
  private readonly glossaryTermsService = inject(GlossaryTermsService);
  private readonly toastService = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly terms = signal<GlossaryTermAdmin[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly formMode = signal<FormMode>('none');

  private editId = '';

  readonly searchControl = this.fb.nonNullable.control('');

  readonly termForm = this.fb.nonNullable.group({
    term: ['', [Validators.required, Validators.maxLength(300)]],
    definition: ['', [Validators.required, Validators.maxLength(2000)]],
    category: ['', [Validators.required, Validators.maxLength(200)]],
    isPublished: [false]
  });

  get isAdding(): boolean { return this.formMode() === 'add'; }
  get isEditing(): boolean { return this.formMode() === 'edit'; }
  get isPanelOpen(): boolean { return this.formMode() !== 'none'; }

  ngOnInit(): void {
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((query) => this.load(query));

    this.load('');
  }

  load(query = this.searchControl.value): void {
    this.loading.set(true);
    this.error.set(null);
    this.glossaryTermsService
      .searchAdmin(query)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (payload) => this.terms.set(payload ?? []),
        error: () => {
          this.terms.set([]);
          this.error.set('Impossible de charger le glossaire.');
        }
      });
  }

  openAdd(): void {
    this.termForm.reset({ term: '', definition: '', category: '', isPublished: false });
    this.formMode.set('add');
    this.editId = '';
  }

  openEdit(term: GlossaryTermAdmin): void {
    this.editId = term.Id;
    this.termForm.patchValue({
      term: term.Term,
      definition: term.Definition,
      category: term.Category,
      isPublished: term.IsPublished
    });
    this.formMode.set('edit');
  }

  closePanel(): void {
    this.formMode.set('none');
    this.termForm.reset();
    this.editId = '';
  }

  submit(): void {
    if (this.termForm.invalid || this.submitting()) {
      this.termForm.markAllAsTouched();
      return;
    }

    const v = this.termForm.getRawValue();
    const payload: GlossaryTermUpsertRequest = {
      Term: v.term.trim(),
      Definition: v.definition.trim(),
      Category: v.category.trim(),
      IsPublished: v.isPublished
    };

    this.submitting.set(true);
    const request$ = this.isEditing
      ? this.glossaryTermsService.updateAdmin(this.editId, payload)
      : this.glossaryTermsService.createAdmin(payload);

    request$
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toastService.success(this.isEditing ? 'Terme mis à jour.' : 'Terme ajouté.');
          this.closePanel();
          this.load();
        },
        error: () => {
          this.toastService.error(this.isEditing ? 'Échec de la mise à jour.' : 'Échec de la création.');
        }
      });
  }

  delete(term: GlossaryTermAdmin): void {
    if (this.deletingId() !== null) return;
    if (!confirm(`Supprimer « ${term.Term} » ?`)) return;

    this.deletingId.set(term.Id);
    this.glossaryTermsService
      .deleteAdmin(term.Id)
      .pipe(
        finalize(() => this.deletingId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toastService.success('Terme supprimé.');
          this.load();
        },
        error: () => this.toastService.error('Suppression impossible.')
      });
  }
}
