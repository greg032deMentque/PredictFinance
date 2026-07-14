import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { AuthService } from '@app/services/AuthService.service';
import { StorageService } from '@app/services/storage.service';

function addAuthorization(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function isAccountEndpoint(url: string): boolean {
  return (
    url.includes('/Account/Login') ||
    url.includes('/Account/Register') ||
    url.includes('/Account/LoginAdmin') ||
    url.includes('/Account/ConfirmEmail') ||
    url.includes('/Account/ResendConfirmationEmail') ||
    url.includes('/Account/Refresh') ||
    url.includes('/Account/ForgotPassword') ||
    url.includes('/Account/ResetPassword') ||
    url.includes('/Account/Logout')
  );
}

export const tokenInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);
  const storageService = inject(StorageService);

  if (isAccountEndpoint(req.url)) {
    return next(req);
  }

  // Capture si une session est présente avant la tentative de refresh.
  // Si ensureValidAccessToken() retourne null alors qu'un token expiré existe en storage,
  // c'est une session expirée sans refresh token valide : on logout immédiatement
  // plutôt que de laisser partir une requête destinée à échouer en 401.
  const hadToken = !!storageService.GetToken();

  return authService.ensureValidAccessToken().pipe(
    switchMap((token) => {
      if (!token) {
        if (hadToken) {
          authService.logout();
          return throwError(
            () => new HttpErrorResponse({ status: 401, statusText: 'Session expirée' })
          );
        }
        return next(req);
      }
      return next(addAuthorization(req, token));
    })
  );
};
