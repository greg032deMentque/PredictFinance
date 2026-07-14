import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, AppRoutes } from '../../../../Routes/app.routes.constants';
import { EducationUpsertRequest } from '../../../../Models/client-finance-models/education-article.model';
import { EducationService } from '../../../../services/education.service';
import { ToastService } from '../../../../services/toastr.service';
import { GeneralService } from '../../../../services/general-service.service';

const PRODUCT_TYPES = [
  { value: 'General', label: 'Général' },
  { value: 'PEA', label: 'PEA' },
  { value: 'PEL', label: 'PEL' },
  { value: 'PER', label: 'PER' },
  { value: 'LifeInsurance', label: 'Assurance vie' }
] as const;

@Component({
  selector: 'app-admin-education-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-education-form.component.html',
  styleUrl: './admin-education-form.component.scss'
})
export class AdminEducationFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly educationService = inject(EducationService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly adminPaths = AdminPaths;
  readonly productTypes = PRODUCT_TYPES;

  readonly isEdit = signal(false);
  readonly loading = signal(false);
  readonly submitting = signal(false);

  private editId = '';

  readonly form = this.fb.nonNullable.group({
    slug: ['', [Validators.required, Validators.maxLength(200)]],
    productType: ['General', [Validators.required]],
    title: ['', [Validators.required, Validators.maxLength(300)]],
    summary: ['', [Validators.required, Validators.maxLength(1000)]],
    bodyMarkdown: ['', [Validators.required]],
    displayOrder: [0, [Validators.required, Validators.min(0)]],
    isPublished: [false]
  });

  get title(): string {
    return this.isEdit() ? 'Modifier un article' : 'Ajouter un article';
  }

  get backPath(): (string | number)[] {
    return ['/', AppRoutes.AdminRoot, AppRoutes.Education];
  }

  ngOnInit(): void {
    const rawId = (GeneralService.getRouteParamDeep('id', this.route) ?? '').trim();

    if (rawId) {
      this.isEdit.set(true);
      this.editId = rawId;
      this.loadArticle(rawId);
    }
  }

  private loadArticle(id: string): void {
    this.loading.set(true);
    this.educationService
      .getAdminById(id)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (article) => {
          this.form.patchValue({
            slug: article.Slug,
            productType: article.ProductType,
            title: article.Title,
            summary: article.Summary,
            bodyMarkdown: article.BodyMarkdown,
            displayOrder: article.DisplayOrder,
            isPublished: article.IsPublished
          });
        },
        error: () => {
          this.toastService.error('Article introuvable.');
          void this.router.navigate(this.backPath);
        }
      });
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const payload: EducationUpsertRequest = {
      Slug: v.slug.trim(),
      ProductType: v.productType,
      Title: v.title.trim(),
      Summary: v.summary.trim(),
      BodyMarkdown: v.bodyMarkdown,
      DisplayOrder: v.displayOrder,
      IsPublished: v.isPublished
    };

    this.submitting.set(true);
    const request$ = this.isEdit()
      ? this.educationService.updateAdmin(this.editId, payload)
      : this.educationService.createAdmin(payload);

    request$
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toastService.success(this.isEdit() ? 'Article mis à jour.' : 'Article créé.');
          void this.router.navigate(this.backPath);
        },
        error: () => {
          this.toastService.error(this.isEdit() ? 'Échec de la mise à jour.' : 'Échec de la création.');
        }
      });
  }

  cancel(): void {
    void this.router.navigate(this.backPath);
  }
}
