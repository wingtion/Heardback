import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { APPLICATIONS_API } from '../applications-api';
import { application } from '../applications.testing';
import { ApplicationDetail, DetailLayout } from './application-detail';

@Component({
  imports: [ApplicationDetail],
  template: `<app-application-detail
    [applicationId]="id()"
    [layout]="layout()"
    (closed)="closed = true"
    (deleted)="deleted = true"
    (changed)="changed = changed + 1"
    [revision]="revision()"
  />`,
})
class Host {
  readonly id = signal('app-1');
  readonly layout = signal<DetailLayout>('panel');
  readonly revision = signal(0);
  closed = false;
  deleted = false;
  changed = 0;
}

const DETAIL_URL = `${APPLICATIONS_API}/app-1`;

describe('ApplicationDetail', () => {
  let http: HttpTestingController;
  let host: Host;
  let root: HTMLElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(Host);
    host = fixture.componentInstance;
    root = fixture.nativeElement as HTMLElement;
    await render();
  });

  afterEach(() => {
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
    http.verify();
  });

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  function detail(): HTMLElement {
    const element = root.querySelector<HTMLElement>('app-application-detail');
    if (!element) throw new Error('No detail');
    return element;
  }

  function button(text: string): HTMLButtonElement {
    const found = Array.from(document.querySelectorAll('button')).find(
      (candidate) =>
        candidate.textContent?.trim() === text || candidate.getAttribute('aria-label') === text,
    );
    if (!found) throw new Error(`No button "${text}"`);
    return found;
  }

  async function respond(body: object = application()): Promise<void> {
    http.expectOne(DETAIL_URL).flush(body);
    await render();
  }

  it('load_InFlight_ShowsLoading', () => {
    expect(detail().textContent).toContain('Loading application…');
    http.expectOne(DETAIL_URL).flush(application());
  });

  it('load_Loaded_ShowsTheEnvelopeStatusNotesAndTimeline', async () => {
    await respond();

    const text = detail().textContent ?? '';
    expect(text).toContain('Northwind Logistics');
    expect(text).toContain('Backend Engineer (.NET)');
    expect(text).toContain('northwind-logistics.example/jobs/4471');
    expect(text).toContain('LinkedIn Easy Apply');
    expect(text).toContain('03 Nov 2026');
    expect(text).toContain('Recruiter call went well.');
    const events = Array.from(detail().querySelectorAll('ol li')).map((item) =>
      item.textContent?.replace(/\s+/g, ' ').trim(),
    );
    expect(events).toEqual([
      expect.stringContaining('Interview 14 Nov · Recruiter call, 30 minutes'),
      expect.stringContaining('Applied 3 Nov · Sent via LinkedIn Easy Apply'),
    ]);
    const link = detail().querySelector<HTMLAnchorElement>('a[href]');
    expect(link?.target).toBe('_blank');
    expect(link?.rel).toBe('noopener noreferrer');
  });

  it('load_PanelLayout_IsALabelledRegionNotADialog', async () => {
    await respond();

    expect(detail().getAttribute('role')).toBeNull();
    expect(detail().getAttribute('aria-label')).toBe('Application details');
    expect(button('Close details')).toBeTruthy();
  });

  it('load_SheetLayout_IsAModalDialog', async () => {
    host.layout.set('sheet');
    await respond();

    expect(detail().getAttribute('role')).toBe('dialog');
    expect(detail().getAttribute('aria-modal')).toBe('true');
    expect(detail().getAttribute('aria-labelledby')).toBe('application-detail-title');
    expect(button('Close and go back to applications')).toBeTruthy();
  });

  it('close_Clicked_EmitsClosed', async () => {
    await respond();

    button('Close details').click();

    expect(host.closed).toBe(true);
  });

  it('load_NotFound_SaysItMayHaveBeenDeleted', async () => {
    http.expectOne(DETAIL_URL).flush(null, { status: 404, statusText: 'Not Found' });
    await render();

    expect(detail().textContent).toContain('Couldn’t find this application');
    expect(detail().textContent).not.toContain('Delete');
  });

  it('load_ServerDown_ShowsErrorAndTryAgainReloads', async () => {
    http.expectOne(DETAIL_URL).flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(detail().textContent).toContain('Couldn’t load this application');
    button('Try again').click();
    await render();
    await respond();
    expect(detail().textContent).toContain('Northwind Logistics');
  });

  it('delete_Confirmed_EmitsDeleted', async () => {
    await respond();

    button('Delete').click();
    await render();
    button('Delete application').click();
    await render();
    http.expectOne(DETAIL_URL).flush(null, { status: 204, statusText: 'No Content' });
    await render();

    expect(host.deleted).toBe(true);
  });

  it('addUpdate_Panel_SitsBesideTheTimelineHeading', async () => {
    await respond();

    const heading = Array.from(detail().querySelectorAll('h3')).find(
      (candidate) => candidate.textContent?.trim() === 'Timeline',
    );
    expect(heading?.parentElement?.textContent).toContain('Add update');
  });

  it('addUpdate_Sheet_SitsInTheFooterBesideDelete', async () => {
    host.layout.set('sheet');
    await render();
    await respond();

    const footerButtons = Array.from(detail().querySelectorAll<HTMLButtonElement>('button')).slice(
      -2,
    );
    expect(footerButtons.map((candidate) => candidate.textContent?.trim())).toEqual([
      'Delete',
      'Add update',
    ]);
  });

  it('addUpdate_Saved_ShowsTheNewStatusHighlightsTheEventAndEmitsChanged', async () => {
    await respond();
    button('Add update').click();
    await render();
    const note = document.querySelector<HTMLTextAreaElement>('app-add-update-dialog textarea');
    if (!note) throw new Error('No note');
    note.value = 'Recruiter called';
    note.dispatchEvent(new Event('input'));
    document.querySelector('app-add-update-dialog form')?.dispatchEvent(new Event('submit'));
    await render();

    const before = application();
    http.expectOne(`${DETAIL_URL}/events`).flush(
      application({
        version: 8,
        events: [
          {
            id: 'e3',
            status: null,
            occurredOn: '2026-11-24',
            note: 'Recruiter called',
            isSubmission: false,
          },
          ...before.events,
        ],
      }),
    );
    await render();

    expect(host.changed).toBe(1);
    expect(detail().querySelector('[role="status"]')?.textContent).toContain('Update added');
    const rows = detail().querySelectorAll('ol li');
    expect(rows[0].textContent).toContain('Recruiter called');
    expect(rows[0].classList).toContain('bg-row-selected');
    expect(rows[1].classList).not.toContain('bg-row-selected');
  });

  it('revision_Bumped_ReloadsAndKeepsTheEnvelopeOnScreen', async () => {
    await respond();

    host.revision.set(1);
    await render();

    expect(detail().textContent).not.toContain('Loading application…');
    await respond(application({ status: 'Offer' }));
    expect(detail().textContent).toContain('Offer');
  });
});
