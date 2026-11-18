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
import { APPLICATIONS_API } from '../applications-api';
import { Application } from '../applications.models';
import { application } from '../applications.testing';
import { EditApplicationData, EditApplicationDialog } from './edit-application-dialog';

const DETAIL_URL = `${APPLICATIONS_API}/app-1`;

describe('EditApplicationDialog', () => {
  let http: HttpTestingController;
  let ref: DialogRef<unknown, EditApplicationDialog>;
  let saved: Application[];
  let reloaded: Application[];

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    const data: EditApplicationData = { application: application() };
    ref = TestBed.inject(DialogLauncher).open(EditApplicationDialog, {
      mobile: 'full-screen',
      data,
    });
    saved = [];
    reloaded = [];
    ref.componentInstance?.saved.subscribe((value) => saved.push(value));
    ref.componentInstance?.reloaded.subscribe((value) => reloaded.push(value));
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
    const element = document.querySelector<HTMLElement>('app-edit-application-dialog');
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

  function saveRequest(): TestRequest {
    return http.expectOne((request) => request.url === DETAIL_URL && request.method === 'PUT');
  }

  it('open_Application_FillsEveryFieldAndExplainsWhereStatusChanges', () => {
    expect(control('Job posting link').value).toBe('https://northwind-logistics.example/jobs/4471');
    expect(control('Company').value).toBe('Northwind Logistics');
    expect(control('Role').value).toBe('Backend Engineer (.NET)');
    expect(control('Channel').value).toBe('LinkedInEasyApply');
    expect(control('Applied date').value).toBe('2026-11-03');
    expect(control('Notes').value).toBe('Recruiter call went well.');
    expect(dialog().textContent).toContain('Status changes through the board or Add update.');
    expect(dialog().textContent).toContain('optional, only you see these');
  });

  it('submit_InvalidFields_ShowsTheErrorsWithoutSaving', async () => {
    type('Job posting link', 'northwind-logistics/jobs');
    type('Company', '  ');
    type('Applied date', '2999-01-01');
    await submit();

    const text = dialog().textContent;
    expect(text).toContain('Enter a full link that starts with https://');
    expect(text).toContain('Enter the company name');
    expect(text).toContain('Pick today or an earlier date');
    http.expectNone(DETAIL_URL);
  });

  it('submit_Valid_SendsTheWholeFormWithTheVersionAndCloses', async () => {
    type('Role', '  Senior Backend Engineer ');
    type('Notes', '');
    type('Job posting link', '');
    await submit();

    const request = saveRequest();
    expect(request.request.body).toEqual({
      company: 'Northwind Logistics',
      role: 'Senior Backend Engineer',
      url: null,
      postingText: null,
      channel: 'LinkedInEasyApply',
      appliedOn: '2026-11-03',
      notes: null,
      version: 7,
    });
    expect(button('Saving…').disabled).toBe(true);
    request.flush(application({ role: 'Senior Backend Engineer', version: 8 }));
    await render();

    expect(saved.map((value) => value.role)).toEqual(['Senior Backend Engineer']);
    expect(document.querySelector('app-edit-application-dialog')).toBeNull();
  });

  it('submit_ChangedElsewhere_WarnsAndDisablesSaveUntilReloaded', async () => {
    type('Role', 'Mine');
    await submit();
    saveRequest().flush(
      { code: 'applications.changed_elsewhere' },
      { status: 409, statusText: 'Conflict' },
    );
    await render();

    const banner = dialog().querySelector('app-banner');
    expect(banner?.getAttribute('role')).toBe('alert');
    expect(banner?.textContent).toContain('This application changed since you opened it.');
    expect(button('Save').disabled).toBe(true);

    button('Reload latest version').click();
    await render();
    http
      .expectOne((request) => request.url === DETAIL_URL && request.method === 'GET')
      .flush(application({ role: 'Theirs', version: 9 }));
    await render();

    expect(control('Role').value).toBe('Theirs');
    expect(dialog().querySelector('app-banner')).toBeNull();
    expect(button('Save').disabled).toBe(false);
    expect(reloaded.map((value) => value.version)).toEqual([9]);

    await submit();
    expect(saveRequest().request.body).toEqual(expect.objectContaining({ version: 9 }));
  });

  it('submit_LinkTrackedByAnotherApplication_SaysSo', async () => {
    await submit();
    saveRequest().flush(
      { code: 'applications.already_tracked' },
      { status: 409, statusText: 'Conflict' },
    );
    await render();

    expect(dialog().textContent).toContain('Another of your applications uses this link.');
    expect(button('Save').disabled).toBe(false);
  });

  it('submit_AppliedAfterTheFirstUpdate_ShowsTheServerFieldError', async () => {
    type('Applied date', '2026-11-16');
    await submit();
    saveRequest().flush(
      {
        code: 'applications.applied_after_first_update',
        errors: { appliedOn: ['Pick a date on or before your first update (14 Nov 2026)'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await render();

    expect(dialog().textContent).toContain(
      'Pick a date on or before your first update (14 Nov 2026)',
    );
    expect(control('Applied date').getAttribute('aria-invalid')).toBe('true');
  });

  it('submit_ServerDown_KeepsTheEdits', async () => {
    type('Role', 'Kept');
    await submit();
    saveRequest().flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(dialog().textContent).toContain('Couldn’t save your changes');
    expect(control('Role').value).toBe('Kept');
  });

  it('submit_DeletedElsewhere_SaysItMayHaveBeenDeleted', async () => {
    await submit();
    saveRequest().flush(
      { code: 'applications.not_found' },
      { status: 404, statusText: 'Not Found' },
    );
    await render();

    expect(dialog().textContent).toContain('Couldn’t find this application');
  });
});
