import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountService } from '../../../services/account.service';
import { ToastService } from '../../../services/toastr.service';
import { AuthPaths, toCommands } from '../../../Routes/app.routes.constants';
import { PasswordFieldComponent } from '../../shared/password-field/password-field.component';
import { STRONG_PASSWORD_PATTERN } from '../password-policy';

function passwordsMatchValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const password = control.get('password')?.value ?? '';
    const confirmPassword = control.get('confirmPassword')?.value ?? '';
    return password === confirmPassword ? null : { passwordMismatch: true };
  };
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, PasswordFieldComponent],
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

  readonly form = this.fb.nonNullable.group(
    {
      email: [this.route.snapshot.queryParamMap.get('email') ?? '', [Validators.required, Validators.email]],
      token: [this.route.snapshot.queryParamMap.get('token') ?? '', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(6), Validators.pattern(STRONG_PASSWORD_PATTERN)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(6)]]
    },
    { validators: passwordsMatchValidator() }
  );

  readonly passwordsMismatch = computed(() => {
    const form = this.form;
    return form.hasError('passwordMismatch') && (form.controls.confirmPassword.touched || form.controls.password.touched);
  });

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.getRawValue();

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
