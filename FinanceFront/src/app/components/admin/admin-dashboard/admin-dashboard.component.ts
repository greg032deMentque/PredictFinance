import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminPaths } from '../../../Routes/app.routes.constants';
import { PaginateInterface } from '../../../Models/Paginate/paginate-interface';
import { PaginateSettings } from '../../../Models/Paginate/paginate-settings';
import { User } from '../../../Models/User';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  readonly adminPaths = AdminPaths;
  readonly activeRateMock = 0.72;
  readonly activeUsersMockLabel = 'estimation (mock)';

  totalUsers = 0;
  activeUsersMock = 0;
  loading = false;
  error: string | null = null;
  private readonly http = inject(HttpClient);

  ngOnInit(): void {
    this.loadTotalUsers();
  }

  private loadTotalUsers(): void {
    const payload: PaginateSettings = {
      PageIndex: 0,
      PageSize: 1,
      Filter: '',
      SortActive: 'LastName',
      SortDirection: false
    };

    this.loading = true;
    this.error = null;

    this.http
      .post<PaginateInterface<User>>(`${environment.apiUrl}User/GetUsersList`, payload)
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (response) => {
          this.totalUsers = response.Total ?? 0;
          this.activeUsersMock = Math.round(this.totalUsers * this.activeRateMock);
        },
        error: () => {
          this.totalUsers = 0;
          this.activeUsersMock = 0;
          this.error = 'Impossible de charger les statistiques utilisateurs.';
        }
      });
  }
}
