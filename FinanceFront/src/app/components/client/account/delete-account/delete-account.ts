import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountService } from '../../../../services/account.service';
import { AuthService } from '../../../../services/AuthService.service';
import { ToastService } from '../../../../services/toastr.service';
import { AppRoutes } from '../../../../Routes/app.routes.constants';

@Component({
  selector: 'app-delete-account',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './delete-account.html',
  styleUrl: './delete-account.scss'
})
export class DeleteAccountComponent {
  private readonly accountService = inject(AccountService);
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly step = signal<1 | 2>(1);
  protected readonly deleting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    confirmDeletion: [false, [Validators.requiredTrue]]
  });

  protected goToStep2(): void {
    this.step.set(2);
  }

  protected goBack(): void {
    this.step.set(1);
    this.form.reset({ currentPassword: '', confirmDeletion: false });
  }

  protected submit(): void {
    if (this.form.invalid || this.deleting()) {
      this.form.markAllAsTouched();
      return;
    }

    const { currentPassword, confirmDeletion } = this.form.getRawValue();

    this.deleting.set(true);
    this.accountService
      .deleteAccount({ CurrentPassword: currentPassword, ConfirmDeletion: confirmDeletion })
      .pipe(
        finalize(() => this.deleting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.authService.logout();
          void this.router.navigate([AppRoutes.Login]);
        },
        error: () => this.toastService.error('Impossible de supprimer le compte. Vérifiez votre mot de passe.')
      });
  }
}
