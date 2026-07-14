import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { User } from '../../../../Models/User';
import { AdminUserUpsertRequest } from '../../../../Models/admin-user-upsert-request';
import { UserRole } from '../../../../Models/user-role';
import { AdminPaths, toCommands } from '../../../../Routes/app.routes.constants';
import { GeneralService } from '../../../../services/general-service.service';
import { ToastService } from '../../../../services/toastr.service';
import { environment } from '../../../../../environments/environment';

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
  private readonly http = inject(HttpClient);
  private readonly toastService = inject(ToastService);

  readonly adminPaths = AdminPaths;
  readonly roleOptions: UserRole[] = [];

  isEdit = false;
  loading = false;
  submitting = false;
  private editedUserId = '';

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: [''],
    password: ['', [Validators.minLength(6)]],
    roleId: ['', [Validators.required]],
    isActive: [true]
  });

  ngOnInit(): void {
    this.loadRoles();

    const routeUserId = GeneralService.getRouteParamDeep('id', this.route);
    const normalizedUserId = (routeUserId ?? '').trim();
    if (!normalizedUserId) {
      this.isEdit = false;
      return;
    }

    this.isEdit = true;
    this.editedUserId = normalizedUserId;
    this.form.controls.password.clearValidators();
    this.form.controls.password.addValidators(Validators.minLength(6));
    this.form.controls.password.updateValueAndValidity();
    this.loadUser(normalizedUserId);
  }

  private loadRoles(): void {
    this.http.get<UserRole[]>(`${environment.apiUrl}admin/users/roles`).subscribe({
      next: (roles) => {
        this.roleOptions.splice(0, this.roleOptions.length, ...(roles ?? []));

        if (!this.isEdit && this.roleOptions.length > 0 && !this.form.controls.roleId.value) {
          this.form.controls.roleId.setValue(this.roleOptions[0].RoleId);
        }
      },
      error: () => {
        this.toastService.error('Impossible de charger la liste des rôles.');
      }
    });
  }

  private loadUser(userId: string): void {
    this.loading = true;

    this.http
      .get<User>(`${environment.apiUrl}admin/users/${encodeURIComponent(userId)}`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (user) => {
          this.form.patchValue({
            firstName: user.FirstName,
            lastName: user.LastName,
            email: user.Email,
            phoneNumber: user.PhoneNumber ?? '',
            roleId: user.Roles?.[0]?.RoleId ?? '',
            isActive: user.IsActive
          });
        },
        error: () => {
          this.toastService.error('Utilisateur introuvable.');
          void this.router.navigate(toCommands(this.adminPaths.UsersList));
        }
      });
  }

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    const formValue = this.form.getRawValue();
    const selectedRole = this.roleOptions.find((role) => role.RoleId === formValue.roleId);
    if (!selectedRole) {
      this.toastService.error('Rôle invalide.');
      return;
    }

    const password = formValue.password.trim();
    if (!this.isEdit && password.length < 6) {
      this.form.controls.password.setErrors({ minlength: true });
      this.toastService.error('Le mot de passe initial est requis et doit contenir au moins 6 caractères.');
      return;
    }

    const payload: AdminUserUpsertRequest = {
      userId: this.isEdit ? this.editedUserId : undefined,
      firstName: formValue.firstName.trim(),
      lastName: formValue.lastName.trim(),
      email: formValue.email.trim().toLowerCase(),
      password: password ? password : undefined,
      role: selectedRole.RoleName,
      isActive: formValue.isActive,
      phoneNumber: formValue.phoneNumber.trim()
    };

    this.submitting = true;

    const request$ = this.isEdit
      ? this.http.put(`${environment.apiUrl}admin/users/${encodeURIComponent(this.editedUserId)}`, payload)
      : this.http.post(`${environment.apiUrl}admin/users`, payload);

    request$
      .pipe(finalize(() => (this.submitting = false)))
      .subscribe({
        next: () => {
          this.toastService.success(this.isEdit ? 'Utilisateur mis à jour.' : 'Utilisateur ajouté.');
          void this.router.navigate(toCommands(this.adminPaths.UsersList));
        },
        error: () => {
          this.toastService.error(this.isEdit ? 'Échec de la mise à jour.' : 'Échec de la création utilisateur.');
        }
      });
  }

  cancel(): void {
    void this.router.navigate(toCommands(this.adminPaths.UsersList));
  }

  get title(): string {
    return this.isEdit ? 'Modifier un utilisateur' : 'Ajouter un utilisateur';
  }

  get submitLabel(): string {
    return this.isEdit ? 'Mettre à jour' : 'Créer';
  }
}
