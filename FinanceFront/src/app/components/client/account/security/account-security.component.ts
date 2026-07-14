import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AccountService } from '../../../../services/account.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-account-security',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './account-security.component.html',
  styleUrl: './account-security.component.scss'
})
export class AccountSecurityComponent {
  private readonly fb = inject(FormBuilder);
  private readonly accountService = inject(AccountService);
  private readonly toastService = inject(ToastService);

  protected submitting = false;

  protected readonly form = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required, Validators.minLength(6)]]
  });

  protected submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    if (value.newPassword !== value.confirmPassword) {
      this.toastService.error('La confirmation du mot de passe ne correspond pas.');
      return;
    }

    this.submitting = true;
    this.accountService
      .changePassword({
        CurrentPassword: value.currentPassword,
        NewPassword: value.newPassword
      })
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.form.reset({ currentPassword: '', newPassword: '', confirmPassword: '' });
          this.toastService.success('Mot de passe mis à jour.');
        },
        error: () => this.toastService.error('Impossible de modifier le mot de passe.')
      });
  }
}
