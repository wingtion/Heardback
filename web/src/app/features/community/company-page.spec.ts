import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { COMMUNITY_API } from './community-api';
import { COMPANY_ROUTE } from './community-routes';
import { CompanyCommunity } from './community.models';
import { TEST_NUMBERS } from './community.testing';
import { CompanyPage } from './company-page';

const OWN_ROWS = 'table[aria-labelledby=own-applications-title] tbody tr';
const URL = `${COMMUNITY_API}/companies/brightline%20payments`;

const COMPANY: CompanyCommunity = {
  companyKey: 'brightline payments',
  company: 'Brightline Payments',
  numbers: TEST_NUMBERS,
  applications: [
    {
      applicationId: 'app-1',
      role: 'Software Engineer, Platform',
      status: 'Applied',
      appliedOn: '2026-11-10',
      noResponseDays: 14,
      updatedAt: '2026-11-10T09:00:00Z',
      ghost: { level: 'High', score: 4 },
    },
    {
      applicationId: 'app-2',
      role: 'Data Engineer',
      status: 'Rejected',
      appliedOn: '2026-09-02',
      noResponseDays: null,
      updatedAt: '2026-09-20T09:00:00Z',
      ghost: { level: 'Low', score: 1 },
    },
  ],
};

describe('CompanyPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: COMPANY_ROUTE, component: CompanyPage }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/applications/companies/brightline%20payments', CompanyPage);
    await render();
  });

  afterEach(() => http.verify());

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  function root(): HTMLElement {
    return harness.routeNativeElement!;
  }

  function text(): string {
    return root().textContent?.replace(/\s+/g, ' ') ?? '';
  }

  it('load_InFlight_ShowsLoading', () => {
    expect(root().querySelector('[role=status]')?.textContent).toContain('Loading company…');
    http.expectOne(URL).flush(COMPANY);
  });

  it('load_Loaded_ShowsYourApplicationsAndTheCompanyNumbers', async () => {
    http.expectOne(URL).flush(COMPANY);
    await render();

    expect(root().querySelector('h1')?.textContent).toContain('Brightline Payments');
    const rows = Array.from(root().querySelectorAll(OWN_ROWS));
    expect(rows.map((row) => row.querySelector('a')?.textContent?.trim())).toEqual([
      'Software Engineer, Platform',
      'Data Engineer',
    ]);
    expect(rows[0].querySelector('a')?.getAttribute('href')).toBe('/applications?id=app-1');
    expect(rows[0].textContent).toContain('No response · 14 days');
    expect(rows[0].querySelector('app-ghost-badge')?.textContent).toContain('High');
    // Low gets no badge.
    expect(rows[1].querySelector('app-ghost-badge')).toBeNull();
    expect(text()).toContain('Stats from other members who applied to this company');
    expect(text()).toContain('Your applications at Brightline Payments');
    expect(text()).toContain('Their postings and roles are never shown.');
  });

  it('backLink_OpenedFromAnApplication_OpensThatApplicationAgain', async () => {
    http.expectOne(URL).flush(COMPANY);
    await harness.navigateByUrl('/applications/companies/brightline%20payments?from=app-7');
    await render();

    const back = Array.from(root().querySelectorAll('a')).find((link) =>
      link.textContent?.includes('Back to job applications'),
    );
    expect(back?.getAttribute('href')).toBe('/applications?id=app-7');
  });

  it('backLink_OpenedDirectly_GoesToTheList', async () => {
    http.expectOne(URL).flush(COMPANY);
    await render();

    const back = Array.from(root().querySelectorAll('a')).find((link) =>
      link.textContent?.includes('Back to job applications'),
    );
    expect(back?.getAttribute('href')).toBe('/applications');
  });

  it('load_TooFewOtherMembers_StillShowsYourApplications', async () => {
    http.expectOne(URL).flush({ ...COMPANY, numbers: null });
    await render();

    expect(text()).toContain('Not enough data yet');
    expect(text()).toContain('Fewer than 3 other members have applied to this company.');
    expect(root().querySelectorAll(OWN_ROWS).length).toBe(2);
  });

  it('load_NotFound_SaysCompanyPagesOnlyOpenFromYourApplications', async () => {
    http.expectOne(URL).flush(null, { status: 404, statusText: 'Not Found' });
    await render();

    expect(root().querySelector('h1')?.textContent).toContain(
      'You have no applications at this company',
    );
    expect(text()).toContain('Go to job applications');
  });

  it('load_ServerDown_ShowsErrorAndTryAgainReloads', async () => {
    http.expectOne(URL).flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(text()).toContain('Couldn’t load this company');
    root().querySelector<HTMLButtonElement>('[role=alert] button')!.click();
    await render();
    http.expectOne(URL).flush(COMPANY);
  });
});
