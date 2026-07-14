import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { UserPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { ClientFinanceService } from '../../../../services/client-finance.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-contact-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './contact-page.component.html',
  styleUrl: './contact-page.component.scss'
})
export class ContactPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly clientFinanceService = inject(ClientFinanceService);
  private readonly toastService = inject(ToastService);

  readonly userPaths = UserPaths;
  readonly toCommands = toCommands;
  protected submitting = false;

  protected readonly form = this.fb.nonNullable.group({
    subject: ['', [Validators.required, Validators.maxLength(160)]],
    message: ['', [Validators.required, Validators.maxLength(4000)]]
  });

  protected submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const subject = value.subject.trim();
    const message = value.message.trim();

    if (!subject || !message) {
      this.form.markAllAsTouched();
      this.toastService.warning('Renseigne un sujet et un message.');
      return;
    }

    this.submitting = true;
    this.clientFinanceService
      .sendContactMessage(subject, message)
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.form.reset({
            subject: '',
            message: ''
          });
          this.toastService.success('Ton message a ete envoye.');
        },
        error: () => this.toastService.error('Envoi du message impossible pour le moment.')
      });
  }
}
