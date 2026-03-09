import { Routes } from '@angular/router';
import { AdminDashboardComponent } from '../components/admin/admin-dashboard/admin-dashboard.component';
import { AdminUserFormComponent } from '../components/admin/user/form/admin-user-form.component';
import { AdminUsersListComponent } from '../components/admin/user/list/admin-users-list.component';
import { UserFinancePageComponent } from '../components/client/user-finance/user-finance-page/user-finance-page.component';
import { AdminGuard, AuthGuard } from '../guard';
import { AppRoutes } from './app.routes.constants';
import { AdminLayoutComponent } from '../components/admin/admin-layout-component/admin-layout-component';
import { AdminAnalyseFinance } from '../components/admin/admin-analyse-finance/admin-analyse-finance';

export const ADMIN_ROUTES: Routes = [
  {
    path: AppRoutes.AdminRoot,
    component: AdminLayoutComponent,
    canActivate: [AuthGuard, AdminGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: AppRoutes.Dashboard },
      { path: AppRoutes.Dashboard, component: AdminDashboardComponent },
      { path: AppRoutes.Analysis, component: AdminAnalyseFinance },
      { path: AppRoutes.Users, component: AdminUsersListComponent },
      { path: `${AppRoutes.Users}/${AppRoutes.Add}`, component: AdminUserFormComponent },
      { path: `${AppRoutes.Users}/${AppRoutes.Edit}/:id`, component: AdminUserFormComponent }
    ]
  }
];
