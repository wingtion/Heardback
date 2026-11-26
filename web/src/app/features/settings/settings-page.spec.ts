import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom } from 'rxjs';
import { FileSaver } from '../../core/save-file';
import { THEME_STORAGE_KEY } from '../../core/theme';
import { ACCOUNT_API, AUTH_API } from '../auth/auth-paths';
import { AuthService } from '../auth/auth.service';
import { authResponse } from '../auth/auth.testing';
import { SettingsPage } from './settings-page';

@Component({ selector: 'app-stub-sign-in', template: '<p>Sign in page</p>' })
class StubSignIn {}

@Component({ selector: 'app-stub-privacy', template: '<p>Privacy page</p>' })
class StubPrivacy {}

const NEW_PASSWORD = 'correct horse battery';

describe('SettingsPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let auth: AuthService;
  let saver: { save: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    localStorage.clear();
    saver = { save: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'settings', component: SettingsPage },
          { path: 'login', component: StubSignIn },
          { path: 'privacy', component: StubPrivacy },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: FileSaver, useValue: saver },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    const login = firstValueFrom(auth.login({ email: 'ada@example.com', password: 'pw' }));
    http.expectOne(AUTH_API.login).flush(authResponse({ token: 'before' }));
    await login;

    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/settings', SettingsPage);
  });

  afterEach(() => {
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
    document.documentElement.removeAttribute('data-theme');
    localStorage.clear();
    http.verify();
  });

  function root(): HTMLElement {
    return harness.routeNativeElement as HTMLElement;
  }

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  function buttonIn(container: HTMLElement, text: string): HTMLButtonElement {
    const button = Array.from(container.querySelectorAll('button')).find(
      (candidate) => candidate.textContent?.trim() === text,
    );
    if (!button) throw new Error(`No button "${text}"`);
    return button;
  }

  function inputLabelled(container: HTMLElement, label: string): HTMLInputElement {
    const element = Array.from(container.querySelectorAll('label')).find(
      (candidate) => candidate.textContent?.trim() === label,
    );
    // A field's label points to its input; a radio chip's label wraps it.
    const input = element?.htmlFor
      ? container.querySelector<HTMLInputElement>(`#${element.htmlFor}`)
      : (element?.querySelector('input') ?? null);
    if (!input) throw new Error(`No input "${label}"`);
    return input;
  }

  function type(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function dialog(): HTMLElement {
    const element = document.querySelector<HTMLElement>('app-delete-account-dialog');
    if (!element) throw new Error('No dialog');
    return element;
  }

  describe('account', () => {
    it('render_SignedIn_ShowsNameEmailAndRole', () => {
      const text = root().textContent ?? '';

      expect(text).toContain('Ada');
      expect(text).toContain('ada@example.com');
      expect(text).toContain('Member');
    });
  });

  describe('demo account', () => {
    async function becomeDemo(): Promise<void> {
      const start = firstValueFrom(auth.startDemo());
      http.expectOne(AUTH_API.demo).flush(authResponse({ demoEndsAt: '2099-01-01T14:30:00Z' }));
      await start;
      await render();
    }

    it('render_DemoVisitor_ExplainsThereIsNoPasswordAndHidesDeletion', async () => {
      await becomeDemo();
      const text = root().textContent ?? '';

      expect(text).toContain('Demo accounts have no password. Sign out to end the demo.');
      expect(text).toContain('Demo');
      expect(text).not.toContain('ada@example.com');
      expect(text).not.toContain('Change password');
      expect(text).not.toContain('Delete account');
      expect(text).toContain('Your data');
    });
  });

  describe('change password', () => {
    async function changePassword(current: string, next: string): Promise<void> {
      type(inputLabelled(root(), 'Current password'), current);
      type(inputLabelled(root(), 'New password'), next);
      buttonIn(root(), 'Change password').click();
      await render();
    }

    it('submit_EmptyFields_ShowsBothErrorsWithoutARequest', async () => {
      buttonIn(root(), 'Change password').click();
      await render();

      expect(root().textContent).toContain('Enter your current password');
      expect(root().textContent).toContain('Use at least 12 characters');
      http.expectNone(ACCOUNT_API.password);
    });

    it('submit_Accepted_UsesTheNewSessionAndSaysSo', async () => {
      await changePassword('old password 1', NEW_PASSWORD);

      const request = http.expectOne(ACCOUNT_API.password);
      expect(request.request.body).toEqual({
        currentPassword: 'old password 1',
        newPassword: NEW_PASSWORD,
      });
      request.flush(authResponse({ token: 'after' }));
      await render();

      expect(auth.accessToken()).toBe('after');
      expect(root().textContent).toContain('Password changed. Your other devices are signed out.');
      expect(inputLabelled(root(), 'New password').value).toBe('');
    });

    it('submit_WrongCurrentPassword_ShowsItOnThatField', async () => {
      await changePassword('wrong password', NEW_PASSWORD);

      http.expectOne(ACCOUNT_API.password).flush(
        {
          code: 'account.wrong_password',
          errors: { currentPassword: ['This password is incorrect.'] },
        },
        { status: 400, statusText: 'Bad Request' },
      );
      await render();

      const current = inputLabelled(root(), 'Current password');
      expect(current.getAttribute('aria-invalid')).toBe('true');
      expect(root().textContent).toContain('This password is incorrect');
      expect(auth.accessToken()).toBe('before');
    });

    it('submit_ServerDown_ShowsThePasswordIsUnchanged', async () => {
      await changePassword('old password 1', NEW_PASSWORD);

      http
        .expectOne(ACCOUNT_API.password)
        .flush(null, { status: 503, statusText: 'Service Unavailable' });
      await render();

      expect(root().textContent).toContain('Couldn’t change your password');
      expect(root().textContent).toContain('Your password is unchanged.');
    });

    it('submit_RateLimited_DisablesTheButtonWithACountdown', async () => {
      await changePassword('old password 1', NEW_PASSWORD);

      http.expectOne(ACCOUNT_API.password).flush(null, {
        status: 429,
        statusText: 'Too Many Requests',
        headers: { 'Retry-After': '30' },
      });
      await render();

      expect(root().textContent).toContain('Too many attempts.');
      expect(buttonIn(root(), 'Change password').disabled).toBe(true);
    });
  });

  describe('theme', () => {
    it('choose_Dark_AppliesAndRemembersIt', async () => {
      const dark = inputLabelled(root(), 'Dark');
      dark.click();
      await render();

      expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
      expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');
      expect(dark.checked).toBe(true);
    });

    it('render_NothingChosen_SameAsDeviceIsChecked', () => {
      expect(inputLabelled(root(), 'Same as device').checked).toBe(true);
    });
  });

  describe('data export', () => {
    it('download_Succeeds_SavesTheFileWithTodaysName', async () => {
      buttonIn(root(), 'Download my data').click();
      await render();

      const file = new Blob(['{}'], { type: 'application/json' });
      http.expectOne(ACCOUNT_API.export).flush(file);
      await render();

      expect(saver.save).toHaveBeenCalledWith(
        file,
        expect.stringMatching(/^heardback-export-\d{4}-\d{2}-\d{2}\.json$/),
      );
      expect(root().textContent).toContain('Your file is downloading');
    });

    it('download_RateLimited_SaysWhenToTryAgainInMinutes', async () => {
      buttonIn(root(), 'Download my data').click();
      await render();

      http.expectOne(ACCOUNT_API.export).flush(new Blob(), {
        status: 429,
        statusText: 'Too Many Requests',
        headers: { 'Retry-After': '600' },
      });
      await render();

      expect(root().textContent).toContain('Download limit reached');
      expect(root().textContent?.replace(/\s+/g, ' ')).toContain('Try again in about 10 minutes');
      expect(saver.save).not.toHaveBeenCalled();
    });

    it('download_ServerDown_ShowsTheErrorBanner', async () => {
      buttonIn(root(), 'Download my data').click();
      await render();

      http
        .expectOne(ACCOUNT_API.export)
        .flush(new Blob(), { status: 500, statusText: 'Server Error' });
      await render();

      expect(root().textContent).toContain('Couldn’t prepare your file');
    });
  });

  describe('delete account', () => {
    async function openDialog(): Promise<void> {
      buttonIn(root(), 'Delete account').click();
      await render();
    }

    async function submitPassword(password: string): Promise<void> {
      type(inputLabelled(dialog(), 'Your password'), password);
      buttonIn(dialog(), 'Delete account').click();
      await render();
    }

    it('open_ShowsAnAlertDialogFocusedOnThePassword', async () => {
      await openDialog();

      const container = dialog().closest('[role="alertdialog"]');
      expect(container).not.toBeNull();
      expect(dialog().textContent).toContain('Delete your account?');
    });

    it('confirm_NoPassword_AsksForItWithoutARequest', async () => {
      await openDialog();

      buttonIn(dialog(), 'Delete account').click();
      await render();

      expect(dialog().textContent).toContain('Enter your password');
      http.expectNone(ACCOUNT_API.account);
    });

    it('confirm_WrongPassword_ShowsItOnTheField', async () => {
      await openDialog();
      await submitPassword('wrong');

      http
        .expectOne(ACCOUNT_API.account)
        .flush(
          { code: 'account.wrong_password', errors: { password: ['This password is incorrect.'] } },
          { status: 400, statusText: 'Bad Request' },
        );
      await render();

      expect(dialog().textContent).toContain('This password is incorrect');
      expect(auth.isAuthenticated()).toBe(true);
    });

    it('confirm_OnlyAdmin_ExplainsWhyItCantBeDeleted', async () => {
      await openDialog();
      await submitPassword('secret');

      http
        .expectOne(ACCOUNT_API.account)
        .flush({ code: 'account.last_admin' }, { status: 409, statusText: 'Conflict' });
      await render();

      expect(dialog().textContent).toContain('You’re the only admin');
    });

    it('confirm_Deleted_SignsOutAndGoesToSignInWithTheReason', async () => {
      await openDialog();
      await submitPassword('secret');

      const request = http.expectOne(ACCOUNT_API.account);
      expect(request.request.body).toEqual({ password: 'secret' });
      request.flush(null, { status: 204, statusText: 'No Content' });
      await render();

      expect(auth.isAuthenticated()).toBe(false);
      expect(TestBed.inject(Router).url).toBe('/login?reason=account-deleted');
      expect(document.querySelector('app-delete-account-dialog')).toBeNull();
    });
  });
});
