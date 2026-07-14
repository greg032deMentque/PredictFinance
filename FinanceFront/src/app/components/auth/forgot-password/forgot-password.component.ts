import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountService } from '../../../services/account.service';
import { ToastService } from '../../../services/toastr.service';
import { AuthPaths, toCommands } from '../../../Routes/app.routes.constants';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly accountService = inject(AccountService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly authPaths = AuthPaths;
  readonly toCommands = toCommands;

  submitting = false;

  readonly form = this.fb.nonNullable.group({
    email: [this.route.snapshot.queryParamMap.get('email') ?? '', [Validators.required, Validators.email]]
  });

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;

    this.accountService
      .forgotPassword({ Email: this.form.controls.email.value.trim().toLowerCase() })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.toastService.success('Si un compte existe, un email de réinitialisation a été envoyé.');
        },
        error: () => {
          this.toastService.error('Impossible de lancer la réinitialisation.');
        }
      });
  }
}
