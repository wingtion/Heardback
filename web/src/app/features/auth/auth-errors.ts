import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

/** Stable error codes from the Identity API (ProblemDetails "code" extension). */
export const AUTH_ERROR_CODES = {
  refreshSuperseded: 'auth.refresh_superseded',
} as const;

/** Another request (e.g. a second tab) refreshed first; the cookie already holds the newer token. */
export function isRefreshSuperseded(error: unknown): boolean {
  return (
    error instanceof HttpErrorResponse &&
    error.status === HttpStatusCode.Conflict &&
    hasErrorCode(error.error, AUTH_ERROR_CODES.refreshSuperseded)
  );
}

export function isUnauthorized(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === HttpStatusCode.Unauthorized;
}

function hasErrorCode(body: unknown, code: string): boolean {
  return typeof body === 'object' && body !== null && 'code' in body && body.code === code;
}
