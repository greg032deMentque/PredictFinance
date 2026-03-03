import { Routes } from '@angular/router';
import { LoginComponent } from '../components/login/login';
import { GuestGuard } from '../guard';
import { AppRoutes } from './app.routes.constants';

export const AUTH_ROUTES: Routes = [
  {
    path: AppRoutes.Login,
    component: LoginComponent,
    canActivate: [GuestGuard],
    data: { publicShell: true }
  },
  { path: '', pathMatch: 'full', redirectTo: AppRoutes.Login }
];
