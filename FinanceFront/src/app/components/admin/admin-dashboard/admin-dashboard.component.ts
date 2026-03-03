import { CommonModule, DatePipe, PercentPipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AdminActivity, AdminDashboardStats, AdminUser } from '../../../Models/admin-user';
import { AdminPaths } from '../../../Routes/app.routes.constants';
import { AdminUsersService } from '../../../services/admin-users.service';
import { AllModule } from '../../../module/allModule.module';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [AllModule, DatePipe, PercentPipe, RouterLink],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  readonly adminPaths = AdminPaths;

  stats = new AdminDashboardStats();
  recentUsers: AdminUser[] = [];
  recentActivities: AdminActivity[] = [];
  loading = false;

  constructor(private readonly adminUsersService: AdminUsersService) {}

  ngOnInit(): void {
    this.loading = true;

    this.adminUsersService
      .getDashboardStats()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (stats) => (this.stats = stats),
        error: () => {
          this.stats = new AdminDashboardStats();
        }
      });

    this.adminUsersService.getUsers(0, 6).subscribe({
      next: (users) => (this.recentUsers = users),
      error: () => {
        this.recentUsers = [];
      }
    });

    this.recentActivities = this.adminUsersService.getRecentActivities();
  }

  get activeUsersRate(): number {
    if (this.stats.totalUsers === 0) {
      return 0;
    }

    return this.stats.activeUsers / this.stats.totalUsers;
  }

  get adminCoverageRate(): number {
    if (this.stats.totalUsers === 0) {
      return 0;
    }

    return (this.stats.admins + this.stats.superAdmins) / this.stats.totalUsers;
  }
}
