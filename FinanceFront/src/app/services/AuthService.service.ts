import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, lastValueFrom, Observable, of, Subscription, throwError, timer } from 'rxjs';
import { catchError, filter, finalize, map, switchMap, take, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { TokenResponse } from '../Models/token-response';
import { AppRoutes } from '../Routes/app.routes.constants';
import { StorageService } from './storage.service';
import { AuthStore } from '../core/auth.store';


@Injectable({ providedIn: 'root' })
export class AuthService {
  private refreshTokenInProgress = false;
  private refreshTokenSubject = new BehaviorSubject<TokenResponse | null>(null);
  private refreshSchedulerSub?: Subscription;

  private readonly http = inject(HttpClient);
  private readonly storageService = inject(StorageService);
  private readonly router = inject(Router);
  private readonly authStore = inject(AuthStore);

  login(model: { Email: string; Password: string }): Observable<void> {
    return this.http.post<TokenResponse>(environment.apiUrl + 'Account/Login', model).pipe(
      tap((token) => this.initSession(token)),
      map(() => undefined)
    );
  }

  initSession(token: TokenResponse): void {
    this.storageService.SetToken(token.Token);
    this.storageService.SetRefreshToken(token.RefreshToken);
    this.authStore.syncFromStorage();
    this.scheduleTokenRefresh(token.Token, token.RefreshToken);
  }

  logout() {
    const token = this.storageService.GetToken();
    const refreshToken = this.storageService.GetRefreshToken();

    this.clearRefreshScheduler();

    void lastValueFrom(this.http.post(environment.apiUrl + 'Account/Logout', {
      Token: token,
      RefreshToken: refreshToken
    })).catch(() => undefined);

    this.storageService.RemoveToken();
    this.storageService.RemoveRefreshToken();
    this.authStore.clear(false);

    void this.router.navigate([AppRoutes.Login]);
  }

  isTokenExpired(token: string, marginSeconds = 60): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expiry = payload.exp;
      const now = Math.floor(Date.now() / 1000);
      return expiry <= now + marginSeconds;
    } catch {
      return true;
    }
  }

  /** DÃ©code l'expiration (en secondes) du token */
  getTokenExpiry(token: string): number | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp ?? null;
    } catch {
      return null;
    }
  }

  /** Lance un timer pour rafraÃ®chir le token 5 minutes avant l'expiration */
  scheduleTokenRefresh(token: string, refreshToken: string) {
    const exp = this.getTokenExpiry(token);
    if (!exp) {
      return;
    }

    const nowMs = Date.now();
    const expMs = exp * 1000;

    const offsetMs = 5 * 60 * 1000;
    let delayMs = expMs - offsetMs - nowMs;

    if (delayMs < 0) {
      delayMs = 0;
    }

    this.clearRefreshScheduler();
    this.refreshSchedulerSub = timer(delayMs).pipe(
      switchMap(() => {
        return this.refreshToken(token, refreshToken);
      })
    ).subscribe({
      next: (res) => {
        this.scheduleTokenRefresh(res.Token, res.RefreshToken);
      },
      error: (err) => {
        console.error('[Scheduler] Échec du refresh automatique', err);
      },
    });
  }

  private clearRefreshScheduler() {
    if (this.refreshSchedulerSub) {
      this.refreshSchedulerSub.unsubscribe();
      this.refreshSchedulerSub = undefined;
    }
  }

  refreshToken(currentToken: string, refreshToken: string): Observable<TokenResponse> {
    if (this.refreshTokenInProgress) {
      return this.refreshTokenSubject.pipe(
        filter((res): res is TokenResponse => res !== null),
        take(1)
      );
    }

    this.refreshTokenInProgress = true;
    this.refreshTokenSubject.next(null);

    return this.http.post<TokenResponse>(environment.apiUrl + 'Account/Refresh', {
      Token: currentToken,
      RefreshToken: refreshToken,
    }).pipe(
      tap((res) => {
        this.storageService.SetToken(res.Token);
        this.storageService.SetRefreshToken(res.RefreshToken);
        this.refreshTokenSubject.next(res);
        this.scheduleTokenRefresh(res.Token, res.RefreshToken);
      }),
      catchError(err => {
        this.refreshTokenSubject.error(err);
        this.refreshTokenSubject = new BehaviorSubject<TokenResponse | null>(null);
        return throwError(() => err);
      }),
      finalize(() => {
        this.refreshTokenInProgress = false;
      })
    );
  }

  ensureValidAccessToken(): Observable<string | null> {
    const token = this.storageService.GetToken();
    if (!token) {
      return of(null);
    }

    if (!this.isTokenExpired(token)) {
      return of(token);
    }

    const refreshToken = this.storageService.GetRefreshToken();
    if (!refreshToken) {
      return of(null);
    }

    return this.refreshToken(token, refreshToken).pipe(
      map((response) => (response.Token ?? '').trim() || null),
      catchError(() => of(null))
    );
  }


  private decodeToken<T = Record<string, unknown>>(token: string): T | null {
    try {
      const payloadPart = token.split('.')[1];
      const base64 = payloadPart.replace(/-/g, '+').replace(/_/g, '/');
      const json = atob(base64);
      return JSON.parse(json) as T;
    } catch {
      return null;
    }
  }

  getUserRolesFromToken(token?: string): string[] {
    const rawToken = token ?? this.storageService.GetToken?.();
    if (!rawToken) return [];

    const payload = this.decodeToken<{ role?: string | string[]; roles?: string | string[] }>(rawToken);
    if (!payload) return [];

    const roles = payload.role ?? payload.roles;
    if (!roles) return [];

    return Array.isArray(roles) ? roles : [roles];
  }

  isAdmin(token?: string): boolean {
    const roles = this.getUserRolesFromToken(token).map(r => (r ?? '').toLowerCase());
    return roles.includes('admin');
  }
}
