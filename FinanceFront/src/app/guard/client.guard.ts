import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { AdminPaths, AppRoutes } from '../Routes/app.routes.constants';
import { AuthService } from '../services/AuthService.service';
import { StorageService } from '../services/storage.service';

@Injectable({ providedIn: 'root' })
export class ClientGuard implements CanActivate {
  constructor(
    private readonly storageService: StorageService,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  canActivate(): boolean | UrlTree {
    const token = this.storageService.GetToken();
    if (!token || this.authService.isTokenExpired(token)) {
      return this.router.createUrlTree([AppRoutes.Login]);
    }

    if (this.authService.isAdmin(token)) {
      return this.router.createUrlTree([AdminPaths.Dashboard]);
    }

    return true;
  }
}
