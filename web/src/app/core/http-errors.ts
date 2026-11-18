import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

/**
 * The HttpErrorResponse behind a failure, also when it arrives wrapped (a signal resource wraps
 * values that are not Error instances and keeps the original as `cause`).
 */
export function httpErrorOf(error: unknown): HttpErrorResponse | null {
  if (error instanceof HttpErrorResponse) {
    return error;
  }
  const cause: unknown = error instanceof Error ? error.cause : null;
  return cause instanceof HttpErrorResponse ? cause : null;
}

/** The signed-in user is not allowed to do this, e.g. an admin page after admin rights were removed. */
export function isForbidden(error: unknown): boolean {
  return httpErrorOf(error)?.status === HttpStatusCode.Forbidden;
}

/** The `code` of a ProblemDetails body, e.g. "applications.already_tracked"; null when there is none. */
export function problemCode(body: unknown): string | null {
  if (typeof body !== 'object' || body === null || !('code' in body)) {
    return null;
  }
  return typeof body.code === 'string' ? body.code : null;
}

/** First message per field from a ValidationProblem body ({ errors: { field: [messages] } }). */
export function validationErrors(body: unknown): Record<string, string> {
  if (typeof body !== 'object' || body === null || !('errors' in body)) {
    return {};
  }

  const errors: unknown = body.errors;
  if (typeof errors !== 'object' || errors === null) {
    return {};
  }

  const result: Record<string, string> = {};
  for (const [field, messages] of Object.entries(errors)) {
    if (Array.isArray(messages) && typeof messages[0] === 'string') {
      result[field] = messages[0];
    }
  }
  return result;
}
