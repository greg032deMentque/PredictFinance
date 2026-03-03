import { Routes } from '@angular/router';
import { ClientDashboardComponent } from '../components/client/client-dashboard/client-dashboard';
import { UserFinancePageComponent } from '../components/client/user-finance/user-finance-page/user-finance-page.component';
import { ClientLayoutComponent } from '../components/layout/client-layout.component';
import { AuthGuard, ClientGuard } from '../guard';
import { AppRoutes } from './app.routes.constants';

export const USER_ROUTES: Routes = [
  {
    path: AppRoutes.ClientRoot,
    component: ClientLayoutComponent,
    canActivate: [AuthGuard, ClientGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: AppRoutes.Dashboard },
      { path: AppRoutes.Dashboard, component: ClientDashboardComponent },
      { path: AppRoutes.Finance, component: UserFinancePageComponent }
    ]
  }
];
