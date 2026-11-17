import { HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from '../../core/api-base-url';
import { isUnauthorized } from './auth-errors';
import { AUTH_API_PREFIX } from './auth-paths';
import { AuthService } from './auth.service';

/**
 * Adds the access token to JobTrail API requests (never to other hosts). On a 401 it refreshes the
 * session once and retries the request once; if the refresh fails the original error is passed on.
 * If the session was already refreshed while the request was in flight, it just retries with the
 * newer token instead of rotating the cookie again.
 * The /api/auth endpoints are left alone: they work from the cookie and must not trigger refreshes.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const apiBaseUrl = inject(API_BASE_URL);

  if (!isApiRequest(request.url, apiBaseUrl) || isAuthRequest(request.url, apiBaseUrl)) {
    return next(request);
  }

  const sentToken = auth.accessToken();
  return send(request, next, sentToken).pipe(
    catchError((error: unknown) => {
      if (!isUnauthorized(error)) {
        return throwError(() => error);
      }

      const currentToken = auth.accessToken();
      if (currentToken !== null && currentToken !== sentToken) {
        return send(request, next, currentToken);
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
