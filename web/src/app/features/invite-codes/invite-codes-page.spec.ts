import { LiveAnnouncer } from '@angular/cdk/a11y';
import { Clipboard } from '@angular/cdk/clipboard';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { INVITE_CODES_API } from './invite-codes-api';
import { InviteCodesPage } from './invite-codes-page';
import { inviteCode, invitePage } from './invite-codes.testing';

@Component({ template: '<p>No access page</p>' })
class StubNoAccess {}

describe('InviteCodesPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let clipboard: { copy: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    clipboard = { copy: vi.fn(() => true) };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'admin/invite-codes', component: InviteCodesPage },
          { path: 'no-access', component: StubNoAccess },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Clipboard, useValue: clipboard },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/admin/invite-codes', InviteCodesPage);
  });

  afterEach(() => {
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
    http.verify();
  });

  function root(): HTMLElement {
    return harness.routeNativeElement as HTMLElement;
  }

  function dialog(): HTMLElement {
    const element = document.querySelector<HTMLElement>('app-invite-code-dialog');
    if (!element) throw new Error('No dialog');
    return element;
  }

  // Not whenStable(): it waits for open HTTP requests, and these tests answer them one by one.
  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  async function respondToList(body: object, page = 1): Promise<void> {
    await render();
    const request = http.expectOne((r) => r.url === INVITE_CODES_API);
    expect(request.request.params.get('page')).toBe(String(page));
    expect(request.request.params.get('pageSize')).toBe('20');
    request.flush(body);
    await render();
  }

  function buttonIn(container: HTMLElement, text: string): HTMLButtonElement {
    const button = Array.from(container.querySelectorAll('button')).find(
      (candidate) => candidate.textContent?.trim() === text,
    );
    if (!button) throw new Error(`No button "${text}"`);
    return button;
  }

  async function openCreateDialog(): Promise<void> {
    buttonIn(root(), 'Create invite code').click();
    await render();
  }

  async function submitCreate(days: string): Promise<void> {
    const input = dialog().querySelector('input');
    if (!input) throw new Error('No input');
    input.value = days;
    input.dispatchEvent(new Event('input'));
    dialog().querySelector('form')?.dispatchEvent(new Event('submit'));
    await render();
  }

  it('load_InFlight_ShowsLoadingState', async () => {
    await render();

    expect(root().textContent).toContain('Loading invite codes…');
    http.expectOne((r) => r.url === INVITE_CODES_API).flush(invitePage());
  });

  it('load_WithCodes_ShowsRowsAndSummary', async () => {
    const used = inviteCode({
      id: 'used',
      code: 'R9PL-6YQA-2MXK',
      status: 'Used',
      usedAt: '2026-11-22T09:00:00Z',
    });

    await respondToList(
      invitePage({ items: [inviteCode(), used], totalCount: 34, totalPages: 2, activeCount: 9 }),
    );

    const rows = root().querySelectorAll('tbody tr');
    expect(root().textContent).toContain('34 codes · 9 active');
    expect(rows).toHaveLength(2);
    expect(rows[0].textContent).toContain('K7QM-2XRD-9PLA');
    expect(rows[0].textContent).toContain('Active');
    expect(rows[0].textContent).toContain('01 Dec 2026');
    expect(rows[0].textContent).toContain('Not used');
    expect(rows[1].textContent).toContain('Used');
    expect(rows[1].textContent).toContain('22 Nov 2026');
  });

  it('load_NoCodes_ShowsEmptyState', async () => {
    await respondToList(invitePage({ items: [] }));

    expect(root().textContent).toContain('No invite codes yet');
    expect(root().querySelector('app-pagination')).toBeNull();
  });

  it('load_ServerError_ShowsErrorAndTryAgainReloads', async () => {
    await render();
    http
      .expectOne((r) => r.url === INVITE_CODES_API)
      .flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(root().textContent).toContain('Couldn’t load invite codes');

    buttonIn(root(), 'Try again').click();
    await respondToList(invitePage());

    expect(root().textContent).toContain('K7QM-2XRD-9PLA');
  });

  it('load_Forbidden_ShowsNoAccessWithoutChangingTheUrl', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    await render();

    http
      .expectOne((r) => r.url === INVITE_CODES_API)
      .flush(null, { status: 403, statusText: 'Forbidden' });
    await render();

    expect(navigate).toHaveBeenCalledWith('/no-access', { skipLocationChange: true });
  });

  it('nextPage_Clicked_LoadsTheSecondPage', async () => {
    await respondToList(invitePage({ totalCount: 34, totalPages: 2 }));

    root().querySelector<HTMLButtonElement>('button[aria-label="Next page"]')?.click();
    await respondToList(invitePage({ page: 2, totalCount: 34, totalPages: 2 }), 2);

    expect(root().textContent).toContain('Showing 21–34 of 34');
    expect(root().textContent).toContain('Page 2 of 2');
  });

  it('copy_RowButton_CopiesTheCodeAndAnnouncesIt', async () => {
    const announce = vi.spyOn(TestBed.inject(LiveAnnouncer), 'announce').mockResolvedValue();
    await respondToList(invitePage());

    root()
      .querySelector<HTMLButtonElement>('button[aria-label="Copy code K7QM-2XRD-9PLA"]')
      ?.click();

    expect(clipboard.copy).toHaveBeenCalledWith('K7QM-2XRD-9PLA');
    expect(announce).toHaveBeenCalledWith('Code copied to clipboard');
  });

  it('copy_RowButton_ShowsACheckForTwoSeconds', async () => {
    const checkPath = 'M3.5 8.5l3 3 6-7';
    await respondToList(invitePage());
    const button = root().querySelector<HTMLButtonElement>(
      'tbody button[aria-label="Copy code K7QM-2XRD-9PLA"]',
    );
    const iconPath = () => button?.querySelector('path')?.getAttribute('d');
    vi.useFakeTimers();

    try {
      button?.click();
      TestBed.tick();
      expect(iconPath()).toBe(checkPath);

      vi.advanceTimersByTime(2000);
      TestBed.tick();
      expect(iconPath()).not.toBe(checkPath);
    } finally {
      vi.useRealTimers();
    }
  });

  it('create_DefaultExpiry_ShowsTheNewCodeAndRefreshesTheList', async () => {
    await respondToList(invitePage({ items: [] }));
    await openCreateDialog();

    await submitCreate('7');
    const create = http.expectOne((r) => r.method === 'POST' && r.url === INVITE_CODES_API);
    expect(create.request.body).toEqual({ expiresInDays: 7 });
    create.flush(inviteCode());
    await respondToList(invitePage());

    expect(dialog().textContent).toContain('Invite code created');
    expect(dialog().textContent).toContain('K7QM-2XRD-9PLA');
    expect(dialog().textContent).toContain('in 7 days');
    expect(root().querySelectorAll('tbody tr')).toHaveLength(1);
  });

  it('create_Copy_ShowsCopiedToClipboard', async () => {
    await respondToList(invitePage({ items: [] }));
    await openCreateDialog();
    await submitCreate('7');
    http.expectOne((r) => r.method === 'POST').flush(inviteCode());
    await respondToList(invitePage());

    buttonIn(dialog(), 'Copy').click();
    await render();

    expect(clipboard.copy).toHaveBeenCalledWith('K7QM-2XRD-9PLA');
    expect(dialog().querySelector('[role=status]')?.textContent).toContain('Copied to clipboard');
    expect(buttonIn(dialog(), 'Copied')).toBeTruthy();
  });

  it('create_EmptyExpiry_LetsTheServerUseItsDefault', async () => {
    await respondToList(invitePage({ items: [] }));
    await openCreateDialog();

    await submitCreate('');

    const create = http.expectOne((r) => r.method === 'POST');
    expect(create.request.body).toEqual({ expiresInDays: null });
    create.flush(inviteCode());
    await respondToList(invitePage());
  });

  it('create_ExpiryOutOfRange_ShowsFieldErrorWithoutRequest', async () => {
    await respondToList(invitePage({ items: [] }));
    await openCreateDialog();

    await submitCreate('120');

    http.expectNone((r) => r.method === 'POST');
    expect(dialog().textContent).toContain('Enter a whole number of days from 1 to 90');
    expect(dialog().querySelector('input')?.getAttribute('aria-invalid')).toBe('true');
  });

  it('create_ServerError_ShowsBannerAndKeepsTheForm', async () => {
    await respondToList(invitePage({ items: [] }));
    await openCreateDialog();

    await submitCreate('7');
    http
      .expectOne((r) => r.method === 'POST')
      .flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(dialog().textContent).toContain('Couldn’t create the invite code');
    expect(dialog().querySelector('input')?.disabled).toBe(false);
  });
});
