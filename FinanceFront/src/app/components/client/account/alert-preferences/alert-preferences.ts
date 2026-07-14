import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import {
  AccountService,
  AlertPreferences,
  UpdateAlertPreferencesRequest
} from '../../../../services/account.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-alert-preferences',
  standalone: true,
  imports: [],
  templateUrl: './alert-preferences.html',
  styleUrl: './alert-preferences.scss'
})
export class AlertPreferencesComponent implements OnInit {
  private readonly accountService = inject(AccountService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly preferences = signal<AlertPreferences | null>(null);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);

  ngOnInit(): void {
    this.loadPreferences();
  }

  private loadPreferences(): void {
    this.loading.set(true);
    this.accountService
      .getAlertPreferences()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => this.preferences.set(data),
        error: () => this.toastService.error('Impossible de charger vos préférences d\'alertes.')
      });
  }

  protected togglePatternStateChange(): void {
    const current = this.preferences();
    if (!current) return;
    this.preferences.set({ ...current, AlertPatternStateChangeEnabled: !current.AlertPatternStateChangeEnabled });
  }

  protected toggleLevelCrossed(): void {
    const current = this.preferences();
    if (!current) return;
    this.preferences.set({ ...current, AlertLevelCrossedEnabled: !current.AlertLevelCrossedEnabled });
  }

  protected toggleDataStale(): void {
    const current = this.preferences();
    if (!current) return;
    this.preferences.set({ ...current, AlertDataStaleEnabled: !current.AlertDataStaleEnabled });
  }

  protected save(): void {
    const current = this.preferences();
    if (!current || this.saving()) return;

    const request: UpdateAlertPreferencesRequest = {
      AlertPatternStateChangeEnabled: current.AlertPatternStateChangeEnabled,
      AlertLevelCrossedEnabled: current.AlertLevelCrossedEnabled,
      AlertDataStaleEnabled: current.AlertDataStaleEnabled
    };

    this.saving.set(true);
    this.accountService
      .updateAlertPreferences(request)
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (updated) => {
          this.preferences.set(updated);
          this.toastService.success('Vos préférences d\'alertes ont été mises à jour.');
        },
        error: () => this.toastService.error('Impossible de mettre à jour vos préférences d\'alertes.')
      });
  }
}
