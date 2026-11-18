import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { DEFAULT_RETRY_AFTER_SECONDS, toAuthFailure } from './auth-errors';

function httpError(status: number, body: unknown = null, headers?: Record<string, string>) {
  return new HttpErrorResponse({ status, error: body, headers: new HttpHeaders(headers) });
}

describe('toAuthFailure', () => {
  it('toAuthFailure_Unauthorized_ReturnsInvalidCredentials', () => {
    const failure = toAuthFailure(httpError(401, { code: 'auth.invalid_credentials' }));

    expect(failure).toEqual({ kind: 'invalid-credentials' });
  });

  it('toAuthFailure_TooManyRequests_ReadsRetryAfterHeader', () => {
    const failure = toAuthFailure(httpError(429, null, { 'Retry-After': '42' }));

    expect(failure).toEqual({ kind: 'rate-limited', retryAfterSeconds: 42 });
  });

  it('toAuthFailure_TooManyRequestsWithoutHeader_UsesDefaultWait', () => {
    const failure = toAuthFailure(httpError(429));

    expect(failure).toEqual({
      kind: 'rate-limited',
      retryAfterSeconds: DEFAULT_RETRY_AFTER_SECONDS,
    });
  });

  it('toAuthFailure_InvalidInviteCode_PutsDesignCopyOnInviteField', () => {
    const failure = toAuthFailure(
      httpError(400, {
        code: 'auth.invalid_invite_code',
        errors: { inviteCode: ['This invite code is invalid, expired or already used.'] },
      }),
    );

    expect(failure).toEqual({
      kind: 'fields',
      fieldErrors: { inviteCode: 'This invite code is invalid, expired or already used' },
    });
  });

  it('toAuthFailure_EmailTaken_PutsErrorOnEmailField', () => {
    const failure = toAuthFailure(httpError(409, { code: 'auth.email_taken' }));

    expect(failure).toEqual({
      kind: 'fields',
      fieldErrors: { email: 'An account with this email already exists' },
    });
  });

  it('toAuthFailure_ValidationProblem_ReturnsFirstMessagePerField', () => {
    const failure = toAuthFailure(
      httpError(400, { errors: { password: ['Too short.', 'Other.'], email: ['Bad email.'] } }),
    );

    expect(failure).toEqual({
      kind: 'fields',
      fieldErrors: { password: 'Too short.', email: 'Bad email.' },
    });
  });

  it('toAuthFailure_BadRequestWithoutFieldErrors_ReturnsUnavailable', () => {
    expect(toAuthFailure(httpError(400, { title: 'Bad request' }))).toEqual({
      kind: 'unavailable',
    });
  });

  it('toAuthFailure_NetworkOrServerError_ReturnsUnavailable', () => {
    expect(toAuthFailure(httpError(0))).toEqual({ kind: 'unavailable' });
    expect(toAuthFailure(httpError(500))).toEqual({ kind: 'unavailable' });
    expect(toAuthFailure(new Error('boom'))).toEqual({ kind: 'unavailable' });
  });
});
