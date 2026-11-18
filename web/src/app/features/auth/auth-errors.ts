import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { validationErrors } from '../../core/http-errors';

/** Stable error codes from the Identity API (ProblemDetails "code" extension). */
export const AUTH_ERROR_CODES = {
  refreshSuperseded: 'auth.refresh_superseded',
  invalidCredentials: 'auth.invalid_credentials',
  invalidInviteCode: 'auth.invalid_invite_code',
  emailTaken: 'auth.email_taken',
} as const;

/** Used when a 429 arrives without a readable Retry-After header. */
export const DEFAULT_RETRY_AFTER_SECONDS = 60;

/** Why a sign-in or registration request failed, in the terms the forms show to the user. */
export type AuthFailure =
  | { readonly kind: 'invalid-credentials' }
  | { readonly kind: 'rate-limited'; readonly retryAfterSeconds: number }
  | { readonly kind: 'fields'; readonly fieldErrors: Readonly<Record<string, string>> }
  | { readonly kind: 'unavailable' };

// Field the client shows each coded error on, and the copy from the design (docs/design.md §10).
const CODED_FIELD_ERRORS: Readonly<Record<string, { field: string; message: string }>> = {
  [AUTH_ERROR_CODES.invalidInviteCode]: {
    field: 'inviteCode',
    message: 'This invite code is invalid, expired or already used',
  },
  [AUTH_ERROR_CODES.emailTaken]: {
    field: 'email',
    message: 'An account with this email already exists',
  },
};

/** Another request (e.g. a second tab) refreshed first; the cookie already holds the newer token. */
export function isRefreshSuperseded(error: unknown): boolean {
  return (
    error instanceof HttpErrorResponse &&
    error.status === HttpStatusCode.Conflict &&
    errorCode(error) === AUTH_ERROR_CODES.refreshSuperseded
  );
}

export function isUnauthorized(error: unknown): boolean {
  return error instanceof HttpErrorResponse && error.status === HttpStatusCode.Unauthorized;
}

/** Maps an HTTP failure from login or register to what the form should show. */
export function toAuthFailure(error: unknown): AuthFailure {
  if (!(error instanceof HttpErrorResponse)) {
    return { kind: 'unavailable' };
  }

  const coded = CODED_FIELD_ERRORS[errorCode(error) ?? ''];
  if (coded) {
    return { kind: 'fields', fieldErrors: { [coded.field]: coded.message } };
  }

  switch (error.status) {
    case HttpStatusCode.Unauthorized:
      return { kind: 'invalid-credentials' };
    case HttpStatusCode.TooManyRequests:
      return { kind: 'rate-limited', retryAfterSeconds: retryAfterSeconds(error) };
    case HttpStatusCode.BadRequest: {
      const fieldErrors = validationErrors(error.error);
      return Object.keys(fieldErrors).length > 0
        ? { kind: 'fields', fieldErrors }
        : { kind: 'unavailable' };
    }
    default:
      return { kind: 'unavailable' };
  }
}

function errorCode(error: HttpErrorResponse): string | null {
  const body: unknown = error.error;
  return typeof body === 'object' &&
    body !== null &&
    'code' in body &&
    typeof body.code === 'string'
    ? body.code
    : null;
}

function retryAfterSeconds(error: HttpErrorResponse): number {
  const seconds = Number.parseInt(error.headers.get('Retry-After') ?? '', 10);
  return Number.isFinite(seconds) && seconds > 0 ? seconds : DEFAULT_RETRY_AFTER_SECONDS;
}
