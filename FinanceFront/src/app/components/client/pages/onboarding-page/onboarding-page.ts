import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { OnboardingGuidance } from '../../../../Models/client-finance-models/learn-parameter-models';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-onboarding-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './onboarding-page.html',
  styleUrl: './onboarding-page.scss'
})
export class OnboardingPageComponent implements OnInit {
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly toCommands = toCommands;
  protected readonly guidance = signal<OnboardingGuidance | null>(null);
  protected readonly loading = signal(false);

  ngOnInit(): void {
    this.loadGuidance();
  }

  private loadGuidance(): void {
    this.loading.set(true);
    this.clientFinanceService
      .getOnboarding()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => {
          if (!data.ShouldDisplay) {
            void this.router.navigate(toCommands(UserPaths.Dashboard));
            return;
          }
          this.guidance.set(data);
        },
        error: () => this.toastService.error('Impossible de charger le guide de démarrage.')
      });
  }
}
