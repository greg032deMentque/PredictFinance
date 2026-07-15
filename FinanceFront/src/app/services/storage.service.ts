import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class StorageService {
  private static readonly tokenKey = 'token';
  private static readonly refreshTokenKey = 'refreshToken';

  SetToken(token: string): void {
    sessionStorage.setItem(StorageService.tokenKey, token);
  }

  SetRefreshToken(refreshToken: string): void {
    sessionStorage.setItem(StorageService.refreshTokenKey, refreshToken);
  }

  GetToken(): string {
    return sessionStorage.getItem(StorageService.tokenKey) ?? '';
  }

  GetRefreshToken(): string {
    return sessionStorage.getItem(StorageService.refreshTokenKey) ?? '';
  }

  RemoveToken(): void {
    sessionStorage.removeItem(StorageService.tokenKey);
  }

  RemoveRefreshToken(): void {
    sessionStorage.removeItem(StorageService.refreshTokenKey);
  }

  ClearSession(): void {
    this.RemoveToken();
    this.RemoveRefreshToken();
  }
}
