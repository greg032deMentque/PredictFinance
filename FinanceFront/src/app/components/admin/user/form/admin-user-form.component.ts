import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { User } from '../../../../Models/User';
import { UserRole } from '../../../../Models/user-role';
import { AdminPaths } from '../../../../Routes/app.routes.constants';
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
    password: ['', [Validators.minLength(6)]],
    roleId: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.loadRoles();

    const id = GeneralService.getRouteParamDeep('id', this.route);
    const normalizedId = (id ?? '').trim();
    if (!normalizedId) {
      this.isEdit = false;
      return;
    }

    this.isEdit = true;
    this.editedUserId = normalizedId;
    this.form.controls.password.clearValidators();
    this.form.controls.password.updateValueAndValidity();
    this.loadUser(normalizedId);
  }

  private loadRoles(): void {
    this.http.get<UserRole[]>(`${environment.apiUrl}User/GetUserRoles`).subscribe({
      next: (roles) => {
        this.roleOptions.splice(0, this.roleOptions.length, ...(roles ?? []));

        if (!this.isEdit && this.roleOptions.length > 0 && !this.form.controls.roleId.value) {
          this.form.controls.roleId.setValue(this.roleOptions[0].RoleId);
        }
      },
      error: () => {
        this.toastService.error('Impossible de charger la liste des roles.');
      }
    });
  }

  private loadUser(id: string): void {
    this.loading = true;

    this.http
      .get<User>(`${environment.apiUrl}User/GetUserById?userId=${encodeURIComponent(id)}`)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (user) => {
          this.form.patchValue({
            firstName: user.FirstName,
            lastName: user.LastName,
            email: user.Email,
            roleId: user.Roles?.[0]?.RoleId ?? ''
          });
        },
        error: () => {
          this.toastService.error('Utilisateur introuvable.');
          void this.router.navigate(['/', this.adminPaths.UsersList]);
        }
      });
  }

  submit(): void {
    if (this.form.invalid || this.submitting) {
      this.form.markAllAsTouched();
      return;
    }

    const payload = this.form.getRawValue();
    const selectedRole = this.roleOptions.find((role) => role.RoleId === payload.roleId);

    if (!selectedRole) {
      this.toastService.error('Role invalide.');
      return;
    }

    const user = new User();
    user.Id = this.editedUserId;
    user.FirstName = payload.firstName.trim();
    user.LastName = payload.lastName.trim();
    user.FullName = `${user.FirstName} ${user.LastName}`.trim();
    user.Email = payload.email.trim().toLowerCase();
    user.UserName = user.Email;
    user.Password = payload.password.trim();
    user.Roles = [selectedRole];

    this.submitting = true;

    if (this.isEdit) {
      this.http
        .put(`${environment.apiUrl}User/UpdateUser`, user)
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
      return;
    }

    const password = payload.password.trim();
    if (!password || password.length < 6) {
      this.submitting = false;
      this.form.controls.password.setErrors({ minlength: true });
      this.toastService.error('Le mot de passe est requis (6 caracteres min).');
      return;
    }

    this.http
      .post(`${environment.apiUrl}User/CreateUser`, user)
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

  cancel(): void {
    void this.router.navigate(['/', this.adminPaths.UsersList]);
  }

  get title(): string {
    return this.isEdit ? 'Modifier un utilisateur' : 'Ajouter un utilisateur';
  }

  get submitLabel(): string {
    return this.isEdit ? 'Mettre a jour' : 'Creer';
  }
}
