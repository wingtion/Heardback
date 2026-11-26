import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { demoEndLabel, toDemoFailure } from './demo';

describe('demo', () => {
  it('toDemoFailure_DemoFull_KeepsWhenAPlaceFreesUp', () => {
    const error = new HttpErrorResponse({
      status: 429,
      error: { code: 'demo.full', resetsAt: '2026-11-27T14:30:00Z' },
    });

    expect(toDemoFailure(error)).toEqual({ kind: 'full', placeFreesAt: '2026-11-27T14:30:00Z' });
  });

  it('toDemoFailure_PlainRateLimit_RoundsTheWaitUpToMinutes', () => {
    const error = new HttpErrorResponse({
      status: 429,
      headers: new HttpHeaders({ 'Retry-After': '61' }),
    });

    expect(toDemoFailure(error)).toEqual({ kind: 'rate-limited', retryAfterMinutes: 2 });
  });

  it('toDemoFailure_AnythingElse_IsUnavailable', () => {
    expect(toDemoFailure(new HttpErrorResponse({ status: 404 }))).toEqual({ kind: 'unavailable' });
    expect(toDemoFailure(new Error('offline'))).toEqual({ kind: 'unavailable' });
  });

  it('demoEndLabel_LaterToday_SaysToday', () => {
    const now = new Date(2026, 10, 26, 9, 0);
    const endsAt = new Date(2026, 10, 26, 14, 30).toISOString();

    expect(demoEndLabel(endsAt, now)).toBe('at 14:30 today');
  });

  it('demoEndLabel_NextDay_SaysTomorrow', () => {
    const now = new Date(2026, 10, 26, 23, 0);
    const endsAt = new Date(2026, 10, 27, 8, 5).toISOString();

    expect(demoEndLabel(endsAt, now)).toBe('at 08:05 tomorrow');
  });

  it('demoEndLabel_LaterThanTomorrow_GivesTheDate', () => {
    const now = new Date(2026, 10, 26, 9, 0);
    const endsAt = new Date(2026, 10, 28, 9, 0).toISOString();

    expect(demoEndLabel(endsAt, now)).toBe('on 28 Nov at 09:00');
  });
});
