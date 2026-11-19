import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APPLICATIONS_API } from '../applications/applications-api';
import { INSIGHTS_API } from './insights-api';
import { InsightsPage, daysText, ofText } from './insights-page';
import { Dashboard, FollowUpPage } from './insights.models';

const DASHBOARD_URL = `${INSIGHTS_API}/dashboard`;
const FOLLOW_UPS_URL = (url: string) => url.startsWith(`${INSIGHTS_API}/follow-ups`);

const weeks = Array.from({ length: 12 }, (_, index) => ({
  weekStart: `2026-${String(9 + Math.floor(index / 4)).padStart(2, '0')}-0${(index % 4) + 1}`,
  applications: index % 3,
}));

const DASHBOARD: Dashboard = {
  totalApplications: 12,
  responded: 5,
  responseRatePercent: 42,
  weeks,
  channels: [
    { channel: 'LinkedInEasyApply', applications: 9, responded: 2, responseRatePercent: 22 },
    { channel: 'Referral', applications: 3, responded: 3, responseRatePercent: 100 },
  ],
  responseTime: { responded: 5, averageDays: 6.5, medianDays: 4 },
  funnel: { applied: 12, interview: 4, offer: 1, rejected: 3 },
};

const EMPTY: Dashboard = {
  ...DASHBOARD,
  totalApplications: 0,
  responded: 0,
  responseRatePercent: null,
  channels: [],
  responseTime: { responded: 0, averageDays: null, medianDays: null },
  funnel: { applied: 0, interview: 0, offer: 0, rejected: 0 },
};

const FOLLOW_UPS: FollowUpPage = {
  items: [
    {
      applicationId: 'app-1',
      company: 'Contoso',
      role: 'Developer',
      channel: 'Other',
      appliedOn: '2026-10-30',
      lastActivityOn: '2026-10-30',
      noResponseDays: 20,
    },
  ],
  totalCount: 7,
};

describe('InsightsPage', () => {
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

  async function create(
    dashboard: Dashboard,
    followUps: FollowUpPage = FOLLOW_UPS,
  ): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(InsightsPage);
    await render();
    http.expectOne(DASHBOARD_URL).flush(dashboard);
    http.expectOne((request) => FOLLOW_UPS_URL(request.url)).flush(followUps);
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

  it('texts_Counts_ReadPlainly', () => {
    expect(ofText(5, 12)).toBe('5 of 12');
    expect(daysText(1)).toBe('1 day');
    expect(daysText(6.5)).toBe('6.5 days');
  });

  it('load_InFlight_SaysItIsWorking', async () => {
    const fixture = TestBed.createComponent(InsightsPage);
    await render();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Working out your numbers…',
    );
    http.expectOne(DASHBOARD_URL).flush(DASHBOARD);
    http.expectOne((request) => FOLLOW_UPS_URL(request.url)).flush(FOLLOW_UPS);
  });

  it('load_NoApplications_ShowsTheEmptyState', async () => {
    const element = await create(EMPTY, { items: [], totalCount: 0 });

    expect(element.textContent).toContain('No numbers yet');
    expect(element.querySelector('a')?.textContent).toContain('Go to Applications');
  });

  it('load_Numbers_ShowsRatesWithTheCountsBehindThem', async () => {
    const element = await create(DASHBOARD);

    expect(element.textContent).toContain('42%');
    expect(element.textContent).toContain('5 of 12 applications got a status update.');
    expect(element.textContent).toContain('6.5 days');
    expect(element.textContent).toContain('Median 4 days, from 5 responses.');
    expect(element.textContent).toContain('LinkedIn Easy Apply');
    expect(element.textContent).toContain('2 of 9 · 22%');
    expect(element.textContent).toContain('3 were rejected.');
  });

  it('load_NoResponsesYet_SaysSoInsteadOfATime', async () => {
    const element = await create({
      ...DASHBOARD,
      responded: 0,
      responseRatePercent: 0,
      responseTime: { responded: 0, averageDays: null, medianDays: null },
    });

    expect(element.textContent).toContain('No responses yet.');
  });

  it('followUps_ShowsTheListWithALinkToAll', async () => {
    const element = await create(DASHBOARD);

    const list = element.querySelector('ul[aria-label="Applications to follow up"]');
    expect(list?.textContent).toContain('Contoso');
    expect(list?.textContent).toContain('No response · 20 days');
    expect(element.textContent).toContain('7 waiting');
    const seeAll = Array.from(element.querySelectorAll('a')).find((link) =>
      link.textContent?.includes('See all 7'),
    );
    expect(seeAll?.getAttribute('href')).toBe('/applications?status=NoResponse');
  });

  it('followUps_NoneWaiting_SaysNothingIsWaiting', async () => {
    const element = await create(DASHBOARD, { items: [], totalCount: 0 });

    expect(element.textContent).toContain('Nothing waiting.');
  });

  it('markFollowedUp_Clicked_AddsTheNoteAndReloadsTheList', async () => {
    const element = await create(DASHBOARD);

    button(element, 'Mark followed up').click();
    await render();
    const request = http.expectOne(`${APPLICATIONS_API}/app-1/events`);
    expect(request.request.body).toEqual(
      expect.objectContaining({ status: null, note: 'Followed up' }),
    );
    request.flush({});
    await render();
    http.expectOne((next) => FOLLOW_UPS_URL(next.url)).flush({ items: [], totalCount: 6 });
    await render();

    expect(element.textContent).toContain('Nothing waiting.');
  });

  it('markFollowedUp_ServerDown_SaysSoOnThatRow', async () => {
    const element = await create(DASHBOARD);

    button(element, 'Mark followed up').click();
    await render();
    http
      .expectOne(`${APPLICATIONS_API}/app-1/events`)
      .flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(element.querySelector('li [role="alert"]')?.textContent).toContain('Couldn’t save.');
  });

  it('load_ServerDown_OffersTryAgain', async () => {
    const fixture = TestBed.createComponent(InsightsPage);
    await render();
    http.expectOne(DASHBOARD_URL).flush(null, { status: 500, statusText: 'Server Error' });
    http.expectOne((request) => FOLLOW_UPS_URL(request.url)).flush(FOLLOW_UPS);
    await render();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('Couldn’t load your numbers');
    button(element, 'Try again').click();
    await render();
    http.expectOne(DASHBOARD_URL).flush(DASHBOARD);
    http.expectOne((request) => FOLLOW_UPS_URL(request.url)).flush(FOLLOW_UPS);
  });
});
