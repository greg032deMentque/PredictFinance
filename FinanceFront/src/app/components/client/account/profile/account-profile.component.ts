import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AccountService } from '../../../../services/account.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-account-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './account-profile.component.html',
  styleUrl: './account-profile.component.scss'
})
export class AccountProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly accountService = inject(AccountService);
  private readonly toastService = inject(ToastService);

  protected loading = false;
  protected submitting = false;

  protected readonly form = this.fb.nonNullable.group({
    email: [{ value: '', disabled: true }],
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    phoneNumber: ['']
  });

  ngOnInit(): void {
    this.loadProfile();
  }

  protected loadProfile(): void {
    this.loading = true;
    this.accountService
      .getProfile()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (profile) => {
          this.form.patchValue({
            email: profile.Email,
            firstName: profile.FirstName,
            lastName: profile.LastName,
            phoneNumber: profile.PhoneNumber ?? ''
          });
        },
        error: () => this.toastService.error('Impossible de charger le profil.')
      });
  }

  protected submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitting = true;
    this.accountService
      .updateProfile({
        FirstName: value.firstName.trim(),
        LastName: value.lastName.trim(),
        PhoneNumber: value.phoneNumber.trim()
      })
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: (profile) => {
          this.form.patchValue({
            email: profile.Email,
            firstName: profile.FirstName,
            lastName: profile.LastName,
            phoneNumber: profile.PhoneNumber ?? ''
          });
          this.toastService.success('Profil mis à jour.');
        },
        error: () => this.toastService.error('Impossible de mettre à jour le profil.')
      });
  }
}
