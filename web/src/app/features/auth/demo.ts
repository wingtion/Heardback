import { HttpStatusCode } from '@angular/common/http';
import { clockTime } from '../../core/clock-time';
import { httpErrorOf } from '../../core/http-errors';
import { retryAfterSeconds } from './auth-errors';

/** Stable error code of a full demo (ADR-0018). */
export const DEMO_FULL_CODE = 'demo.full';

const SECONDS_PER_MINUTE = 60;
const MS_PER_DAY = 86_400_000;

/** Why "Try the demo" failed, in the terms the sign-in page shows. */
export type DemoFailure =
  | { readonly kind: 'full'; readonly placeFreesAt: string | null }
  | { readonly kind: 'rate-limited'; readonly retryAfterMinutes: number }
  | { readonly kind: 'unavailable' };

/**
 * A 429 is either the demo being full (code demo.full, with the time the oldest demo ends) or this
 * IP having started its share of demos (the plain rate limit, with Retry-After).
 */
export function toDemoFailure(error: unknown): DemoFailure {
  const response = httpErrorOf(error);
  if (response?.status !== HttpStatusCode.TooManyRequests) {
    return { kind: 'unavailable' };
  }

  const body: unknown = response.error;
  if (typeof body === 'object' && body !== null && 'code' in body && body.code === DEMO_FULL_CODE) {
    const placeFreesAt =
      'resetsAt' in body && typeof body.resetsAt === 'string' ? body.resetsAt : null;
    return { kind: 'full', placeFreesAt };
  }

  const minutes = Math.ceil(retryAfterSeconds(response) / SECONDS_PER_MINUTE);
  return { kind: 'rate-limited', retryAfterMinutes: Math.max(1, minutes) };
}

/**
 * When a demo ends, in words: "at 14:30 today", "at 14:30 tomorrow" or "on 28 Nov at 14:30", in the
 * user's own time zone.
 */
export function demoEndLabel(endsAt: string, now: Date = new Date()): string {
  const end = new Date(endsAt);
  const time = clockTime(endsAt);
  const days = Math.round((startOfDay(end) - startOfDay(now)) / MS_PER_DAY);
  if (days <= 0) {
    return `at ${time} today`;
  }
  if (days === 1) {
    return `at ${time} tomorrow`;
  }
  const date = new Intl.DateTimeFormat('en-GB', { day: 'numeric', month: 'short' }).format(end);
  return `on ${date} at ${time}`;
}

function startOfDay(date: Date): number {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
}
