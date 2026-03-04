import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AppRoutes } from '../Routes/app.routes.constants';
import { AuthService } from '../services/AuthService.service';
import { StorageService } from '../services/storage.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(
    private readonly storageService: StorageService,
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  canActivate(): boolean | UrlTree | Observable<boolean | UrlTree> {
    const token = this.storageService.GetToken();
    if (!token) {
      return this.router.createUrlTree([AppRoutes.Login]);
    }

    if (!this.authService.isTokenExpired(token)) {
      return true;
    }

    const refreshToken = this.storageService.GetRefreshToken();
    if (!refreshToken) {
      return this.router.createUrlTree([AppRoutes.Login]);
    }

    return this.authService.refreshToken(token, refreshToken).pipe(
      map(() => true),
      catchError(() => {
        return of(this.router.createUrlTree([AppRoutes.Login]));
      })
    );
  }
}
