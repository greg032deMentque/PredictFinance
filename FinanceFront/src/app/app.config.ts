import { ApplicationConfig, LOCALE_ID, provideZoneChangeDetection } from '@angular/core';
import { DatePipe, registerLocaleData } from '@angular/common';
import localeFr from '@angular/common/locales/fr';
import { provideRouter } from '@angular/router';
import {
  provideHttpClient,
  withInterceptors,
  withXsrfConfiguration
} from '@angular/common/http';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { JwtHelperService, JWT_OPTIONS } from '@auth0/angular-jwt';
import { provideToastr } from 'ngx-toastr';
import { routes } from './app.routes';
import { AuthService } from './services/AuthService.service';
import { apiErrorInterceptor, tokenInterceptor } from './interceptor';

registerLocaleData(localeFr, 'fr');

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideNoopAnimations(),
    provideHttpClient(
      withInterceptors([tokenInterceptor, apiErrorInterceptor]),
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
    { provide: LOCALE_ID, useValue: 'fr' },
    { provide: JWT_OPTIONS, useValue: JWT_OPTIONS },
    JwtHelperService,
    DatePipe,
    AuthService
  ]
};
