import { CommonModule } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthPaths, toCommands } from '../../../Routes/app.routes.constants';
import { AccountService } from '../../../services/account.service';
import { ToastService } from '../../../services/toastr.service';
import { environment } from '../../../../environments/environment';
import { PasswordFieldComponent } from '../../shared/password-field/password-field.component';
import { STRONG_PASSWORD_PATTERN } from '../password-policy';

type RegisterPageState = 'form' | 'email-sent' | 'confirming' | 'confirmed' | 'confirm-failed';

function passwordsMatchValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const password = control.get('Password')?.value ?? '';
    const confirmPassword = control.get('ConfirmPassword')?.value ?? '';
    return password === confirmPassword ? null : { passwordMismatch: true };
  };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, PasswordFieldComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly accountService = inject(AccountService);
  private readonly toastService = inject(ToastService);

  private lastConfirmationKey = '';

  readonly authPaths = AuthPaths;
  readonly toCommands = toCommands;
  readonly termsUrl = environment.termsUrl;
  readonly privacyUrl = environment.privacyUrl;

  readonly pageState = signal<RegisterPageState>('form');
  readonly confirmationEmail = signal('');
  readonly isSubmitting = signal(false);
  readonly isResending = signal(false);

  readonly form = this.fb.nonNullable.group(
    {
      Email: this.fb.nonNullable.control('', [Validators.required, Validators.email]),
      Password: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(6), Validators.pattern(STRONG_PASSWORD_PATTERN)]),
      ConfirmPassword: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(6)]),
      AcceptLegal: this.fb.nonNullable.control(false, [Validators.requiredTrue])
    },
    { validators: passwordsMatchValidator() }
  );

  readonly passwordsMismatch = computed(() => {
    const form = this.form;
    return form.hasError('passwordMismatch') && (form.controls.ConfirmPassword.touched || form.controls.Password.touched);
  });

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const email = (params.get('email') ?? '').trim().toLowerCase();
        const token = (params.get('token') ?? '').trim();

        if (email) {
          this.form.controls.Email.setValue(email);
          this.confirmationEmail.set(email);
        }

        if (!email || !token) {
          return;
        }

        const confirmationKey = `${email}|${token}`;
        if (this.lastConfirmationKey === confirmationKey) {
          return;
        }

        this.lastConfirmationKey = confirmationKey;
        this.pageState.set('confirming');
        this.confirmEmail(email, token);
      });
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.getRawValue();
    const normalizedEmail = formValue.Email.trim().toLowerCase();

    this.isSubmitting.set(true);

    this.accountService
      .register({
        Email: normalizedEmail,
        Password: formValue.Password,
        ConfirmPassword: formValue.ConfirmPassword
      })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.confirmationEmail.set(normalizedEmail);
          this.pageState.set('email-sent');
        },
        error: () => {
          this.pageState.set('form');
        }
      });
  }

  resendConfirmationEmail(): void {
    const email = (this.confirmationEmail() || this.form.controls.Email.value || '').trim().toLowerCase();
    if (!email || this.isResending()) {
      if (!email) {
        this.form.controls.Email.markAsTouched();
      }
      return;
    }

    this.isResending.set(true);

    this.accountService
      .resendConfirmationEmail({ Email: email })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isResending.set(false)))
      .subscribe({
        next: () => {
          this.confirmationEmail.set(email);
          this.pageState.set('email-sent');
          this.toastService.success('Si le compte existe et reste à confirmer, un nouvel email a été envoyé.');
        },
        error: () => {
          if (this.pageState() === 'confirming') {
            this.pageState.set('confirm-failed');
          }
        }
      });
  }

  openForgotPassword(): void {
    const email = (this.confirmationEmail() || this.form.controls.Email.value || '').trim().toLowerCase();
    const queryParams = email ? { email } : undefined;
    void this.router.navigate(toCommands(this.authPaths.ForgotPassword), { queryParams });
  }

  private confirmEmail(email: string, token: string): void {
    this.accountService
      .confirmEmail({ Email: email, Token: token })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.pageState.set('confirmed');
          this.toastService.success('Adresse email confirmée. Vous pouvez maintenant vous connecter.');
          window.setTimeout(() => {
            void this.router.navigate(toCommands(this.authPaths.Login), { queryParams: { email } });
          }, 1200);
        },
        error: () => {
          this.pageState.set('confirm-failed');
        }
      });
  }
}
