import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastService } from '../services/toastr.service';

type ApiClientErrorResponse = {
  statusCode?: number;
  traceId?: string;
  message?: string;
  errors?: string[];
};

@Injectable()
export class ErrorHandlingInterceptor implements HttpInterceptor {
  constructor(private readonly toastService: ToastService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(req).pipe(
      catchError((err: unknown) => {
        const message = this.extractMessage(err);
        if (message) {
          this.toastService.error(message);
        }

        return throwError(() => err);
      })
    );
  }

  private extractMessage(err: unknown): string {
    if (!(err instanceof HttpErrorResponse)) {
      return 'Une erreur est survenue.';
    }

    if (err.status === 0) {
      return 'Impossible de contacter le serveur. Verifie ta connexion ou l API.';
    }

    const body = err.error as ApiClientErrorResponse | string | null | undefined;

    if (typeof body === 'string' && body.trim().length > 0) {
      return body.trim();
    }

    if (body && typeof body === 'object') {
      const apiMessage = (body.message ?? '').trim();
      const traceId = (body.traceId ?? '').trim();
      const errors = Array.isArray(body.errors) ? body.errors.filter((x) => !!x && x.trim().length > 0) : [];

      if (apiMessage.length > 0) {
        return traceId ? `${apiMessage} (ref: ${traceId})` : apiMessage;
      }

      if (errors.length > 0) {
        const joined = errors.slice(0, 3).join(' | ');
        return traceId ? `${joined} (ref: ${traceId})` : joined;
      }
    }

    const fallback = (err.message ?? '').trim();
    return fallback.length > 0 ? fallback : 'Une erreur est survenue.';
  }
}
