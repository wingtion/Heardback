import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APPLICATIONS_API } from '../applications/applications-api';
import { CV_API } from '../cv/cv-api';
import { Tailoring, TailoringDetails } from './ai.models';
import { TEST_USAGE } from './ai.testing';
import { TailoringPanel } from './tailoring-panel';

@Component({
  imports: [TailoringPanel],
  template: `<app-tailoring-panel applicationId="app-1" [version]="version()" />`,
})
class PanelHost {
  readonly version = signal(3);
}

const URL = `${APPLICATIONS_API}/app-1/tailoring`;
const CONSENT_URL = `${CV_API}/ai-consent`;

const DETAILS: TailoringDetails = {
  summary: 'Strong .NET match; Kubernetes is missing.',
  suggestions: [
    {
      cvSection: 'Experience',
      cvQuote: 'Cut PostgreSQL query times by 60%',
      advice: 'Move this result to the top.',
    },
  ],
  isOutdated: false,
  createdAt: '2026-11-19T09:00:00Z',
};

const state = (
  value: Tailoring['state'],
  tailoring: TailoringDetails | null = null,
): Tailoring => ({ state: value, tailoring, missingKeywords: ['Kubernetes'], usage: TEST_USAGE });

describe('TailoringPanel', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  async function create(answer: Tailoring): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(PanelHost);
    await render();
    http.expectOne(URL).flush(answer);
    await render();
    return fixture.nativeElement as HTMLElement;
  }

  function button(element: HTMLElement, label: string): HTMLButtonElement {
    const found = Array.from(element.querySelectorAll('button')).find((candidate) =>
      candidate.textContent?.includes(label),
    );
    if (!found) {
      throw new Error(`No "${label}" button`);
    }
    return found;
  }

  for (const hidden of ['Unavailable', 'NoPostingText', 'NoCv'] as const) {
    it(`load_${hidden}_ShowsNothing`, async () => {
      const element = await create(state(hidden));

      expect(element.textContent?.trim()).toBe('');
    });
  }

  it('load_ConsentRequired_SaysWhatIsSentAndToWhom', async () => {
    const element = await create(state('ConsentRequired'));

    expect(element.textContent).toContain('sends your CV text');
    expect(element.textContent).toContain('Groq');
    expect(element.querySelector('a')?.textContent).toContain('CV page');
    expect(button(element, 'Allow and get suggestions')).toBeTruthy();
  });

  it('allowAndRequest_Clicked_RecordsConsentThenAsks', async () => {
    const element = await create(state('ConsentRequired'));

    button(element, 'Allow and get suggestions').click();
    await render();
    const consent = http.expectOne(CONSENT_URL);
    expect(consent.request.method).toBe('PUT');
    expect(consent.request.body).toEqual({ allowed: true });
    consent.flush({ allowed: true, allowedAt: '2026-11-19T09:00:00Z', available: true });
    await render();
    const request = http.expectOne(URL);
    expect(request.request.method).toBe('POST');
    request.flush(state('Queued'));
    await render();

    expect(element.textContent).toContain('Writing suggestions…');
  });

  it('load_NotRequested_OffersSuggestionsWithWhatIsLeft', async () => {
    const element = await create(state('NotRequested'));

    expect(button(element, 'Get suggestions')).toBeTruthy();
    expect(element.textContent).toContain('28 of 30 left today');
  });

  it('load_Ready_ShowsSummarySuggestionsAndTheAiNote', async () => {
    const element = await create(state('Ready', DETAILS));

    expect(element.textContent).toContain('Strong .NET match');
    const item = element.querySelector('ol[aria-label="Suggestions for your CV"] li');
    expect(item?.textContent).toContain('Experience');
    expect(item?.querySelector('q')?.textContent).toBe('Cut PostgreSQL query times by 60%');
    expect(item?.textContent).toContain('Move this result to the top.');
    expect(element.textContent).toContain('Check each suggestion before you use it.');
  });

  it('load_Outdated_OffersAnUpdate', async () => {
    const element = await create(state('Ready', { ...DETAILS, isOutdated: true }));

    expect(element.textContent).toContain('changed since these suggestions were written');
    button(element, 'Update suggestions').click();
    await render();
    http.expectOne(URL).flush(state('Queued', { ...DETAILS, isOutdated: true }));
    await render();

    expect(element.textContent).toContain('Writing suggestions…');
    expect(element.textContent).toContain('Strong .NET match');
    expect(element.textContent).not.toContain('Update suggestions');
  });

  it('load_Failed_OffersTryAgain', async () => {
    const element = await create(state('Failed'));

    expect(element.textContent).toContain('Couldn’t write suggestions.');
  });

  it('request_DailyLimitUsed_ShowsTheLimitBanner', async () => {
    const element = await create(state('NotRequested'));

    button(element, 'Get suggestions').click();
    await render();
    http
      .expectOne(URL)
      .flush(
        { code: 'matching.tailoring_quota_exceeded', resetsAt: '2026-11-20T00:00:00Z' },
        { status: 429, statusText: 'Too Many Requests' },
      );
    await render();
    http.expectOne(URL).flush(state('NotRequested'));
    await render();

    expect(element.textContent).toContain('You’ve used today’s suggestions');
  });
});
