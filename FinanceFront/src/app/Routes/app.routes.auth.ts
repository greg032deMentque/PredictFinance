import { Routes } from '@angular/router';
import { LoginComponent } from '../components/login/login';
import { AppRoutes } from './app.routes.constants';

export const AUTH_ROUTES: Routes = [
  {
    path: AppRoutes.Login,
    component: LoginComponent,
    data: { publicShell: true }
  },
  { path: '', pathMatch: 'full', redirectTo: AppRoutes.Login }
];
