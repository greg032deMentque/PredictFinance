import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AllModule } from '../../module/allModule.module';
import { AuthStore } from '../../core/auth.store';
import { AuthService } from '../../services/AuthService.service';
import { ToastService } from '../../services/toastr.service';
import { StorageService } from '../../services/storage.service';
import { AdminPaths, AuthPaths, UserPaths } from '../../Routes/app.routes.constants';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [AllModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  private readonly router = inject(Router);
  private readonly toastr = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly storageService = inject(StorageService);
  private readonly auth = inject(AuthStore);
  private readonly authService = inject(AuthService);

  readonly authPaths = AuthPaths;
  readonly isSubmitting = signal(false);

  readonly form = this.fb.nonNullable.group({
    Email: this.fb.nonNullable.control('', [Validators.required, Validators.email]),
    Password: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(4)]),
  });

  ngOnInit(): void {
    this.storageService.RemoveToken();
    this.storageService.RemoveRefreshToken();
    this.auth.clear(false);
  }

  openForgotPassword(): void {
    const email = (this.form.controls.Email.value ?? '').trim();
    const queryParams = email ? { email } : undefined;
    void this.router.navigate(['/', this.authPaths.ForgotPassword], { queryParams });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    this.authService
      .login(this.form.getRawValue())
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          const isAdmin = this.authService.isAdmin();
          void this.router.navigate([isAdmin ? AdminPaths.Dashboard : UserPaths.Dashboard]);
        },
        error: () => this.toastr.error('Échec de la connexion.'),
      });
  }
}
