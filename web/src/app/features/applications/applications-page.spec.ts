import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { ShellActions } from '../../shell/shell-actions';
import { APPLICATIONS_API } from './applications-api';
import { ApplicationsPage } from './applications-page';
import { ApplicationsSummaryStore } from './applications-summary';
import { application, applicationItem, applicationPage } from './applications.testing';

const SUMMARY_URL = `${APPLICATIONS_API}/summary`;
const SEARCH_WAIT_MS = 350;

describe('ApplicationsPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'applications', component: ApplicationsPage }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        // Both come from the shell in the app.
        ApplicationsSummaryStore,
        ShellActions,
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/applications', ApplicationsPage);
    await render();
    http.expectOne(SUMMARY_URL).flush({ total: 8, noResponse: 2 });
  });

  afterEach(() => {
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
    http.verify();
  });

  function root(): HTMLElement {
    return harness.routeNativeElement as HTMLElement;
  }

  // Not whenStable(): it waits for open HTTP requests, and these tests answer them one by one.
  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  function listRequest(): TestRequest {
    return http.expectOne(
      (request) => request.url === APPLICATIONS_API && request.method === 'GET',
    );
  }

  async function respondToList(body: object = applicationPage()): Promise<TestRequest> {
    await render();
    const request = listRequest();
    request.flush(body);
    await render();
    return request;
  }

  function buttonIn(container: ParentNode, text: string): HTMLButtonElement {
    const button = Array.from(container.querySelectorAll('button')).find(
      (candidate) =>
        candidate.textContent?.trim() === text || candidate.getAttribute('aria-label') === text,
    );
    if (!button) throw new Error(`No button "${text}"`);
    return button;
  }

  function select(label: string): HTMLSelectElement {
    const field = Array.from(root().querySelectorAll('app-toolbar-field')).find((candidate) =>
      candidate.querySelector('label')?.textContent?.includes(label),
    );
    const element = field?.querySelector('select');
    if (!element) throw new Error(`No select "${label}"`);
    return element;
  }

  it('load_InFlight_ShowsLoadingState', async () => {
    await render();

    expect(root().textContent).toContain('Loading applications…');
    listRequest().flush(applicationPage());
  });

  it('load_WithApplications_ShowsRowsAndTheSummary', async () => {
    const request = await respondToList(
      applicationPage({
        items: [
          applicationItem(),
          applicationItem({
            id: 'app-2',
            company: 'Brightline',
            status: 'Applied',
            noResponseDays: 14,
          }),
        ],
      }),
    );

    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('pageSize')).toBe('20');
    expect(request.request.params.has('status')).toBe(false);
    expect(root().textContent).toContain('8 applications · 2 with no response');
    const rows = root().querySelectorAll('tbody tr');
    expect(rows).toHaveLength(2);
    expect(rows[0].textContent).toContain('Northwind Logistics');
    expect(rows[0].textContent).toContain('LinkedIn Easy Apply');
    expect(rows[0].textContent).toContain('03 Nov 2026');
    expect(rows[0].textContent).toContain('Interview');
    expect(rows[1].textContent).toContain('No response · 14 days');
  });

  it('load_NoApplications_ShowsTheEmptyState', async () => {
    await respondToList(applicationPage({ items: [] }));

    expect(root().textContent).toContain('No applications yet');
    expect(root().querySelector('app-pagination')).toBeNull();
  });

  it('load_ServerDown_ShowsErrorAndTryAgainReloads', async () => {
    await render();
    listRequest().flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(root().textContent).toContain('Couldn’t load applications');
    buttonIn(root(), 'Try again').click();
    await respondToList();
    expect(root().querySelectorAll('tbody tr')).toHaveLength(1);
  });

  it('filter_StatusAndChannel_RequestThemFromPageOne', async () => {
    await respondToList();

    select('Status').value = 'NoResponse';
    select('Status').dispatchEvent(new Event('change'));
    const byStatus = await respondToList();
    select('Channel').value = 'KariyerNet';
    select('Channel').dispatchEvent(new Event('change'));
    const byChannel = await respondToList();

    expect(byStatus.request.params.get('status')).toBe('NoResponse');
    expect(byChannel.request.params.get('status')).toBe('NoResponse');
    expect(byChannel.request.params.get('channel')).toBe('KariyerNet');
    expect(byChannel.request.params.get('page')).toBe('1');
  });

  it('filter_Search_WaitsForTypingToPauseAndTrims', async () => {
    await respondToList();
    const input = root().querySelector<HTMLInputElement>('input[type="search"]');
    if (!input) throw new Error('No search');

    input.value = '  north ';
    input.dispatchEvent(new Event('input'));
    await render();
    http.expectNone((request) => request.url === APPLICATIONS_API);
    await new Promise((resolve) => setTimeout(resolve, SEARCH_WAIT_MS));
    const request = await respondToList();

    expect(request.request.params.get('search')).toBe('north');
  });

  it('filter_NoMatches_OffersToClearTheFilters', async () => {
    await respondToList();
    select('Status').value = 'Offer';
    select('Status').dispatchEvent(new Event('change'));
    await respondToList(applicationPage({ items: [] }));

    expect(root().textContent).toContain('No matching applications');
    expect(root().textContent).not.toContain('No applications yet');
    buttonIn(root(), 'Clear filters').click();
    const cleared = await respondToList();
    expect(cleared.request.params.has('status')).toBe(false);
  });

  it('select_RowClicked_OpensTheDetailAndMarksTheRow', async () => {
    await respondToList();

    root().querySelector<HTMLElement>('tbody tr')?.click();
    await render();
    http.expectOne(`${APPLICATIONS_API}/app-1`).flush(application());
    await render();

    expect(TestBed.inject(Router).url).toBe('/applications?id=app-1');
    expect(root().querySelector('tbody tr')?.getAttribute('aria-current')).toBe('true');
    expect(root().querySelector('app-application-detail')?.textContent).toContain('Recruiter call');
  });

  it('select_EnterOnRow_OpensTheDetail', async () => {
    await respondToList();

    root()
      .querySelector('tbody tr')
      ?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    await render();
    http.expectOne(`${APPLICATIONS_API}/app-1`).flush(application());
    await render();

    expect(TestBed.inject(Router).url).toBe('/applications?id=app-1');
  });

  it('delete_FromTheDetail_RefreshesCountsAndListAndClosesTheDetail', async () => {
    await respondToList();
    root().querySelector<HTMLElement>('tbody tr')?.click();
    await render();
    http.expectOne(`${APPLICATIONS_API}/app-1`).flush(application());
    await render();

    buttonIn(root(), 'Delete').click();
    await render();
    buttonIn(document, 'Delete application').click();
    await render();
    http
      .expectOne(`${APPLICATIONS_API}/app-1`)
      .flush(null, { status: 204, statusText: 'No Content' });
    await render();

    http.expectOne(SUMMARY_URL).flush({ total: 7, noResponse: 2 });
    await respondToList(applicationPage({ items: [] }));
    expect(TestBed.inject(Router).url).toBe('/applications');
    expect(root().querySelector('app-application-detail')).toBeNull();
    expect(root().textContent).toContain('7 applications');
  });

  it('add_Created_RefreshesTheListAndOpensTheNewApplication', async () => {
    await respondToList(applicationPage({ items: [] }));
    buttonIn(root(), 'Add application').click();
    await render();
    const dialog = document.querySelector<HTMLElement>('app-add-application-dialog');
    if (!dialog) throw new Error('No dialog');
    for (const [label, value] of [
      ['Company', 'Kestrel Health'],
      ['Role', 'Junior .NET Developer'],
    ]) {
      const input = Array.from(dialog.querySelectorAll('label')).find((candidate) =>
        candidate.textContent?.trim().startsWith(label),
      )?.control;
      if (!(input instanceof HTMLInputElement)) throw new Error(`No ${label}`);
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }

    dialog.querySelector('form')?.dispatchEvent(new Event('submit'));
    await render();
    http
      .expectOne((request) => request.url === APPLICATIONS_API && request.method === 'POST')
      .flush(application({ id: 'new-id', company: 'Kestrel Health' }));
    await render();

    http.expectOne(SUMMARY_URL).flush({ total: 9, noResponse: 2 });
    await respondToList();
    http.expectOne(`${APPLICATIONS_API}/new-id`).flush(application({ id: 'new-id' }));
    await render();
    expect(TestBed.inject(Router).url).toBe('/applications?id=new-id');
    expect(document.querySelector('app-add-application-dialog')).toBeNull();
  });
});
