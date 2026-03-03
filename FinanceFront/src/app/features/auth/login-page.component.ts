import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { SessionService } from '../../core/services/session.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.scss'
})
export class LoginPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly sessionService = inject(SessionService);
  private readonly router = inject(Router);

  readonly submitting = signal(false);
  readonly loginError = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  onSubmit(): void {
    this.loginError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    const { email, password } = this.form.getRawValue();
    const authenticated = this.sessionService.login(email, password);
    this.submitting.set(false);

    if (!authenticated) {
      this.loginError.set('Identifiants invalides. Utilise un email valide et un mot de passe de 6 caracteres minimum.');
      return;
    }

    this.router.navigateByUrl('/app/dashboard');
  }
}
