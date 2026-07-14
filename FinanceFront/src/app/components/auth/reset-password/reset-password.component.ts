import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountService } from '../../../services/account.service';
import { ToastService } from '../../../services/toastr.service';
import { AuthPaths, toCommands } from '../../../Routes/app.routes.constants';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly accountService = inject(AccountService);
  private readonly toastService = inject(ToastService);

  readonly authPaths = AuthPaths;
  readonly toCommands = toCommands;
  submitting = false;

  readonly form = this.fb.nonNullable.group({
    email: [this.route.snapshot.queryParamMap.get('email') ?? '', [Validators.required, Validators.email]],
    token: [this.route.snapshot.queryParamMap.get('token') ?? '', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.getRawValue();
    if (formValue.password !== formValue.confirmPassword) {
      this.toastService.error('La confirmation du mot de passe ne correspond pas.');
      return;
    }

    this.submitting = true;

    this.accountService
      .resetPassword({
        Email: formValue.email.trim().toLowerCase(),
        Token: formValue.token.trim(),
        Password: formValue.password,
        ConfirmPassword: formValue.confirmPassword
      })
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.toastService.success('Mot de passe réinitialisé.');
          void this.router.navigate(toCommands(this.authPaths.Login));
        },
        error: () => {
          this.toastService.error('Impossible de réinitialiser le mot de passe.');
        }
      });
  }
}
