import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastService } from '@app/services/toastr.service';

type ApiClientErrorResponse = {
  statusCode?: number;
  traceId?: string;
  message?: string;
  title?: string;
  detail?: string | null;
  errors?: string[];
};

function extractTraceId(
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

function withTraceId(message: string, traceId: string): string {
  return traceId ? `${message} (ref: ${traceId})` : message;
}

function getGenericMessage(status: number): string {
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

function extractMessage(err: unknown): string {
  if (!(err instanceof HttpErrorResponse)) {
    return 'Une erreur est survenue.';
  }

  if (err.status === 0) {
    return 'Impossible de contacter le serveur. Verifie ta connexion ou l API.';
  }

  const body = err.error as ApiClientErrorResponse | string | null | undefined;
  const traceId = extractTraceId(err, body);

  if (typeof body === 'string' && body.trim().length > 0) {
    return withTraceId(body.trim(), traceId);
  }

  if (body && typeof body === 'object') {
    const apiMessage = (body.message ?? '').trim();
    const title = (body.title ?? '').trim();
    const errors = Array.isArray(body.errors)
      ? body.errors.filter((x) => !!x && x.trim().length > 0)
      : [];

    if (apiMessage.length > 0) {
      return withTraceId(apiMessage, traceId);
    }

    if (errors.length > 0) {
      const joined = errors.slice(0, 3).join(' | ');
      return withTraceId(joined, traceId);
    }

    if (title.length > 0) {
      return withTraceId(title, traceId);
    }
  }

  return withTraceId(getGenericMessage(err.status), traceId);
}

export const apiErrorInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const toastService = inject(ToastService);

  return next(req).pipe(
    catchError((err: unknown) => {
      const message = extractMessage(err);
      if (message) {
        toastService.error(message);
      }
      return throwError(() => err);
    })
  );
};
