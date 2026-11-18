import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Postmark } from './postmark';
import { TimelineStamp } from './timeline-stamp';

@Component({
  imports: [Postmark, TimelineStamp],
  template: `
    <app-postmark date="2026-11-14" />
    <app-timeline-stamp date="2026-11-14" tone="interview" />
  `,
})
class Host {}

// Regression: the stamps passed "UTC" to DatePipe, which reads a date-only string as local midnight, so
// east of Greenwich (Türkiye is UTC+3) every stamp showed the day before.
describe('calendar stamps', () => {
  beforeEach(() => vi.stubEnv('TZ', 'Europe/Istanbul'));
  afterEach(() => vi.unstubAllEnvs());

  it('render_TimeZoneAheadOfUtc_ShowsTheCalendarDay', () => {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    for (const selector of ['app-postmark', 'app-timeline-stamp']) {
      const text = root.querySelector(selector)?.textContent?.replace(/\s+/g, ' ').trim();
      expect(text).toBe('Nov 14');
    }
  });
});
