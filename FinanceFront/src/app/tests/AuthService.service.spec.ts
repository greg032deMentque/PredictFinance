import { HttpClient } from '@angular/common/http';
import { Injector, runInInjectionContext } from '@angular/core';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { TokenResponse } from '../Models/token-response';
import { AuthService } from '../services/AuthService.service';
import { StorageService } from '../services/storage.service';
import { AuthStore } from '../core/auth.store';

describe('AuthService', () => {
  const createJwt = (payload: Record<string, unknown>) => {
    const encodedPayload = btoa(JSON.stringify(payload));
    return `header.${encodedPayload}.signature`;
  };

  const createSut = (overrides?: {
    token?: string;
    refreshToken?: string;
    postResult?: unknown;
    postError?: unknown;
  }) => {
    const storageService = {
      GetToken: jasmine.createSpy().and.returnValue(overrides?.token ?? 'access-token'),
      GetRefreshToken: jasmine.createSpy().and.returnValue(overrides?.refreshToken ?? 'refresh-token'),
      SetToken: jasmine.createSpy(),
      SetRefreshToken: jasmine.createSpy(),
      RemoveToken: jasmine.createSpy(),
      RemoveRefreshToken: jasmine.createSpy()
    };

    const http = {
      post: jasmine.createSpy().and.callFake(() => {
        if (overrides && 'postError' in overrides) {
          return throwError(() => overrides.postError);
        }

        return of(overrides?.postResult ?? {});
      })
    };

    const router = {
      navigate: jasmine.createSpy().and.returnValue(Promise.resolve(true))
    };

    const authStore = {
      clear: jasmine.createSpy(),
      syncFromStorage: jasmine.createSpy()
    };

    const injector = Injector.create({
      providers: [
        { provide: HttpClient, useValue: http },
        { provide: StorageService, useValue: storageService },
        { provide: Router, useValue: router },
        { provide: AuthStore, useValue: authStore }
      ]
    });

    return {
      storageService,
      http,
      router,
      authStore,
      service: runInInjectionContext(injector, () => new AuthService())
    };
  };

  it('logout should post logout, clear tokens and navigate to login', async () => {
    const { service, http, storageService, router, authStore } = createSut();

    service.logout();
    await Promise.resolve();

    expect(http.post).toHaveBeenCalledOnceWith(jasmine.stringMatching(/Account\/Logout$/), {
      Token: 'access-token',
      RefreshToken: 'refresh-token'
    });
    expect(storageService.RemoveToken).toHaveBeenCalledTimes(1);
    expect(storageService.RemoveRefreshToken).toHaveBeenCalledTimes(1);
    expect(authStore.clear).toHaveBeenCalledOnceWith(false);
    expect(router.navigate).toHaveBeenCalledOnceWith(['login']);
  });

  it('logout should still clear local session when logout endpoint fails', async () => {
    const { service, storageService, router, authStore } = createSut({ postError: new Error('network') });

    service.logout();
    await Promise.resolve();

    expect(storageService.RemoveToken).toHaveBeenCalledTimes(1);
    expect(storageService.RemoveRefreshToken).toHaveBeenCalledTimes(1);
    expect(authStore.clear).toHaveBeenCalledOnceWith(false);
    expect(router.navigate).toHaveBeenCalledOnceWith(['login']);
  });

  it('ensureValidAccessToken should return the current token when it is not expired', (done) => {
    const token = createJwt({ exp: Math.floor(Date.now() / 1000) + 3600 });
    const { service, http } = createSut({ token });

    service.ensureValidAccessToken().subscribe((result) => {
      expect(result).toBe(token);
      expect(http.post).not.toHaveBeenCalled();
      done();
    });
  });

  it('ensureValidAccessToken should refresh and return the new token when the token is expired', (done) => {
    const expiredToken = createJwt({ exp: Math.floor(Date.now() / 1000) - 10 });
    const refreshedToken = createJwt({ exp: Math.floor(Date.now() / 1000) + 7200, role: 'admin' });
    const { service, http, storageService } = createSut({
      token: expiredToken,
      refreshToken: 'valid-refresh-token',
      postResult: { Token: refreshedToken, RefreshToken: 'next-refresh-token' }
    });

    service.ensureValidAccessToken().subscribe((result) => {
      expect(result).toBe(refreshedToken);
      expect(http.post).toHaveBeenCalledOnceWith(jasmine.stringMatching(/Account\/Refresh$/), {
        Token: expiredToken,
        RefreshToken: 'valid-refresh-token'
      });
      expect(storageService.SetToken).toHaveBeenCalledOnceWith(refreshedToken);
      expect(storageService.SetRefreshToken).toHaveBeenCalledOnceWith('next-refresh-token');
      done();
    });
  });

  it('initSession should persist tokens, sync the store and schedule refresh', () => {
    const { service, storageService, authStore } = createSut();
    spyOn(service, 'scheduleTokenRefresh').and.stub();

    service.initSession({ Token: 'access-token', RefreshToken: 'refresh-token' } as never);

    expect(storageService.SetToken).toHaveBeenCalledOnceWith('access-token');
    expect(storageService.SetRefreshToken).toHaveBeenCalledOnceWith('refresh-token');
    expect(authStore.syncFromStorage).toHaveBeenCalledTimes(1);
    expect(service.scheduleTokenRefresh).toHaveBeenCalledOnceWith('access-token', 'refresh-token');
  });

  it('login should delegate to the API and initialize the session', (done) => {
    const tokenResponse = {
      Token: 'access-token',
      RefreshToken: 'refresh-token'
    } as TokenResponse;
    const { service, http } = createSut({
      postResult: tokenResponse
    });
    spyOn(service, 'initSession').and.callThrough();

    service.login({ Email: 'user@example.com', Password: 'Password1' } as never).subscribe((result) => {
      expect(result).toBeUndefined();
      expect(http.post).toHaveBeenCalledOnceWith(jasmine.stringMatching(/Account\/Login$/), {
        Email: 'user@example.com',
        Password: 'Password1'
      });
      expect(service.initSession).toHaveBeenCalledOnceWith(tokenResponse);
      done();
    });
  });

  it('getUserRolesFromToken should normalize a single role string', () => {
    const token = createJwt({ role: 'Admin' });
    const { service } = createSut();

    expect(service.getUserRolesFromToken(token)).toEqual(['Admin']);
    expect(service.isAdmin(token)).toBeTrue();
  });

  it('getUserRolesFromToken should normalize a role array and keep admin access explicit', () => {
    const token = createJwt({ roles: ['reader', 'Admin'] });
    const { service } = createSut();

    expect(service.getUserRolesFromToken(token)).toEqual(['reader', 'Admin']);
    expect(service.isAdmin(token)).toBeTrue();
  });
});
