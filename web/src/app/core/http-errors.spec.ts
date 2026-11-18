import { HttpErrorResponse } from '@angular/common/http';
import { httpErrorOf, isForbidden, problemCode, validationErrors } from './http-errors';

describe('http errors', () => {
  const forbidden = new HttpErrorResponse({ status: 403 });

  it('httpErrorOf_WrappedInCause_ReturnsTheHttpError', () => {
    expect(httpErrorOf(new Error('wrapped', { cause: forbidden }))).toBe(forbidden);
  });

  it('isForbidden_OtherFailures_IsFalse', () => {
    expect(isForbidden(forbidden)).toBe(true);
    expect(isForbidden(new HttpErrorResponse({ status: 500 }))).toBe(false);
    expect(isForbidden(new Error('network'))).toBe(false);
    expect(isForbidden(undefined)).toBe(false);
  });

  it('validationErrors_ProblemBody_ReturnsFirstMessagePerField', () => {
    const body = { errors: { expiresInDays: ['Too long', 'Second'], other: 'not a list' } };

    expect(validationErrors(body)).toEqual({ expiresInDays: 'Too long' });
  });

  it('problemCode_ProblemBodyWithCode_ReturnsIt', () => {
    expect(problemCode({ code: 'applications.same_status', detail: 'x' })).toBe(
      'applications.same_status',
    );
  });

  it('problemCode_MissingOrNotText_IsNull', () => {
    expect(problemCode(null)).toBeNull();
    expect(problemCode({ detail: 'x' })).toBeNull();
    expect(problemCode({ code: 7 })).toBeNull();
  });
});
