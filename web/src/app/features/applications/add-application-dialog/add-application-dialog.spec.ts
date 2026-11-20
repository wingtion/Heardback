import { DialogRef } from '@angular/cdk/dialog';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DialogLauncher } from '../../../shared/ui/dialog/dialog-launcher';
import { localToday } from '../application-display';
import { APPLICATIONS_API } from '../applications-api';
import { application } from '../applications.testing';
import { AddApplicationDialog } from './add-application-dialog';

const TRACKED_URL = `${APPLICATIONS_API}/tracked`;
const LOOKUP_WAIT_MS = 450;

describe('AddApplicationDialog', () => {
  let http: HttpTestingController;
  let ref: DialogRef<unknown, AddApplicationDialog>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    ref = TestBed.inject(DialogLauncher).open(AddApplicationDialog, { mobile: 'full-screen' });
    await render();
  });

  afterEach(() => {
    ref.close();
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
    http.verify();
  });

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  function dialog(): HTMLElement {
    const element = document.querySelector<HTMLElement>('app-add-application-dialog');
    if (!element) throw new Error('No dialog');
    return element;
  }

  function control(label: string): HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement {
    const labelElement = Array.from(dialog().querySelectorAll('label')).find((candidate) =>
      candidate.textContent?.trim().startsWith(label),
    );
    const element = labelElement?.htmlFor ? document.getElementById(labelElement.htmlFor) : null;
    if (
      !(element instanceof HTMLInputElement) &&
      !(element instanceof HTMLTextAreaElement) &&
      !(element instanceof HTMLSelectElement)
    ) {
      throw new Error(`No control "${label}"`);
    }
    return element;
  }

  function type(label: string, value: string): void {
    const element = control(label);
    element.value = value;
    element.dispatchEvent(new Event(element instanceof HTMLSelectElement ? 'change' : 'input'));
  }

  function button(text: string): HTMLButtonElement {
    const found = Array.from(dialog().querySelectorAll('button')).find(
      (candidate) => candidate.textContent?.trim() === text,
    );
    if (!found) throw new Error(`No button "${text}"`);
    return found;
  }

  async function submit(): Promise<void> {
    dialog().querySelector('form')?.dispatchEvent(new Event('submit'));
    await render();
  }

  function fillRequired(): void {
    type('Company', '  Northwind Logistics ');
    type('Role', 'Backend Engineer (.NET)');
  }

  // Initial focus on the link field needs real layout (CDK's focus trap skips invisible elements, and
  // jsdom has no layout); it is checked in the browser instead.
  it('open_Default_StartsWithTodayAndLinkedIn', () => {
    expect(control('Applied date').value).toBe(localToday(new Date()));
    expect(control('Channel').value).toBe('LinkedInEasyApply');
  });

  it('submit_Empty_ShowsTheDesignedFieldErrorsAndSendsNothing', async () => {
    await submit();

    expect(dialog().textContent).toContain('Enter the company name');
    expect(dialog().textContent).toContain('Enter the role');
    expect(control('Company').getAttribute('aria-invalid')).toBe('true');
    expect(document.activeElement).toBe(control('Company'));
    http.expectNone(APPLICATIONS_API);
  });

  it('submit_InvalidLinkAndFutureDate_ShowsTheirErrors', async () => {
    fillRequired();
    type('Job posting link', 'northwind-logistics/jobs');
    type('Applied date', '2999-01-01');

    await submit();

    expect(dialog().textContent).toContain('Enter a full link that starts with https://');
    expect(dialog().textContent).toContain('Pick today or an earlier date');
  });

  it('submit_DateBefore2000_ShowsTheEarliestDateError', async () => {
    fillRequired();
    type('Applied date', '1999-12-31');

    await submit();

    expect(dialog().textContent).toContain('Pick a date after 1 January 2000');
  });

  it('submit_Valid_PostsTheTrimmedDetailsEmitsCreatedAndCloses', async () => {
    let created: string | null = null;
    ref.componentInstance?.created.subscribe((value) => (created = value.id));
    fillRequired();
    type('Job description', '  Shipment tracking APIs. ');
    type('Channel', 'Referral');

    await submit();

    const request = http.expectOne(APPLICATIONS_API);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      company: 'Northwind Logistics',
      role: 'Backend Engineer (.NET)',
      url: null,
      postingText: 'Shipment tracking APIs.',
      channel: 'Referral',
      appliedOn: localToday(new Date()),
    });
    expect(button('Adding…').disabled).toBe(true);
    request.flush(application({ id: 'new-id' }));
    await render();

    expect(created).toBe('new-id');
    expect(document.querySelector('app-add-application-dialog')).toBeNull();
  });

  it('submit_ServerFieldError_ShowsItOnTheField', async () => {
    fillRequired();

    await submit();
    http
      .expectOne(APPLICATIONS_API)
      .flush({ errors: { role: ['Enter the role'] } }, { status: 400, statusText: 'Bad Request' });
    await render();

    expect(control('Role').getAttribute('aria-invalid')).toBe('true');
    expect(dialog().textContent).toContain('Enter the role');
  });

  it('submit_ServerDown_ShowsTheBannerAndKeepsTheDetails', async () => {
    fillRequired();

    await submit();
    http.expectOne(APPLICATIONS_API).flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(dialog().textContent).toContain('Couldn’t add the application');
    expect(control('Company').value).toBe('  Northwind Logistics ');
    expect(control('Company').disabled).toBe(false);
  });

  it('link_PostingTheUserTracks_ShowsAlreadyTrackedWithViewApplication', async () => {
    let viewed: string | null = null;
    ref.componentInstance?.viewTracked.subscribe((id) => (viewed = id));

    type('Job posting link', 'https://jobs.example.com/4471');
    await new Promise((resolve) => setTimeout(resolve, LOOKUP_WAIT_MS));
    const lookup = http.expectOne((request) => request.url === TRACKED_URL);
    expect(lookup.request.params.get('url')).toBe('https://jobs.example.com/4471');
    lookup.flush({
      id: 'tracked-id',
      company: 'Northwind Logistics',
      role: 'Backend Engineer (.NET)',
      status: 'Interview',
      noResponseDays: null,
      appliedOn: '2026-11-03',
    });
    await render();

    expect(dialog().textContent).toContain('You already track this posting');
    expect(dialog().textContent).toContain('03 Nov 2026');
    expect(dialog().textContent).not.toContain('Company');
    button('View application').click();
    await render();
    expect(viewed).toBe('tracked-id');
  });

  it('link_NotTracked_KeepsTheForm', async () => {
    type('Job posting link', 'https://jobs.example.com/new');
    await new Promise((resolve) => setTimeout(resolve, LOOKUP_WAIT_MS));
    http
      .expectOne((request) => request.url === TRACKED_URL)
      .flush(null, { status: 204, statusText: 'No Content' });
    await render();

    expect(dialog().textContent).not.toContain('You already track this posting');
    expect(control('Company')).toBeTruthy();
  });

  it('submit_ConflictWithoutLink_ShowsTheDuplicateTextBanner', async () => {
    fillRequired();
    type('Job description', 'Same text as before');

    await submit();
    http
      .expectOne(APPLICATIONS_API)
      .flush({ code: 'applications.already_tracked' }, { status: 409, statusText: 'Conflict' });
    await render();

    expect(dialog().textContent).toContain(
      'Another of your applications has the same job description.',
    );
  });
});
