import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths, AppRoutes } from '../../../../Routes/app.routes.constants';
import { EducationArticleAdmin } from '../../../../Models/client-finance-models/education-article.model';
import { educationProductTypeLabel } from '../../../../Models/client-finance-models/education-product-type.util';
import { EducationService } from '../../../../services/education.service';

@Component({
  selector: 'app-admin-education-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-education-detail.component.html',
  styleUrl: './admin-education-detail.component.scss'
})
export class AdminEducationDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly educationService = inject(EducationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly adminPaths = AdminPaths;
  readonly backPath = ['/', AppRoutes.AdminRoot, AppRoutes.Education];

  detail: EducationArticleAdmin | null = null;
  loading = false;
  error: string | null = null;

  readonly productTypeLabel = educationProductTypeLabel;

  ngOnInit(): void {
    const raw = (this.route.snapshot.paramMap.get('slug') ?? '').trim();
    if (!raw) {
      this.error = 'Identifiant de l\'article manquant.';
      return;
    }
    this.loadById(raw);
  }

  private loadById(id: string): void {
    this.loading = true;
    this.error = null;
    this.educationService
      .getAdminById(id)
      .pipe(
        finalize(() => (this.loading = false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (payload: EducationArticleAdmin) => { this.detail = payload; },
        error: () => {
          this.detail = null;
          this.error = 'Impossible de charger cet article.';
        }
      });
  }
}
