import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TimelineStamp } from './timeline-stamp';

@Component({
  imports: [TimelineStamp],
  template: `<app-timeline-stamp date="2026-11-14" tone="interview" />`,
})
class Host {}

// Regression: the stamp passed "UTC" to DatePipe, which reads a date-only string as local midnight, so
// east of Greenwich (Türkiye is UTC+3) every stamp showed the day before.
describe('calendar stamps', () => {
  beforeEach(() => vi.stubEnv('TZ', 'Europe/Istanbul'));
  afterEach(() => vi.unstubAllEnvs());

  it('render_TimeZoneAheadOfUtc_ShowsTheCalendarDay', () => {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    const text = root.querySelector('app-timeline-stamp')?.textContent?.replace(/\s+/g, ' ').trim();
    expect(text).toBe('Nov 14');
  });
});
