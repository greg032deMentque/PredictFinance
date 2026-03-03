import { CommonModule, DatePipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminUser } from '../../../../Models/admin-user';
import { AdminPaths } from '../../../../Routes/app.routes.constants';
import { AdminUsersService } from '../../../../services/admin-users.service';
import { ToastService } from '../../../../services/toastr.service';

@Component({
  selector: 'app-admin-users-list',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe],
  templateUrl: './admin-users-list.component.html',
  styleUrl: './admin-users-list.component.scss'
})
export class AdminUsersListComponent implements OnInit {
  readonly adminPaths = AdminPaths;
  users: AdminUser[] = [];
  loading = false;
  deletingUserId = '';

  constructor(
    private readonly adminUsersService: AdminUsersService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.adminUsersService
      .getUsers()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (users) => (this.users = users),
        error: () => {
          this.users = [];
          this.toastService.error('Impossible de charger les utilisateurs.');
        }
      });
  }

  deleteUser(user: AdminUser): void {
    if (!user.id || this.deletingUserId.length > 0) {
      return;
    }

    const shouldDelete = window.confirm(`Supprimer l'utilisateur ${user.firstName} ${user.lastName} ?`);
    if (!shouldDelete) {
      return;
    }

    this.deletingUserId = user.id;
    this.adminUsersService
      .deleteUser(user.id)
      .pipe(finalize(() => (this.deletingUserId = '')))
      .subscribe({
        next: () => {
          this.users = this.users.filter((x) => x.id !== user.id);
          this.toastService.success('Utilisateur supprime.');
        },
        error: () => {
          this.toastService.error('Suppression impossible.');
        }
      });
  }
}
