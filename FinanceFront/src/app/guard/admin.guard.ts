import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { AppRoutes, UserPaths } from '../Routes/app.routes.constants';
import { AuthService } from '../services/AuthService.service';
import { StorageService } from '../services/storage.service';

@Injectable({ providedIn: 'root' })
export class AdminGuard implements CanActivate {
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
      return true;
    }

    return this.router.createUrlTree([UserPaths.Dashboard]);
  }
}
