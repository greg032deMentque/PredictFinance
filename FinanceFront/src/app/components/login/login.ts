import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AllModule } from '../../module/allModule.module';
import { AuthStore } from '../../core/auth.store';
import { AuthService } from '../../services/AuthService.service';
import { ToastService } from '../../services/toastr.service';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { TokenResponse } from '../../Models/token-response';
import { StorageService } from '../../services/storage.service';
import { AdminPaths, UserPaths } from '../../Routes/app.routes.constants';

type ForgotPasswordRequest = { email: string };

@Component({
  selector: 'app-login',
  imports: [AllModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly storageService = inject(StorageService);
  private readonly auth = inject(AuthStore);

  readonly isSubmitting = signal(false);

  readonly isForgotOpen = signal(false);
  readonly isForgotSubmitting = signal(false);
  readonly forgotEmail = signal('');
  readonly forgotError = signal<string | null>(null);
  readonly forgotSuccess = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    Email: this.fb.nonNullable.control('', [Validators.required, Validators.email]),
    Password: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(4)]),
  });

  ngOnInit(): void {
    localStorage.clear();
    sessionStorage.clear();
  }

  openForgotPassword(): void {
    const email = this.form.controls.Email.value?.trim() ?? '';
    this.forgotEmail.set(email);
    this.forgotError.set(null);
    this.forgotSuccess.set(null);
    this.isForgotOpen.set(true);
  }

  closeForgotPassword(): void {
    this.isForgotOpen.set(false);
  }

  confirmForgotPassword(): void {
    const email = (this.forgotEmail() ?? '').trim();
    if (!email) {
      this.forgotError.set('Veuillez renseigner un email.');
      return;
    }

    this.isForgotSubmitting.set(true);
    this.forgotError.set(null);
    this.forgotSuccess.set(null);

    const payload: ForgotPasswordRequest = { email };

    this.http
      .post<void>(environment.apiUrl + 'Account/ForgotPassword', payload)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isForgotSubmitting.set(false)))
      .subscribe({
        next: () => this.forgotSuccess.set('Si l’adresse existe, un email de réinitialisation a été envoyé.'),
        error: () => this.forgotError.set("Impossible d’envoyer l’email pour le moment."),
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    this.http
      .post<TokenResponse>(environment.apiUrl + 'Account/Login', this.form.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: (obj) => {
          this.storageService.SetToken(obj.Token);
          this.storageService.SetRefreshToken(obj.RefreshToken);

          this.auth.syncFromStorage();

          void this.router.navigate([
            this.auth.canAccessAdmin() ? AdminPaths.Dashboard : UserPaths.Dashboard
          ]);

        },
        error: () => this.toastr.error('Échec de la connexion.'),
      });
  }
}
