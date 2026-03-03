import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminUser } from '../../../../Models/admin-user';
import { AdminPaths } from '../../../../Routes/app.routes.constants';
import { AdminUsersService } from '../../../../services/admin-users.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-admin-user-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-user-form.component.html',
  styleUrl: './admin-user-form.component.scss'
})
export class AdminUserFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly adminUsersService = inject(AdminUsersService);
  private readonly toastService = inject(ToastService);

  readonly adminPaths = AdminPaths;
  isEdit = false;
  loading = false;
  submitting = false;
  private editedUserId = '';

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.minLength(6)]],
    role: ['User' as 'User' | 'Admin' | 'SuperAdmin', [Validators.required]],
    isActive: [true]
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }

    this.isEdit = true;
    this.editedUserId = id;
    this.loading = true;

    this.adminUsersService
      .getUserById(id)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (user) => {
          this.form.patchValue({
            firstName: user.firstName,
            lastName: user.lastName,
            email: user.email,
            role: user.role,
            isActive: user.isActive
          });
        },
        error: () => {
          this.toastService.error('Utilisateur introuvable.');
          void this.router.navigate(['/', this.adminPaths.UsersList]);
        }
      });

    this.form.controls.password.clearValidators();
    this.form.controls.password.updateValueAndValidity();
  }

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.getRawValue();
    const user = new AdminUser({
      id: this.editedUserId,
      firstName: payload.firstName.trim(),
      lastName: payload.lastName.trim(),
      email: payload.email.trim().toLowerCase(),
      role: payload.role,
      isActive: payload.isActive
    });
    this.submitting = true;

    if (this.isEdit) {
      this.adminUsersService
        .updateUser(user)
        .pipe(finalize(() => (this.submitting = false)))
        .subscribe({
          next: () => {
            this.toastService.success('Utilisateur mis a jour.');
            void this.router.navigate(['/', this.adminPaths.UsersList]);
          },
          error: () => {
            this.toastService.error('Echec de la mise a jour.');
          }
        });
    } else {
      const password = payload.password.trim();
      if (!password || password.length < 6) {
        this.submitting = false;
        this.form.controls.password.setErrors({ minlength: true });
        this.toastService.error('Le mot de passe est requis (6 caracteres min).');
        return;
      }

      this.adminUsersService
        .addUser(user, password)
        .pipe(finalize(() => (this.submitting = false)))
        .subscribe({
          next: () => {
            this.toastService.success('Utilisateur ajoute.');
            void this.router.navigate(['/', this.adminPaths.UsersList]);
          },
          error: () => {
            this.toastService.error('Echec de la creation utilisateur.');
          }
        });
    }
  }

  cancel(): void {
    void this.router.navigate(['/', this.adminPaths.UsersList]);
  }

  get title(): string {
    return this.isEdit ? 'Modifier un utilisateur' : 'Ajouter un utilisateur';
  }

  get submitLabel(): string {
    return this.isEdit ? 'Mettre a jour' : 'Creer';
  }

  get roleOptions(): Array<'User' | 'Admin' | 'SuperAdmin'> {
    return ['User', 'Admin', 'SuperAdmin'];
  }
}
