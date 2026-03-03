import { Routes } from '@angular/router';
import { AppRoutes } from './Routes/app.routes.constants';
import { ADMIN_ROUTES } from './Routes/app.routes.admin';
import { AUTH_ROUTES } from './Routes/app.routes.auth';
import { USER_ROUTES } from './Routes/app.routes.user';

export const routes: Routes = [
  ...AUTH_ROUTES,
  ...ADMIN_ROUTES,
  ...USER_ROUTES,
  { path: '**', redirectTo: AppRoutes.Login }
];
