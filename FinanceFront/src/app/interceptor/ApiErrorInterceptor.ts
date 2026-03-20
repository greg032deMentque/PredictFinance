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
  title?: string;
  detail?: string | null;
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
    const traceId = this.extractTraceId(err, body);

    if (typeof body === 'string' && body.trim().length > 0) {
      return this.withTraceId(body.trim(), traceId);
    }

    if (body && typeof body === 'object') {
      const apiMessage = (body.message ?? '').trim();
      const title = (body.title ?? '').trim();
      const errors = Array.isArray(body.errors) ? body.errors.filter((x) => !!x && x.trim().length > 0) : [];

      if (apiMessage.length > 0) {
        return this.withTraceId(apiMessage, traceId);
      }

      if (errors.length > 0) {
        const joined = errors.slice(0, 3).join(' | ');
        return this.withTraceId(joined, traceId);
      }

      if (title.length > 0) {
        return this.withTraceId(title, traceId);
      }
    }

    return this.withTraceId(this.getGenericMessage(err.status), traceId);
  }

  private extractTraceId(
    err: HttpErrorResponse,
    body: ApiClientErrorResponse | string | null | undefined
  ): string {
    if (body && typeof body === 'object') {
      const traceId = (body.traceId ?? '').trim();
      if (traceId.length > 0) {
        return traceId;
      }
    }

    return (err.headers.get('X-Trace-Id') ?? '').trim();
  }

  private withTraceId(message: string, traceId: string): string {
    return traceId ? `${message} (ref: ${traceId})` : message;
  }

  private getGenericMessage(status: number): string {
    switch (status) {
      case 400:
        return 'Requête invalide.';
      case 401:
        return 'Veuillez vous reconnecter.';
      case 403:
        return 'Action non autorisée.';
      case 404:
        return 'Ressource introuvable.';
      case 409:
        return 'Conflit sur la ressource.';
      case 422:
        return 'Données invalides.';
      case 429:
        return 'Trop de requêtes. Patientez un instant.';
      default:
        return status >= 500 ? 'Service momentanément indisponible.' : 'Une erreur est survenue.';
    }
  }
}
