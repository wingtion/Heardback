import { Clipboard } from '@angular/cdk/clipboard';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { MEMBERS_API } from './members-api';
import { MembersSection } from './members-section';
import { member, memberPage } from './members.testing';

@Component({ selector: 'app-stub-no-access', template: '<p>No access page</p>' })
class StubNoAccess {}

const GRACE = member();
const ADMIN = member({
  id: '0195a3c2-0000-7000-8000-0000000000b2',
  displayName: 'Ada Admin',
  email: 'ada@example.com',
  isAdmin: true,
});
const RESET_URL = `${MEMBERS_API}/${GRACE.id}/reset-code`;

describe('MembersSection', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let clipboard: { copy: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    clipboard = { copy: vi.fn(() => true) };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'admin/members', component: MembersSection },
          { path: 'no-access', component: StubNoAccess },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Clipboard, useValue: clipboard },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/admin/members', MembersSection);
  });

  afterEach(() => {
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
    http.verify();
  });

  function root(): HTMLElement {
    return harness.routeNativeElement as HTMLElement;
  }

  function dialog(): HTMLElement {
    const element = document.querySelector<HTMLElement>('app-reset-code-dialog');
    if (!element) throw new Error('No dialog');
    return element;
  }

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  async function respondToList(body: object): Promise<void> {
    await render();
    const request = http.expectOne((r) => r.url === MEMBERS_API);
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

  /** The table's button; the phone list has its own copy with the same label. */
  function tableResetButton(name: string): HTMLButtonElement | null {
    return root().querySelector<HTMLButtonElement>(
      `table button[aria-label="Create a reset code for ${name}"]`,
    );
  }

  async function openDialogFor(name: string): Promise<void> {
    tableResetButton(name)?.click();
    await render();
  }

  async function createCode(): Promise<void> {
    buttonIn(dialog(), 'Create reset code').click();
    await render();
  }

  it('load_Members_ListsNameEmailRoleAndCount', async () => {
    await respondToList(memberPage({ items: [ADMIN, GRACE] }));

    const text = root().textContent ?? '';
    expect(text).toContain('2 members');
    expect(text).toContain('Grace Hopper');
    expect(text).toContain('grace@example.com');
    expect(text).toContain('12 Nov 2026');
  });

  it('load_Admin_HasNoResetButtonAndTheReasonIsShown', async () => {
    await respondToList(memberPage({ items: [ADMIN, GRACE] }));

    expect(tableResetButton('Ada Admin')).toBeNull();
    expect(tableResetButton('Grace Hopper')).not.toBeNull();
    expect(root().textContent).toContain('They use the owner command on the server.');
  });

  it('load_OpenCode_ShowsWhenItEnds', async () => {
    await respondToList(
      memberPage({ items: [member({ resetCodeExpiresAt: '2026-11-26T14:30:00Z' })] }),
    );

    expect(root().querySelector('table')?.textContent).toContain('Open until');
    expect(root().querySelector('table')?.textContent).toContain('26 Nov');
  });

  it('load_Fails_ShowsErrorAndRetries', async () => {
    await render();
    http
      .expectOne((r) => r.url === MEMBERS_API)
      .flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(root().textContent).toContain('Couldn’t load members');
    buttonIn(root(), 'Try again').click();
    await respondToList(memberPage());

    expect(root().textContent).toContain('Grace Hopper');
  });

  it('load_Forbidden_ShowsNoAccess', async () => {
    await render();
    http
      .expectOne((r) => r.url === MEMBERS_API)
      .flush(null, { status: 403, statusText: 'Forbidden' });
    await render();
    await harness.fixture.whenStable();

    expect(document.body.textContent).toContain('No access page');
  });

  it('createResetCode_Confirmed_ShowsTheCodeOnceAndRefreshesTheList', async () => {
    await respondToList(memberPage({ items: [GRACE] }));
    await openDialogFor('Grace Hopper');

    expect(dialog().textContent).toContain('Create a reset code');
    expect(dialog().textContent).toContain('grace@example.com');
    await createCode();

    const request = http.expectOne(RESET_URL);
    expect(request.request.method).toBe('POST');
    request.flush({ code: 'K7QM-2XRD-9PLA', expiresAt: '2026-11-26T14:30:00Z' });
    await respondToList(
      memberPage({ items: [{ ...GRACE, resetCodeExpiresAt: '2026-11-26T14:30:00Z' }] }),
    );

    expect(dialog().textContent).toContain('Reset code created');
    expect(dialog().textContent).toContain('K7QM-2XRD-9PLA');
    expect(root().querySelector('table')?.textContent).toContain('Open until');
  });

  it('copy_AfterCreating_CopiesAndSaysSo', async () => {
    await respondToList(memberPage({ items: [GRACE] }));
    await openDialogFor('Grace Hopper');
    await createCode();
    http.expectOne(RESET_URL).flush({ code: 'K7QM-2XRD-9PLA', expiresAt: '2026-11-26T14:30:00Z' });
    await respondToList(memberPage({ items: [GRACE] }));

    buttonIn(dialog(), 'Copy').click();
    await render();

    expect(clipboard.copy).toHaveBeenCalledWith('K7QM-2XRD-9PLA');
    expect(dialog().textContent).toContain('Copied to clipboard');
  });

  it('open_MemberWithOpenCode_SaysTheNewOneReplacesIt', async () => {
    await respondToList(
      memberPage({ items: [{ ...GRACE, resetCodeExpiresAt: '2026-11-26T14:30:00Z' }] }),
    );
    await openDialogFor('Grace Hopper');

    expect(dialog().textContent).toContain('A new one replaces it.');
  });

  it('createResetCode_MemberGone_SaysSoAndReloadsTheList', async () => {
    await respondToList(memberPage({ items: [GRACE] }));
    await openDialogFor('Grace Hopper');
    await createCode();

    http
      .expectOne(RESET_URL)
      .flush({ code: 'admin.member_not_found' }, { status: 404, statusText: 'Not Found' });
    await respondToList(memberPage({ items: [] }));

    expect(dialog().textContent).toContain('This member no longer exists');
    expect(buttonIn(dialog(), 'Create reset code').disabled).toBe(true);
  });

  it('createResetCode_MemberBecameAdmin_ExplainsTheOwnerCommand', async () => {
    await respondToList(memberPage({ items: [GRACE] }));
    await openDialogFor('Grace Hopper');
    await createCode();

    http
      .expectOne(RESET_URL)
      .flush({ code: 'admin.reset_admin' }, { status: 403, statusText: 'Forbidden' });
    await render();

    expect(dialog().textContent).toContain('Admins can’t get a reset code here');
  });

  it('createResetCode_NoLongerAdmin_ShowsNoAccess', async () => {
    await respondToList(memberPage({ items: [GRACE] }));
    await openDialogFor('Grace Hopper');
    await createCode();

    http.expectOne(RESET_URL).flush(null, { status: 403, statusText: 'Forbidden' });
    await render();
    await harness.fixture.whenStable();

    expect(document.body.textContent).toContain('No access page');
    expect(document.querySelector('app-reset-code-dialog')).toBeNull();
  });

  it('createResetCode_HourlyLimit_SaysWhenToTryAgain', async () => {
    await respondToList(memberPage({ items: [GRACE] }));
    await openDialogFor('Grace Hopper');
    await createCode();

    http.expectOne(RESET_URL).flush(null, {
      status: 429,
      statusText: 'Too Many Requests',
      headers: { 'Retry-After': '90' },
    });
    await render();

    expect(dialog().textContent).toContain('Reset code limit reached');
    expect(dialog().textContent?.replace(/\s+/g, ' ')).toContain('Try again in about 2 minutes');
  });

  it('createResetCode_ServerDown_LetsTheAdminTryAgain', async () => {
    await respondToList(memberPage({ items: [GRACE] }));
    await openDialogFor('Grace Hopper');
    await createCode();

    http.expectOne(RESET_URL).flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(dialog().textContent).toContain('Couldn’t create the reset code');
    expect(buttonIn(dialog(), 'Create reset code').disabled).toBe(false);
  });
});
