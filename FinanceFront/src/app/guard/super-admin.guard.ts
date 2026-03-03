import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { AppRoutes } from '../Routes/app.routes.constants';
import { AuthService } from '../services/AuthService.service';

@Injectable({ providedIn: 'root' })
export class SuperAdminGuard implements CanActivate {
  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  canActivate(): boolean | UrlTree {
    if (this.authService.isSuperAdmin()) {
      return true;
    }

    return this.router.createUrlTree([AppRoutes.Login]);
  }
}
