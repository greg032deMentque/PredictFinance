import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { LegalCardAdminItem, LegalCardUpsertRequest } from '../../../../Models/client-finance-models/legal-card.model';
import { LegalService } from '../../../../services/legal.service';
import { ToastService } from '../../../../services/toastr.service';

type FormMode = 'none' | 'add' | 'edit';

@Component({
  selector: 'app-admin-legal-cards',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-legal-cards.component.html',
  styleUrl: './admin-legal-cards.component.scss'
})
export class AdminLegalCardsComponent implements OnInit {
  private readonly legalService = inject(LegalService);
  private readonly toastService = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<LegalCardAdminItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly deletingId = signal<number | null>(null);
  readonly formMode = signal<FormMode>('none');

  private editId = 0;

  readonly itemForm = this.fb.nonNullable.group({
    key: ['', [Validators.required, Validators.maxLength(100)]],
    icon: ['', [Validators.required, Validators.maxLength(100)]],
    title: ['', [Validators.required, Validators.maxLength(300)]],
    description: ['', [Validators.required, Validators.maxLength(1000)]],
    effectiveDate: [''],
    targetRoute: [''],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    isPublished: [false]
  });

  get isEditing(): boolean { return this.formMode() === 'edit'; }
  get isPanelOpen(): boolean { return this.formMode() !== 'none'; }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.legalService
      .getAdminList()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.items.set(data ?? []),
        error: () => {
          this.items.set([]);
          this.error.set('Impossible de charger les cartes légales.');
        }
      });
  }

  openAdd(): void {
    this.itemForm.reset({ key: '', icon: 'bi-file-text', title: '', description: '', effectiveDate: '', targetRoute: '', displayOrder: 0, isPublished: false });
    this.formMode.set('add');
    this.editId = 0;
  }

  openEdit(item: LegalCardAdminItem): void {
    this.editId = item.id;
    this.itemForm.patchValue({
      key: item.key,
      icon: item.icon,
      title: item.title,
      description: item.description,
      effectiveDate: item.effectiveDate ?? '',
      targetRoute: item.targetRoute ?? '',
      displayOrder: item.displayOrder,
      isPublished: item.isPublished
    });
    this.formMode.set('edit');
  }

  closePanel(): void {
    this.formMode.set('none');
    this.itemForm.reset();
    this.editId = 0;
  }

  submit(): void {
    if (this.itemForm.invalid || this.submitting()) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const v = this.itemForm.getRawValue();
    const payload: LegalCardUpsertRequest = {
      key: v.key.trim(),
      icon: v.icon.trim(),
      title: v.title.trim(),
      description: v.description.trim(),
      effectiveDate: v.effectiveDate.trim() || null,
      targetRoute: v.targetRoute.trim() || null,
      displayOrder: v.displayOrder,
      isPublished: v.isPublished
    };

    this.submitting.set(true);
    const request$ = this.isEditing
      ? this.legalService.updateAdmin(this.editId, payload)
      : this.legalService.createAdmin(payload);

    request$
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toastService.success(this.isEditing ? 'Carte mise à jour.' : 'Carte ajoutée.');
          this.closePanel();
          this.load();
        },
        error: () => {
          this.toastService.error(this.isEditing ? 'Échec de la mise à jour.' : 'Échec de la création.');
        }
      });
  }

  delete(item: LegalCardAdminItem): void {
    if (this.deletingId() !== null) return;
    if (!confirm(`Supprimer « ${item.title} » ?`)) return;

    this.deletingId.set(item.id);
    this.legalService
      .deleteAdmin(item.id)
      .pipe(
        finalize(() => this.deletingId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toastService.success('Carte supprimée.');
          this.load();
        },
        error: () => this.toastService.error('Suppression impossible.')
      });
  }
}
