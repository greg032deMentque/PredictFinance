import { Injectable } from '@angular/core';
import { CanActivate, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AppRoutes } from '../Routes/app.routes.constants';
import { AuthService } from '../services/AuthService.service';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  canActivate(): Observable<boolean | UrlTree> {
    return this.authService.ensureValidAccessToken().pipe(
      map((token) => token ? true : this.router.createUrlTree([AppRoutes.Login]))
    );
  }
}
