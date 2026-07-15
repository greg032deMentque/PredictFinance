import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { FaqAdminItem, FaqUpsertRequest } from '../../../../Models/client-finance-models/faq.model';
import { FaqService } from '../../../../services/faq.service';
import { ToastService } from '../../../../services/toastr.service';

type FormMode = 'none' | 'add' | 'edit';

@Component({
  selector: 'app-admin-faq',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-faq.component.html',
  styleUrl: './admin-faq.component.scss'
})
export class AdminFaqComponent implements OnInit {
  private readonly faqService = inject(FaqService);
  private readonly toastService = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<FaqAdminItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly formMode = signal<FormMode>('none');

  private editId = '';

  readonly itemForm = this.fb.nonNullable.group({
    category: ['', [Validators.required, Validators.maxLength(200)]],
    question: ['', [Validators.required, Validators.maxLength(500)]],
    answer: ['', [Validators.required, Validators.maxLength(4000)]],
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
    this.faqService
      .getAdminList()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.items.set(data ?? []),
        error: () => {
          this.items.set([]);
          this.error.set('Impossible de charger la FAQ.');
        }
      });
  }

  openAdd(): void {
    this.itemForm.reset({ category: '', question: '', answer: '', displayOrder: 0, isPublished: false });
    this.formMode.set('add');
    this.editId = '';
  }

  openEdit(item: FaqAdminItem): void {
    this.editId = item.Id;
    this.itemForm.patchValue({
      category: item.Category,
      question: item.Question,
      answer: item.Answer,
      displayOrder: item.DisplayOrder,
      isPublished: item.IsPublished
    });
    this.formMode.set('edit');
  }

  closePanel(): void {
    this.formMode.set('none');
    this.itemForm.reset();
    this.editId = '';
  }

  submit(): void {
    if (this.itemForm.invalid || this.submitting()) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const v = this.itemForm.getRawValue();
    const payload: FaqUpsertRequest = {
      Category: v.category.trim(),
      Question: v.question.trim(),
      Answer: v.answer.trim(),
      DisplayOrder: v.displayOrder,
      IsPublished: v.isPublished
    };

    this.submitting.set(true);
    const request$ = this.isEditing
      ? this.faqService.updateAdmin(this.editId, payload)
      : this.faqService.createAdmin(payload);

    request$
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toastService.success(this.isEditing ? 'Question mise à jour.' : 'Question ajoutée.');
          this.closePanel();
          this.load();
        },
        error: () => {
          this.toastService.error(this.isEditing ? 'Échec de la mise à jour.' : 'Échec de la création.');
        }
      });
  }

  delete(item: FaqAdminItem): void {
    if (this.deletingId() !== null) return;
    if (!confirm(`Supprimer « ${item.Question} » ?`)) return;

    this.deletingId.set(item.Id);
    this.faqService
      .deleteAdmin(item.Id)
      .pipe(
        finalize(() => this.deletingId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toastService.success('Question supprimée.');
          this.load();
        },
        error: () => this.toastService.error('Suppression impossible.')
      });
  }
}
