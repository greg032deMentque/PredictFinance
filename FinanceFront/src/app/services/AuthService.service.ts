import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, lastValueFrom, Observable, of, Subscription, throwError, timer } from 'rxjs';
import { catchError, filter, finalize, switchMap, take, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { TokenResponse } from '../Models/token-response';
import { AdminPaths, AppRoutes, UserPaths } from '../Routes/app.routes.constants';
import { AuthStore } from '../core/auth.store';
import { StorageService } from './storage.service';


@Injectable({ providedIn: 'root' })
export class AuthService {
  private refreshTokenInProgress = false;
  private refreshTokenSubject = new BehaviorSubject<TokenResponse | null>(null);
  private refreshSchedulerSub?: Subscription;

  constructor(
    private http: HttpClient,
    private storageService: StorageService,
    private router: Router
  ) { }

  logout() {
    this.clearRefreshScheduler();
    this.storageService.RemoveToken();
    this.storageService.RemoveRefreshToken();

    void lastValueFrom(this.http.get(environment.apiUrl + 'Account/Logout')).catch(() => undefined);

    void this.router.navigate([AppRoutes.Login]);
  }

  isTokenExpired(token: string, marginSeconds = 60): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expiry = payload.exp;
      const now = Math.floor(Date.now() / 1000);
      return expiry <= now + marginSeconds;
    } catch (e) {
      return true;
    }
  }

  /** Décode l'expiration (en secondes) du token */
  getTokenExpiry(token: string): number | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp ?? null;
    } catch {
      return null;
    }
  }

  /** Lance un timer pour rafraîchir le token 5 minutes avant l'expiration */
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


  private decodeToken<T = any>(token: string): T | null {
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

    const payload = this.decodeToken<any>(rawToken);
    if (!payload) return [];

    const roles = payload.role ?? payload.roles;
    if (!roles) return [];

    return Array.isArray(roles) ? roles : [roles];
  }

  isSuperAdmin(token?: string): boolean {
    const roles = this.getUserRolesFromToken(token);
    return roles.some(r => r?.toLowerCase() === 'superadmin');
  }

  isAdmin(token?: string): boolean {
    const roles = this.getUserRolesFromToken(token).map(r => (r ?? '').toLowerCase());
    return roles.includes('superadmin') || roles.includes('admin');
  }



}
