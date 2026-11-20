import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { APPLICATIONS_API } from '../applications/applications-api';
import { PostingAnalysis, PostingAnalysisFields } from './ai.models';
import { TEST_USAGE } from './ai.testing';
import { PostingAnalysisPanel } from './posting-analysis-panel';

@Component({
  imports: [PostingAnalysisPanel],
  template: `<app-posting-analysis-panel applicationId="app-1" [version]="version()" />`,
})
class PanelHost {
  readonly version = signal(3);
}

const URL = `${APPLICATIONS_API}/app-1/posting-analysis`;

const FIELDS: PostingAnalysisFields = {
  company: 'Northwind',
  title: 'Senior Backend Engineer',
  seniority: 'Senior',
  workMode: 'Hybrid',
  skills: ['.NET', 'PostgreSQL'],
  location: null,
  analyzedAt: '2026-11-19T09:00:00Z',
};

const state = (
  value: PostingAnalysis['state'],
  analysis: PostingAnalysisFields | null = null,
): PostingAnalysis => ({ state: value, analysis, usage: TEST_USAGE });

describe('PostingAnalysisPanel', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  async function create(answer: PostingAnalysis): Promise<HTMLElement> {
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

  for (const hidden of ['Unavailable', 'NoPostingText'] as const) {
    it(`load_${hidden}_ShowsNothing`, async () => {
      const element = await create(state(hidden));

      expect(element.textContent?.trim()).toBe('');
    });
  }

  it('load_NotRequested_OffersTheAnalysisWithWhatIsLeft', async () => {
    const element = await create(state('NotRequested'));

    expect(element.textContent).toContain('Job summary');
    expect(button(element, 'Summarize job')).toBeTruthy();
    expect(element.textContent).toContain('28 of 30 left today');
  });

  it('request_Clicked_PostsAndShowsAnalyzing', async () => {
    const element = await create(state('NotRequested'));

    button(element, 'Summarize job').click();
    await render();
    const request = http.expectOne(URL);
    expect(request.request.method).toBe('POST');
    request.flush(state('Queued'));
    await render();

    expect(element.textContent).toContain('Summarizing the job…');
  });

  it('queued_AfterAFewSeconds_ChecksAgainUntilReady', async () => {
    const fixture = TestBed.createComponent(PanelHost);
    await render();
    vi.useFakeTimers();
    http.expectOne(URL).flush(state('Queued'));
    TestBed.tick();
    await vi.advanceTimersByTimeAsync(0);
    TestBed.tick();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('Summarizing the job…');

    await vi.advanceTimersByTimeAsync(2900);
    TestBed.tick();
    http.expectNone(URL);
    await vi.advanceTimersByTimeAsync(100);
    TestBed.tick();
    http.expectOne(URL).flush(state('Ready', FIELDS));
    vi.useRealTimers();
    await render();

    expect(element.textContent).toContain('Senior Backend Engineer');
  });

  it('load_Ready_ListsTheFieldsAndSkills', async () => {
    const element = await create(state('Ready', FIELDS));

    expect(element.textContent).toContain('Northwind');
    expect(element.textContent).toContain('Hybrid');
    expect(element.textContent).toContain('Not stated');
    const chips = Array.from(
      element.querySelectorAll('ul[aria-label="Skills the posting asks for"] app-skill-chip'),
      (chip) => chip.textContent?.trim(),
    );
    expect(chips).toEqual(['.NET', 'PostgreSQL']);
    expect(element.textContent).toContain('Summarized by AI from the job description');
  });

  it('load_WaitingForCapacity_SaysWhenItRuns', async () => {
    const element = await create(state('WaitingForCapacity'));

    expect(element.textContent).toContain('AI has reached its daily limit');
  });

  it('load_Failed_OffersTryAgain', async () => {
    const element = await create(state('Failed'));

    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Couldn’t summarize this job.',
    );
    button(element, 'Try again').click();
    await render();
    http.expectOne(URL).flush(state('Queued'));
  });

  it('request_DailyLimitUsed_ShowsTheLimitBanner', async () => {
    const element = await create(state('NotRequested'));

    button(element, 'Summarize job').click();
    await render();
    http
      .expectOne(URL)
      .flush(
        { code: 'applications.analysis_quota_exceeded', resetsAt: '2026-11-20T00:00:00Z' },
        { status: 429, statusText: 'Too Many Requests' },
      );
    await render();

    expect(element.textContent).toContain('You’ve used all 30 job summaries for today');
  });

  it('request_ServerDown_SaysSo', async () => {
    const element = await create(state('NotRequested'));

    button(element, 'Summarize job').click();
    await render();
    http.expectOne(URL).flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(element.textContent).toContain('Couldn’t start the summary');
  });

  it('load_ServerDown_OffersTryAgain', async () => {
    const fixture = TestBed.createComponent(PanelHost);
    await render();
    http.expectOne(URL).flush(null, { status: 500, statusText: 'Server Error' });
    await render();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('Couldn’t load the analysis.');
    button(element, 'Try again').click();
    await render();
    http.expectOne(URL).flush(state('NotRequested'));
  });
});
