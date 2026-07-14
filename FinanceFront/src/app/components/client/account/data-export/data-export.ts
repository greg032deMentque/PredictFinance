import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { AccountService } from '../../../../services/account.service';
import { ToastService } from '../../../../services/toastr.service';

type ExportStatus = 'idle' | 'pending' | 'requested';

@Component({
  selector: 'app-data-export',
  standalone: true,
  imports: [],
  templateUrl: './data-export.html',
  styleUrl: './data-export.scss'
})
export class DataExportComponent {
  private readonly accountService = inject(AccountService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly exportStatus = signal<ExportStatus>('idle');

  protected requestExport(): void {
    if (this.exportStatus() === 'pending') return;

    this.exportStatus.set('pending');
    this.accountService
      .requestDataExport()
      .pipe(
        finalize(() => {
          if (this.exportStatus() === 'pending') {
            this.exportStatus.set('idle');
          }
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.exportStatus.set('requested');
          this.toastService.success('Demande d\'export enregistrée. Vous recevrez un email sous 72h.');
        },
        error: () => this.toastService.error('Impossible de soumettre la demande d\'export.')
      });
  }
}
