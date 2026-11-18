import { DialogRef } from '@angular/cdk/dialog';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DialogLauncher } from '../../../shared/ui/dialog/dialog-launcher';
import { localToday } from '../application-display';
import { APPLICATIONS_API } from '../applications-api';
import { Application } from '../applications.models';
import { application } from '../applications.testing';
import { AddUpdateData, AddUpdateDialog } from './add-update-dialog';

const EVENTS_URL = `${APPLICATIONS_API}/app-1/events`;

describe('AddUpdateDialog', () => {
  let http: HttpTestingController;
  let ref: DialogRef<unknown, AddUpdateDialog>;
  let saved: Application[];

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    const data: AddUpdateData = { application: application() };
    ref = TestBed.inject(DialogLauncher).open(AddUpdateDialog, { data });
    saved = [];
    ref.componentInstance?.saved.subscribe((value) => saved.push(value));
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
    const element = document.querySelector<HTMLElement>('app-add-update-dialog');
    if (!element) throw new Error('No dialog');
    return element;
  }

  function radio(label: string): HTMLInputElement {
    const chip = Array.from(dialog().querySelectorAll('fieldset label')).find(
      (candidate) => candidate.textContent?.trim() === label,
    );
    const input = chip?.querySelector('input');
    if (!input) throw new Error(`No chip "${label}"`);
    return input;
  }

  function control(label: string): HTMLInputElement | HTMLTextAreaElement {
    const labelElement = Array.from(dialog().querySelectorAll('label')).find((candidate) =>
      candidate.textContent?.trim().startsWith(label),
    );
    const element = labelElement?.htmlFor ? document.getElementById(labelElement.htmlFor) : null;
    if (!(element instanceof HTMLInputElement) && !(element instanceof HTMLTextAreaElement)) {
      throw new Error(`No control "${label}"`);
    }
    return element;
  }

  function type(label: string, value: string): void {
    const element = control(label);
    element.value = value;
    element.dispatchEvent(new Event('input'));
  }

  function choose(label: string): void {
    const input = radio(label);
    input.checked = true;
    input.dispatchEvent(new Event('change'));
  }

  async function submit(): Promise<void> {
    dialog().querySelector('form')?.dispatchEvent(new Event('submit'));
    await render();
  }

  function eventRequest(): TestRequest {
    return http.expectOne(EVENTS_URL);
  }

  it('open_InterviewApplication_ShowsNoChangeAndTheCurrentStatusDisabled', () => {
    expect(dialog().textContent).toContain('Northwind Logistics · Backend Engineer (.NET)');
    expect(radio('No change').checked).toBe(true);
    expect(radio('Interview · current').disabled).toBe(true);
    expect(radio('Offer').disabled).toBe(false);
    expect((control('Date') as HTMLInputElement).value).toBe(localToday(new Date()));
    expect(dialog().textContent).toContain('0 / 280');
  });

  it('submit_NothingChosen_AsksForAStatusOrANote', async () => {
    type('Note', '   ');
    await submit();

    expect(dialog().querySelector('[role="alert"]')?.textContent).toContain(
      'Choose a new status or write a note.',
    );
    http.expectNone(EVENTS_URL);
  });

  it('submit_FutureDate_ShowsTheDateError', async () => {
    choose('Offer');
    type('Date', '2999-01-01');
    await submit();

    expect(dialog().textContent).toContain(
      'The date can’t be in the future. Pick today or earlier.',
    );
    http.expectNone(EVENTS_URL);
  });

  it('submit_DateBeforeApplied_NamesTheAppliedDate', async () => {
    choose('Offer');
    type('Date', '2026-11-01');
    await submit();

    expect(dialog().textContent).toContain(
      'Pick a date on or after the applied date (03 Nov 2026)',
    );
    http.expectNone(EVENTS_URL);
  });

  it('submit_StatusAndNote_SavesEmitsAndCloses', async () => {
    choose('Offer');
    type('Note', '  Offer call  ');
    await submit();

    const request = eventRequest();
    expect(request.request.body).toEqual({
      status: 'Offer',
      occurredOn: localToday(new Date()),
      note: 'Offer call',
    });
    expect(dialog().textContent).toContain('Saving…');
    request.flush(application({ status: 'Offer' }));
    await render();

    expect(saved.map((value) => value.status)).toEqual(['Offer']);
    expect(document.querySelector('app-add-update-dialog')).toBeNull();
  });

  it('submit_NoteOnly_SendsNoStatus', async () => {
    type('Note', 'Recruiter called');
    await submit();

    expect(eventRequest().request.body).toEqual(
      expect.objectContaining({ status: null, note: 'Recruiter called' }),
    );
  });

  it('submit_ServerDown_KeepsTheFormWithABanner', async () => {
    choose('Rejected');
    await submit();

    eventRequest().flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(dialog().textContent).toContain('Couldn’t save the update');
    expect(radio('Rejected').checked).toBe(true);
    expect(radio('Interview · current').disabled).toBe(true);
    expect(saved).toEqual([]);
  });

  it('submit_StatusChangedElsewhere_ShowsTheServerMessage', async () => {
    choose('Offer');
    await submit();

    eventRequest().flush(
      {
        code: 'applications.same_status',
        errors: { status: ['The application already has this status.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await render();

    expect(dialog().querySelector('[role="alert"]')?.textContent).toContain(
      'The application already has this status.',
    );
  });
});
