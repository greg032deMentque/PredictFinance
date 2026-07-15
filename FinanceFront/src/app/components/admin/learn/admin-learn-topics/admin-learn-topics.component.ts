import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { LearnTopicAdminItem, LearnTopicUpsertRequest } from '../../../../Models/client-finance-models/learn-topic-admin.model';
import { LearnTopicsService } from '../../../../services/learn-topics.service';
import { ToastService } from '../../../../services/toastr.service';

type FormMode = 'none' | 'add' | 'edit';

@Component({
  selector: 'app-admin-learn-topics',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-learn-topics.component.html',
  styleUrl: './admin-learn-topics.component.scss'
})
export class AdminLearnTopicsComponent implements OnInit {
  private readonly learnTopicsService = inject(LearnTopicsService);
  private readonly toastService = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly items = signal<LearnTopicAdminItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly deletingId = signal<string | null>(null);
  readonly formMode = signal<FormMode>('none');

  private editId = '';

  readonly itemForm = this.fb.nonNullable.group({
    topicId: ['', [Validators.required, Validators.maxLength(100)]],
    title: ['', [Validators.required, Validators.maxLength(300)]],
    summary: ['', [Validators.required, Validators.maxLength(1000)]],
    routePath: ['', [Validators.required, Validators.maxLength(300)]],
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
    this.learnTopicsService
      .getAdminList()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.items.set(data ?? []),
        error: () => {
          this.items.set([]);
          this.error.set('Impossible de charger les topics.');
        }
      });
  }

  openAdd(): void {
    this.itemForm.reset({ topicId: '', title: '', summary: '', routePath: '', displayOrder: 0, isPublished: false });
    this.formMode.set('add');
    this.editId = '';
  }

  openEdit(item: LearnTopicAdminItem): void {
    this.editId = item.Id;
    this.itemForm.patchValue({
      topicId: item.TopicId,
      title: item.Title,
      summary: item.Summary,
      routePath: item.RoutePath,
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
    const payload: LearnTopicUpsertRequest = {
      topicId: v.topicId.trim(),
      title: v.title.trim(),
      summary: v.summary.trim(),
      routePath: v.routePath.trim(),
      displayOrder: v.displayOrder,
      isPublished: v.isPublished
    };

    this.submitting.set(true);
    const request$ = this.isEditing
      ? this.learnTopicsService.updateAdmin(this.editId, payload)
      : this.learnTopicsService.createAdmin(payload);

    request$
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toastService.success(this.isEditing ? 'Topic mis à jour.' : 'Topic ajouté.');
          this.closePanel();
          this.load();
        },
        error: () => {
          this.toastService.error(this.isEditing ? 'Échec de la mise à jour.' : 'Échec de la création.');
        }
      });
  }

  delete(item: LearnTopicAdminItem): void {
    if (this.deletingId() !== null) return;
    if (!confirm(`Supprimer « ${item.Title} » ?`)) return;

    this.deletingId.set(item.Id);
    this.learnTopicsService
      .deleteAdmin(item.Id)
      .pipe(
        finalize(() => this.deletingId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toastService.success('Topic supprimé.');
          this.load();
        },
        error: () => this.toastService.error('Suppression impossible.')
      });
  }
}
