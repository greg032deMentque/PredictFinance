import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/AuthService.service';
import { StorageService } from '../services/storage.service';

@Injectable({ providedIn: 'root' })
export class GuestGuard implements CanActivate {
  constructor(
    private readonly storageService: StorageService,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  canActivate(): boolean | UrlTree {
    const token = this.storageService.GetToken();
    if (token && !this.authService.isTokenExpired(token)) {
      return this.router.createUrlTree([this.authService.getDefaultRouteByRole(token)]);
    }

    return true;
  }
}
