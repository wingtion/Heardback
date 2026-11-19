import { HttpStatusCode } from '@angular/common/http';
import { httpErrorOf } from '../../core/http-errors';
import { AiUsage } from './ai.models';

/** "03:00": a reset time in the user's own time zone, 24-hour. */
export function resetTime(iso: string): string {
  return new Intl.DateTimeFormat(undefined, {
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23',
  }).format(new Date(iso));
}

/** "28 of 30 left today · resets 03:00". */
export function usageLine(usage: AiUsage): string {
  return `${usage.remaining} of ${usage.limit} left today · resets ${resetTime(usage.resetsAt)}`;
}

/** The reset time of a 429 "daily limit used" answer, or null when the failure was something else. */
export function quotaResetOf(error: unknown): string | null {
  const response = httpErrorOf(error);
  if (response?.status !== HttpStatusCode.TooManyRequests) {
    return null;
  }

  const body: unknown = response.error;
  if (
    typeof body === 'object' &&
    body !== null &&
    'resetsAt' in body &&
    typeof body.resetsAt === 'string'
  ) {
    return body.resetsAt;
  }
  return null;
}
