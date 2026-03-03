import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { AuthService } from '../services/AuthService.service';
import { StorageService } from '../services/storage.service';

@Injectable()
export class TokenInterceptor implements HttpInterceptor {
  constructor(
    private readonly authService: AuthService,
    private readonly storageService: StorageService
  ) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (this.isAccountEndpoint(req.url)) {
      return next.handle(req);
    }

    const token = this.storageService.GetToken();
    if (!token) {
      return next.handle(req);
    }

    if (!this.authService.isTokenExpired(token)) {
      return next.handle(this.addAuthorization(req, token));
    }

    const refreshToken = this.storageService.GetRefreshToken();
    if (!refreshToken) {
      this.authService.logout();
      return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Session expired' }));
    }

    return this.authService.refreshToken(refreshToken).pipe(
      switchMap((response) =>
        next.handle(this.addAuthorization(req, (response.Token ?? response.token ?? '').trim()))
      ),
      catchError((err) => {
        this.authService.logout();
        return throwError(() => err);
      })
    );
  }

  private addAuthorization(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
    return req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  private isAccountEndpoint(url: string): boolean {
    return (
      url.includes('/Account/Login') ||
      url.includes('/Account/LoginAdmin') ||
      url.includes('/Account/Refresh') ||
      url.includes('/Account/ForgotPassword') ||
      url.includes('/Account/ResetPassword') ||
      url.includes('/Account/Logout')
    );
  }
}
