import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class StorageService {
  private static readonly tokenKey = 'token';
  private static readonly refreshTokenKey = 'refreshToken';
  private static readonly userNameKey = 'userName';

  SetToken(token: string): void {
    sessionStorage.setItem(StorageService.tokenKey, token);
  }

  SetRefreshToken(refreshToken: string): void {
    sessionStorage.setItem(StorageService.refreshTokenKey, refreshToken);
  }

  SetUserName(userName: string): void {
    sessionStorage.setItem(StorageService.userNameKey, userName);
  }

  GetToken(): string {
    return sessionStorage.getItem(StorageService.tokenKey) ?? '';
  }

  GetRefreshToken(): string {
    return sessionStorage.getItem(StorageService.refreshTokenKey) ?? '';
  }

  GetUserName(): string {
    return sessionStorage.getItem(StorageService.userNameKey) ?? '';
  }

  RemoveToken(): void {
    sessionStorage.removeItem(StorageService.tokenKey);
  }

  RemoveRefreshToken(): void {
    sessionStorage.removeItem(StorageService.refreshTokenKey);
  }

  RemoveUserName(): void {
    sessionStorage.removeItem(StorageService.userNameKey);
  }

  ClearSession(): void {
    this.RemoveToken();
    this.RemoveRefreshToken();
    this.RemoveUserName();
  }
}
