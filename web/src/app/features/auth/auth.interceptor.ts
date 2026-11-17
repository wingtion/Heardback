import {
  HttpErrorResponse,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
  HttpStatusCode,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import { AUTH_API_PREFIX } from './auth-paths';
import { AuthService } from './auth.service';

/**
 * Adds the access token to JobTrail API requests (never to other hosts). On a 401 it refreshes the
 * session once and retries the request once; if the refresh fails the original error is passed on.
 * The /api/auth endpoints are left alone: they work from the cookie and must not trigger refreshes.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const apiBaseUrl = inject(API_BASE_URL);

  if (!isApiRequest(request.url, apiBaseUrl) || isAuthRequest(request.url, apiBaseUrl)) {
    return next(request);
  }

  return send(request, next, auth.accessToken()).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.Unauthorized) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        catchError(() => throwError(() => error)),
        switchMap(() => send(request, next, auth.accessToken())),
      );
    }),
  );
};

function send(request: HttpRequest<unknown>, next: HttpHandlerFn, token: string | null) {
  return next(
    token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request,
  );
}

function isApiRequest(url: string, apiBaseUrl: string): boolean {
  return url.startsWith(`${apiBaseUrl}/api/`);
}

function isAuthRequest(url: string, apiBaseUrl: string): boolean {
  return url.startsWith(`${apiBaseUrl}${AUTH_API_PREFIX}/`);
}
