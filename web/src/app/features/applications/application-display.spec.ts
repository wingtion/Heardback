import {
  applicationsSummary,
  daysAgo,
  localToday,
  noResponseLabel,
  statusDisplay,
} from './application-display';

describe('application display', () => {
  it('statusDisplay_NoResponseDays_ReplacesAppliedWithTheNoResponseLabel', () => {
    expect(statusDisplay('Applied', 14)).toEqual({
      label: 'No response · 14 days',
      tone: 'no-response',
      glyph: 'clock',
    });
    expect(statusDisplay('Applied', null)).toEqual({
      label: 'Applied',
      tone: 'applied',
      glyph: 'open',
    });
    expect(statusDisplay('Rejected', null)).toEqual({
      label: 'Rejected',
      tone: 'danger',
      glyph: 'cross',
    });
  });

  it('noResponseLabel_OneDay_UsesTheSingular', () => {
    expect(noResponseLabel(1)).toBe('No response · 1 day');
  });

  it('applicationsSummary_Counts_ReadsLikeTheDesign', () => {
    expect(applicationsSummary({ total: 8, noResponse: 2 })).toBe(
      '8 applications · 2 with no response',
    );
    expect(applicationsSummary({ total: 1, noResponse: 0 })).toBe(
      '1 application · 0 with no response',
    );
  });

  it('daysAgo_Timestamps_RoundsDownToWholeDays', () => {
    const now = new Date('2026-11-18T12:00:00Z');

    expect(daysAgo('2026-11-18T08:00:00Z', now)).toBe('today');
    expect(daysAgo('2026-11-17T08:00:00Z', now)).toBe('1 day ago');
    expect(daysAgo('2026-11-04T12:00:00Z', now)).toBe('14 days ago');
  });

  it('daysAgo_ClockBehindTheServer_NeverGoesNegative', () => {
    expect(daysAgo('2026-11-19T12:00:00Z', new Date('2026-11-18T12:00:00Z'))).toBe('today');
  });

  it('localToday_LocalDate_IsZeroPadded', () => {
    expect(localToday(new Date(2026, 0, 5, 23, 30))).toBe('2026-01-05');
  });
});
