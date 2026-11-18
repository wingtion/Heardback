import { DialogRef } from '@angular/cdk/dialog';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DialogLauncher } from '../../../shared/ui/dialog/dialog-launcher';
import { APPLICATIONS_API } from '../applications-api';
import { DeleteApplicationData, DeleteApplicationDialog } from './delete-application-dialog';

const DATA: DeleteApplicationData = { id: 'app-1', company: 'Northwind Logistics' };
const DELETE_URL = `${APPLICATIONS_API}/app-1`;

describe('DeleteApplicationDialog', () => {
  let http: HttpTestingController;
  let ref: DialogRef<unknown, DeleteApplicationDialog>;
  let deleted: boolean;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    ref = TestBed.inject(DialogLauncher).open(DeleteApplicationDialog, {
      role: 'alertdialog',
      data: DATA,
    });
    deleted = false;
    ref.componentInstance?.deleted.subscribe(() => (deleted = true));
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

  function dialog(): HTMLElement | null {
    return document.querySelector<HTMLElement>('app-delete-application-dialog');
  }

  function button(text: string): HTMLButtonElement {
    const found = Array.from(dialog()?.querySelectorAll('button') ?? []).find(
      (candidate) => candidate.textContent?.trim() === text,
    );
    if (!found) throw new Error(`No button "${text}"`);
    return found;
  }

  it('open_Default_IsAnAlertDialogNamingTheCompany', () => {
    expect(document.querySelector('[role="alertdialog"]')).not.toBeNull();
    expect(dialog()?.textContent).toContain('Delete this application?');
    expect(dialog()?.textContent).toContain(
      'Northwind Logistics and its timeline will be removed. You can’t undo this.',
    );
  });

  it('confirm_Succeeds_DeletesEmitsAndCloses', async () => {
    button('Delete application').click();
    await render();

    const request = http.expectOne(DELETE_URL);
    expect(request.request.method).toBe('DELETE');
    expect(button('Deleting…').getAttribute('aria-busy')).toBe('true');
    request.flush(null, { status: 204, statusText: 'No Content' });
    await render();

    expect(deleted).toBe(true);
    expect(dialog()).toBeNull();
  });

  it('confirm_AlreadyDeletedElsewhere_TreatsItAsDone', async () => {
    button('Delete application').click();
    await render();

    http.expectOne(DELETE_URL).flush(null, { status: 404, statusText: 'Not Found' });
    await render();

    expect(deleted).toBe(true);
    expect(dialog()).toBeNull();
  });

  it('confirm_ServerDown_ShowsTheErrorAndStaysOpen', async () => {
    button('Delete application').click();
    await render();

    http.expectOne(DELETE_URL).flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(deleted).toBe(false);
    expect(dialog()?.textContent).toContain('Couldn’t delete the application');
    expect(button('Delete application').disabled).toBe(false);
  });

  it('cancel_Clicked_ClosesWithoutDeleting', async () => {
    button('Cancel').click();
    await render();

    http.expectNone(DELETE_URL);
    expect(dialog()).toBeNull();
  });
});
