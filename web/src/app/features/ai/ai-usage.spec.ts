import { HttpErrorResponse } from '@angular/common/http';
import { quotaResetOf, resetTime, usageLine } from './ai-usage';
import { TEST_USAGE } from './ai.testing';

describe('ai-usage', () => {
  it('usageLine_Usage_SaysWhatIsLeftAndWhenItResets', () => {
    expect(usageLine(TEST_USAGE)).toBe(
      `28 of 30 left today · resets ${resetTime(TEST_USAGE.resetsAt)}`,
    );
  });

  it('resetTime_Iso_IsHoursAndMinutes', () => {
    expect(resetTime('2026-11-20T00:00:00Z')).toMatch(/^\d{2}:\d{2}$/);
  });

  it('quotaResetOf_429WithResetsAt_ReturnsIt', () => {
    const error = new HttpErrorResponse({
      status: 429,
      error: { code: 'matching.tailoring_quota_exceeded', resetsAt: '2026-11-20T00:00:00Z' },
    });

    expect(quotaResetOf(error)).toBe('2026-11-20T00:00:00Z');
  });

  it('quotaResetOf_OtherFailures_IsNull', () => {
    expect(quotaResetOf(new HttpErrorResponse({ status: 500 }))).toBeNull();
    expect(quotaResetOf(new HttpErrorResponse({ status: 429, error: {} }))).toBeNull();
    expect(quotaResetOf(new Error('offline'))).toBeNull();
  });
});
