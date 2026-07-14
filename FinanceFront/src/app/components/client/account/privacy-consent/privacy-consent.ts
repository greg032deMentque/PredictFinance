import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  AccountService,
  UpdateUserConsentsRequest,
  UserConsents
} from '../../../../services/account.service';
import { ToastService } from '../../../../services/toastr.service';
import { UserPaths } from '../../../../Routes/app.routes.constants';

@Component({
  selector: 'app-privacy-consent',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './privacy-consent.html',
  styleUrl: './privacy-consent.scss'
})
export class PrivacyConsentComponent implements OnInit {
  private readonly accountService = inject(AccountService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly consents = signal<UserConsents | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);

  protected readonly dataExportPath = '/' + UserPaths.DataExport;
  protected readonly deleteAccountPath = '/' + UserPaths.DeleteAccount;

  ngOnInit(): void {
    this.loadConsents();
  }

  private loadConsents(): void {
    this.loading.set(true);
    this.accountService
      .getConsents()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.consents.set(data),
        error: () => this.toastService.error('Impossible de charger vos consentements.')
      });
  }

  protected toggleAnalytics(): void {
    const current = this.consents();
    if (!current) return;
    this.consents.set({ ...current, AnalyticsConsent: !current.AnalyticsConsent });
  }

  protected toggleMarketing(): void {
    const current = this.consents();
    if (!current) return;
    this.consents.set({ ...current, MarketingEmailConsent: !current.MarketingEmailConsent });
  }

  protected toggleProductImprovement(): void {
    const current = this.consents();
    if (!current) return;
    this.consents.set({ ...current, ProductImprovementConsent: !current.ProductImprovementConsent });
  }

  protected save(): void {
    const current = this.consents();
    if (!current || this.saving()) return;

    const request: UpdateUserConsentsRequest = {
      AnalyticsConsent: current.AnalyticsConsent,
      MarketingEmailConsent: current.MarketingEmailConsent,
      ProductImprovementConsent: current.ProductImprovementConsent
    };

    this.saving.set(true);
    this.accountService
      .updateConsents(request)
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (updated) => {
          this.consents.set(updated);
          this.toastService.success('Vos consentements ont été mis à jour.');
        },
        error: () => this.toastService.error('Impossible de mettre à jour vos consentements.')
      });
  }
}
