import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastService } from '@app/services/toastr.service';

interface ApiClientErrorResponse {
  statusCode?: number;
  traceId?: string;
  message?: string;
  errors?: string[];
}

function getGenericMessage(status: number): string {
  switch (status) {
    case 400: return 'Requête invalide.';
    case 401: return 'Veuillez vous reconnecter.';
    case 403: return 'Action non autorisée.';
    case 409: return 'Conflit sur la ressource.';
    case 422: return 'Données invalides.';
    case 429: return 'Trop de requêtes. Patientez un instant.';
    case 504: return 'La requête a pris trop de temps. Réessayez plus tard.';
    default: return status >= 500 ? 'Service momentanément indisponible.' : 'Une erreur est survenue.';
  }
}

function readTraceId(err: HttpErrorResponse, body: ApiClientErrorResponse | unknown): string {
  if (body && typeof body === 'object') {
    const traceId = ((body as ApiClientErrorResponse).traceId ?? '').trim();
    if (traceId.length > 0) return traceId;
  }
  return (err.headers.get('X-Trace-Id') ?? '').trim();
}

export const apiErrorInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const toastService = inject(ToastService);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse)) {
        toastService.error('Une erreur est survenue.');
        return throwError(() => err);
      }

      if (err.status === 0) {
        if (err.error instanceof ProgressEvent && err.error.type === 'abort') {
          return throwError(() => err);
        }
        toastService.error('Impossible de contacter le serveur. Vérifie ta connexion.');
        return throwError(() => err);
      }

      if (err.status === 404) {
        const traceId = readTraceId(err, err.error);
        if (traceId) {
          console.error(`[404] ${req.url} — traceId: ${traceId}`);
        }
        return throwError(() => err);
      }

      const body = err.error as ApiClientErrorResponse | null | undefined;
      const traceId = readTraceId(err, body);

      if (traceId) {
        console.error(`[${err.status}] ${req.url} — traceId: ${traceId}`);
      }

      if (err.status === 400 || err.status === 422) {
        if (body && Array.isArray(body.errors)) {
          const validationErrors = body.errors.filter((x) => !!x && x.trim().length > 0);
          if (validationErrors.length > 0) {
            toastService.error(validationErrors[0].trim());
            return throwError(() => err);
          }
        }
        const customMessage = (body?.message ?? '').trim();
        if (customMessage.length > 0) {
          toastService.error(customMessage);
          return throwError(() => err);
        }
      }

      toastService.error(getGenericMessage(err.status));
      return throwError(() => err);
    })
  );
};
