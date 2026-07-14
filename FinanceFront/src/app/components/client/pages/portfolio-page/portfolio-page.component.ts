import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PortfolioType, UserPortfolioViewModel, getPortfolioTypeLabel } from '../../../../Models/client-finance-models/user-portfolio.model';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { PortfolioService } from '../../../../services/portfolio.service';
import { ToastService } from '../../../../services/toastr.service';
import { ConfirmModalComponent } from '../../../shared/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-portfolio-page',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, ConfirmModalComponent],
  templateUrl: './portfolio-page.component.html',
  styleUrl: './portfolio-page.component.scss'
})
export class PortfolioPageComponent implements OnInit {
  private readonly portfolioService = inject(PortfolioService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;
  readonly getPortfolioTypeLabel = getPortfolioTypeLabel;
  readonly portfolioTypes: PortfolioType[] = ['CompteTitres', 'Pea', 'AssuranceVie', 'Per', 'Autre'];

  portfolios = signal<UserPortfolioViewModel[]>([]);
  portfoliosLoading = signal(false);
  formLoading = signal(false);
  createError = signal<string | null>(null);
  portfolioToDeleteId = signal<string | null>(null);
  renamingPortfolioId = signal<string | null>(null);
  renameNameError = signal<string | null>(null);

  readonly createForm = this.fb.nonNullable.group({
    name: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
    portfolioType: this.fb.nonNullable.control<PortfolioType>('CompteTitres', [Validators.required])
  });

  readonly renameForm = this.fb.nonNullable.group({
    name: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)])
  });

  ngOnInit(): void {
    this.loadPortfolios();
  }

  navigateToDetail(portfolioId: string): void {
    void this.router.navigate(toCommands(UserPaths.PortfolioDetail(portfolioId)));
  }

  private loadPortfolios(): void {
    this.portfoliosLoading.set(true);
    this.portfolioService.getPortfolios()
      .pipe(finalize(() => this.portfoliosLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => this.portfolios.set(items),
        error: () => this.portfolios.set([])
      });
  }

  submitCreatePortfolio(): void {
    this.createError.set(null);
    if (this.createForm.invalid || this.formLoading()) {
      this.createForm.markAllAsTouched();
      return;
    }
    const { name, portfolioType } = this.createForm.getRawValue();
    this.formLoading.set(true);
    this.portfolioService.createPortfolio({ Name: name, PortfolioType: portfolioType })
      .pipe(finalize(() => this.formLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (created) => {
          this.portfolios.set([...this.portfolios(), created]);
          this.createForm.reset({ name: '', portfolioType: 'CompteTitres' });
          this.toastService.success(`Portefeuille « ${created.Name} » créé.`);
        },
        error: (err) => {
          if (err?.status === 409) {
            this.createError.set('Ce nom de portefeuille est déjà utilisé. Choisis un autre nom.');
          }
        }
      });
  }

  startRename(p: UserPortfolioViewModel): void {
    this.renamingPortfolioId.set(p.Id);
    this.renameNameError.set(null);
    this.renameForm.reset({ name: p.Name });
  }

  cancelRename(): void {
    this.renamingPortfolioId.set(null);
    this.renameNameError.set(null);
  }

  submitRename(portfolioId: string): void {
    this.renameNameError.set(null);
    if (this.renameForm.invalid || this.formLoading()) {
      this.renameForm.markAllAsTouched();
      return;
    }
    const { name } = this.renameForm.getRawValue();
    this.formLoading.set(true);
    this.portfolioService.renamePortfolio(portfolioId, { Name: name })
      .pipe(finalize(() => this.formLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.portfolios.set(this.portfolios().map(p => p.Id === portfolioId ? updated : p));
          this.renamingPortfolioId.set(null);
          this.toastService.success('Portefeuille renommé.');
        },
        error: (err) => {
          if (err?.status === 409) {
            this.renameNameError.set('Ce nom est déjà utilisé par un autre portefeuille.');
          }
        }
      });
  }

  requestDeletePortfolio(portfolioId: string): void {
    this.portfolioToDeleteId.set(portfolioId);
  }

  confirmDeletePortfolio(): void {
    const id = this.portfolioToDeleteId();
    if (!id || this.formLoading()) return;
    this.formLoading.set(true);
    this.portfolioService.deletePortfolio(id)
      .pipe(finalize(() => this.formLoading.set(false)), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.portfolios.set(this.portfolios().filter(p => p.Id !== id));
          this.portfolioToDeleteId.set(null);
          this.toastService.success('Portefeuille supprimé.');
        },
        error: () => this.portfolioToDeleteId.set(null)
      });
  }

  get portfolioToDeleteName(): string {
    const id = this.portfolioToDeleteId();
    return id ? (this.portfolios().find(p => p.Id === id)?.Name ?? '') : '';
  }
}
