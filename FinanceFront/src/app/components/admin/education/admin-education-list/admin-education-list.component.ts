import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, AppRoutes } from '../../../../Routes/app.routes.constants';
import { EducationArticleAdmin } from '../../../../Models/client-finance-models/education-article.model';
import { educationProductTypeLabel } from '../../../../Models/client-finance-models/education-product-type.util';
import { EducationService } from '../../../../services/education.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-admin-education-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-education-list.component.html',
  styleUrl: './admin-education-list.component.scss'
})
export class AdminEducationListComponent implements OnInit {
  private readonly educationService = inject(EducationService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly adminPaths = AdminPaths;
  readonly items = signal<EducationArticleAdmin[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly deletingId = signal<string | null>(null);

  readonly productTypeLabel = educationProductTypeLabel;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.educationService
      .getAdminList()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (payload) => this.items.set(payload ?? []),
        error: () => {
          this.items.set([]);
          this.error.set('Impossible de charger les articles éducatifs.');
        }
      });
  }

  goToAdd(): void {
    void this.router.navigate(['/', AppRoutes.AdminRoot, AppRoutes.Education, AppRoutes.Add]);
  }

  goToEdit(id: string): void {
    void this.router.navigate(['/', AppRoutes.AdminRoot, AppRoutes.Education, AppRoutes.Edit, id]);
  }

  delete(item: EducationArticleAdmin): void {
    if (this.deletingId() !== null) return;
    if (!confirm(`Supprimer « ${item.Title} » ?`)) return;

    this.deletingId.set(item.Id);
    this.educationService
      .deleteAdmin(item.Id)
      .pipe(
        finalize(() => this.deletingId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.toastService.success('Article supprimé.');
          this.load();
        },
        error: () => this.toastService.error('Suppression impossible.')
      });
  }
}
