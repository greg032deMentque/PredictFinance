import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CurrentUserProfile {
  Email: string;
  FirstName: string;
  LastName: string;
  PhoneNumber: string;
}

export interface UpdateCurrentUserProfileRequest {
  FirstName: string;
  LastName: string;
  PhoneNumber: string;
}

export interface ChangePasswordRequest {
  CurrentPassword: string;
  NewPassword: string;
}

export interface ForgotPasswordRequest {
  Email: string;
}

export interface PublicSignupRequest {
  Email: string;
  Password: string;
  ConfirmPassword: string;
}

export interface PublicSignupResponse {
  Email: string;
  IsActive: boolean;
  CanLogin: boolean;
  RequiresEmailConfirmation: boolean;
}

export interface ConfirmEmailRequest {
  Email: string;
  Token: string;
}

export interface ResendConfirmationEmailRequest {
  Email: string;
}

export interface ResetPasswordRequest {
  Email: string;
  Token: string;
  Password: string;
  ConfirmPassword: string;
}

export interface UserConsents {
  AnalyticsConsent: boolean;
  MarketingEmailConsent: boolean;
  ProductImprovementConsent: boolean;
  LastUpdatedUtc: string | null;
}

export interface UpdateUserConsentsRequest {
  AnalyticsConsent: boolean;
  MarketingEmailConsent: boolean;
  ProductImprovementConsent: boolean;
}

export interface DeleteAccountRequest {
  CurrentPassword: string;
  ConfirmDeletion: boolean;
}

export interface AlertPreferences {
  AlertPatternStateChangeEnabled: boolean;
  AlertLevelCrossedEnabled: boolean;
  AlertDataStaleEnabled: boolean;
}

export interface UpdateAlertPreferencesRequest {
  AlertPatternStateChangeEnabled: boolean;
  AlertLevelCrossedEnabled: boolean;
  AlertDataStaleEnabled: boolean;
}

export interface DataExportResponse {
  Status: string;
  EstimatedDeliveryHours: number;
  Message: string;
}

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}Account/`;

  getProfile(): Observable<CurrentUserProfile> {
    return this.http.get<CurrentUserProfile>(`${this.baseUrl}Profile`);
  }

  updateProfile(request: UpdateCurrentUserProfileRequest): Observable<CurrentUserProfile> {
    return this.http.put<CurrentUserProfile>(`${this.baseUrl}Profile`, request);
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}ChangePassword`, request);
  }

  register(request: PublicSignupRequest): Observable<PublicSignupResponse> {
    return this.http.post<PublicSignupResponse>(`${this.baseUrl}Register`, request);
  }

  confirmEmail(request: ConfirmEmailRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}ConfirmEmail`, request);
  }

  resendConfirmationEmail(request: ResendConfirmationEmailRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}ResendConfirmationEmail`, request);
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}ForgotPassword`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}ResetPassword`, request);
  }

  getConsents(): Observable<UserConsents> {
    return this.http.get<UserConsents>(`${this.baseUrl}consents`);
  }

  updateConsents(request: UpdateUserConsentsRequest): Observable<UserConsents> {
    return this.http.patch<UserConsents>(`${this.baseUrl}consents`, request);
  }

  requestDataExport(): Observable<DataExportResponse> {
    return this.http.post<DataExportResponse>(`${this.baseUrl}data-export`, {});
  }

  deleteAccount(request: DeleteAccountRequest): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}self`, { body: request });
  }

  getAlertPreferences(): Observable<AlertPreferences> {
    return this.http.get<AlertPreferences>(`${this.baseUrl}alert-preferences`);
  }

  updateAlertPreferences(request: UpdateAlertPreferencesRequest): Observable<AlertPreferences> {
    return this.http.patch<AlertPreferences>(`${this.baseUrl}alert-preferences`, request);
  }
}
