import { ApplicationConfig, LOCALE_ID, provideZoneChangeDetection } from '@angular/core';
import { DatePipe, registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import { provideRouter } from '@angular/router';
import {
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withInterceptorsFromDi,
  withXsrfConfiguration
} from '@angular/common/http';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideToastr } from 'ngx-toastr';
import { routes } from './app.routes';
import { AuthService } from './services/AuthService.service';
import { ErrorHandlingInterceptor, TokenInterceptor } from './interceptor';

registerLocaleData(localeFr, 'fr');

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideNoopAnimations(),
    provideHttpClient(
      withInterceptorsFromDi(),
      withXsrfConfiguration({
        cookieName: 'XSRF-TOKEN',
        headerName: 'X-XSRF-TOKEN'
      })
    ),
    provideToastr({
      closeButton: false,
      positionClass: 'toast-bottom-right',
      timeOut: 5000,
      preventDuplicates: true
    }),
    { provide: HTTP_INTERCEPTORS, useClass: TokenInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorHandlingInterceptor, multi: true },
    { provide: LOCALE_ID, useValue: 'fr' },
    DatePipe,
    AuthService
  ]
};
