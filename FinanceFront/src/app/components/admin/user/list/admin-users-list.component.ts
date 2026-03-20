import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import type { SweetAlertResult } from 'sweetalert2';
import Swal from 'sweetalert2/dist/sweetalert2.esm.all.js';
import { User } from '../../../../Models/User';
import { PaginateInterface } from '../../../../Models/Paginate/paginate-interface';
import { PaginateSettings } from '../../../../Models/Paginate/paginate-settings';
import { AppRoutes } from '../../../../Routes/app.routes.constants';
import { ToastService } from '../../../../services/toastr.service';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-admin-users-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-users-list.component.html',
  styleUrl: './admin-users-list.component.scss'
})
export class AdminUsersListComponent {
  readonly users = signal<User[]>([]);
  readonly total = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly filter = signal('');
  readonly sortActive = signal('LastName');
  readonly sortDescending = signal(false);
  readonly loading = signal(false);

  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  readonly maxPageIndex = computed(() => Math.max(0, this.pageCount() - 1));
  readonly currentPageLabel = computed(() => `${this.pageIndex() + 1} / ${this.pageCount()}`);
  readonly canAdd = computed(() => !this.loading());

  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly activeRoute = inject(ActivatedRoute);
  private readonly toastr = inject(ToastService);
  private readonly http = inject(HttpClient);

  ngOnInit(): void {
    this.loadUsers();
  }

  setFilter(value: string): void {
    this.pageIndex.set(0);
    this.filter.set(value.trim());
    this.loadUsers();
  }

  toggleSort(column: string): void {
    if (this.sortActive() === column) {
      this.sortDescending.set(!this.sortDescending());
    } else {
      this.sortActive.set(column);
      this.sortDescending.set(false);
    }

    this.pageIndex.set(0);
    this.loadUsers();
  }

  sortIcon(column: string): string {
    if (this.sortActive() !== column) return 'bi-arrow-down-up';
    return this.sortDescending() ? 'bi-sort-down' : 'bi-sort-up';
  }

  prev(): void {
    if (this.pageIndex() <= 0) return;
    this.pageIndex.set(this.pageIndex() - 1);
    this.loadUsers();
  }

  next(): void {
    if (this.pageIndex() >= this.maxPageIndex()) return;
    this.pageIndex.set(this.pageIndex() + 1);
    this.loadUsers();
  }

  goToCreate(): void {
    if (this.loading()) return;
    void this.router.navigate([AppRoutes.Add], { relativeTo: this.activeRoute });
  }

  goToEdit(userId: string): void {
    const id = (userId ?? '').trim();
    if (!id || this.loading()) return;
    void this.router.navigate([AppRoutes.Edit, id], { relativeTo: this.activeRoute });
  }

  loadUsers(): void {
    const payload: PaginateSettings = {
      PageIndex: this.pageIndex(),
      PageSize: this.pageSize(),
      Filter: this.filter(),
      SortActive: this.sortActive(),
      SortDirection: this.sortDescending()
    };

    this.loading.set(true);

    this.http
      .post<PaginateInterface<User>>(`${environment.apiUrl}User/GetUsersList`, payload)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: (response) => {
          this.users.set(response.Items ?? []);
          this.total.set(response.Total ?? 0);
        },
        error: () => {
          this.users.set([]);
          this.total.set(0);
          this.toastr.error('Impossible de charger la liste des utilisateurs.');
        }
      });
  }

  deleteUser(userId: string): void {
    const normalizedId = (userId ?? '').trim();
    if (!normalizedId || this.loading()) return;

    void Swal.fire({
      title: 'Etes-vous sÃ»r ?',
      text: "L'utilisateur sera definitivement supprimÃ©.",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Oui',
      cancelButtonText: 'Non',
      reverseButtons: true
    }).then((result: SweetAlertResult<unknown>) => {
      if (!result.isConfirmed || this.loading()) return;

      this.loading.set(true);

      this.http
        .delete(`${environment.apiUrl}User/DeleteUser?userId=${encodeURIComponent(normalizedId)}`)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.loading.set(false))
        )
        .subscribe({
          next: async () => {
            await Swal.fire('SupprimÃ©', "L'utilisateur a bien Ã©tÃ© supprimÃ©.", 'success');

            const isLastItemOnPage = this.users().length === 1 && this.pageIndex() > 0;
            if (isLastItemOnPage) {
              this.pageIndex.set(this.pageIndex() - 1);
            }

            this.loadUsers();
          },
          error: (err: HttpErrorResponse) => {
            this.toastr.error((err.error as string) || 'Suppression impossible.');
          }
        });
    });
  }
}
