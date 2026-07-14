import { Routes } from '@angular/router';
import { AppRoutes } from './app.routes.constants';
import { publicOnlyGuard } from '../guard/public-only.guard';

export const AUTH_ROUTES: Routes = [
  {
    path: AppRoutes.Login,
    loadComponent: () => import('../components/login/login').then(m => m.LoginComponent),
    canActivate: [publicOnlyGuard],
    data: { publicShell: true }
  },
  {
    path: AppRoutes.Register,
    loadComponent: () => import('../components/auth/register/register.component').then(m => m.RegisterComponent),
    canActivate: [publicOnlyGuard],
    data: { publicShell: true }
  },
  {
    path: AppRoutes.ForgotPassword,
    loadComponent: () => import('../components/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent),
    canActivate: [publicOnlyGuard],
    data: { publicShell: true }
  },
  {
    path: AppRoutes.ResetPassword,
    loadComponent: () => import('../components/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
    canActivate: [publicOnlyGuard],
    data: { publicShell: true }
  },
  { path: '', pathMatch: 'full', redirectTo: AppRoutes.Login }
];
