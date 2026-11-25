import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom } from 'rxjs';
import { AUTH_API } from '../auth-paths';
import { AuthService } from '../auth.service';
import { authResponse } from '../auth.testing';
import { SignInPage } from './sign-in-page';

describe('SignInPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'login', component: SignInPage }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  async function open(url = '/login') {
    await harness.navigateByUrl(url, SignInPage);
    return harness.fixture as ComponentFixture<unknown>;
  }

  function root(): HTMLElement {
    return harness.routeNativeElement as HTMLElement;
  }

  function type(selector: string, value: string): void {
    const input = root().querySelector<HTMLInputElement>(selector);
    if (!input) throw new Error(`No input ${selector}`);
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function submit(): void {
    root().querySelector('form')?.dispatchEvent(new Event('submit'));
    harness.detectChanges();
  }

  async function fillAndSubmit(): Promise<void> {
    await open();
    type('input[type=email]', 'ada@example.com');
    type('input[type=password]', 'correct-horse-12');
    submit();
  }

  it('submit_EmptyForm_ShowsFieldErrorsWithoutCallingTheApi', async () => {
    await open();

    submit();

    expect(root().textContent).toContain('Enter a valid email address');
    expect(root().textContent).toContain('Enter your password');
    http.expectNone(AUTH_API.login);
  });

  it('submit_ValidForm_SignsInAndFollowsTheReturnUrl', async () => {
    await open('/login?returnUrl=%2Fapplications%2F42');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    type('input[type=email]', 'ada@example.com');
    type('input[type=password]', 'correct-horse-12');

    submit();
    expect(root().textContent).toContain('Signing in…');
    const request = http.expectOne(AUTH_API.login);
    expect(request.request.body).toEqual({
      email: 'ada@example.com',
      password: 'correct-horse-12',
    });
    request.flush(authResponse());

    expect(navigate).toHaveBeenCalledWith('/applications/42');
  });

  it('submit_ExternalReturnUrl_GoesHomeInstead', async () => {
    await open('/login?returnUrl=%2F%2Fevil.example');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    type('input[type=email]', 'ada@example.com');
    type('input[type=password]', 'correct-horse-12');

    submit();
    http.expectOne(AUTH_API.login).flush(authResponse());

    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('submit_WrongCredentials_ShowsOneMessageAndKeepsTheDetails', async () => {
    await fillAndSubmit();

    http
      .expectOne(AUTH_API.login)
      .flush({ code: 'auth.invalid_credentials' }, { status: 401, statusText: 'Unauthorized' });
    harness.detectChanges();

    expect(root().querySelector('[role=alert]')?.textContent).toContain(
      'Email or password is incorrect',
    );
    expect(root().querySelector<HTMLInputElement>('input[type=email]')?.value).toBe(
      'ada@example.com',
    );
  });

  it('submit_RateLimited_DisablesSignInUntilTheCountdownEnds', async () => {
    vi.useFakeTimers();
    await fillAndSubmit();

    http.expectOne(AUTH_API.login).flush(null, {
      status: 429,
      statusText: 'Too Many Requests',
      headers: { 'Retry-After': '2' },
    });
    harness.detectChanges();
    const button = root().querySelector<HTMLButtonElement>('button[type=submit]');

    expect(root().textContent).toContain('Too many attempts.');
    expect(button?.disabled).toBe(true);

    vi.advanceTimersByTime(2000);
    harness.detectChanges();

    expect(root().textContent).not.toContain('Too many attempts.');
    expect(button?.disabled).toBe(false);
  });

  it('submit_ServerUnavailable_ShowsConnectionError', async () => {
    await fillAndSubmit();

    http.expectOne(AUTH_API.login).flush(null, { status: 503, statusText: 'Unavailable' });
    harness.detectChanges();

    expect(root().textContent).toContain('Couldn’t reach Heardback');
  });

  it('open_AfterSessionRestoreGaveUp_ShowsConnectionError', async () => {
    const restore = TestBed.inject(AuthService).restoreSession();
    http.expectOne(AUTH_API.refresh).flush(null, {
      status: 429,
      statusText: 'Too Many Requests',
      headers: { 'Retry-After': '600' },
    });
    await restore;

    await open();

    expect(root().textContent).toContain('Couldn’t reach Heardback');
    expect(root().textContent).toContain('Check your connection and try again.');
    expect(root().textContent).not.toContain('Your details are still filled in');
  });

  it('open_AfterSessionExpired_PrefillsTheEmailFromMemory', async () => {
    const auth = TestBed.inject(AuthService);
    const login = firstValueFrom(auth.login({ email: 'ada@example.com', password: 'pw' }));
    http.expectOne(AUTH_API.login).flush(authResponse());
    await login;
    const refresh = firstValueFrom(auth.refresh());
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
    await expect(refresh).rejects.toBeTruthy();

    await open('/login?reason=session-expired');

    expect(root().querySelector<HTMLInputElement>('input[type=email]')?.value).toBe(
      'ada@example.com',
    );
  });

  it('open_EmailInNavigationState_IsIgnored', async () => {
    await TestBed.inject(Router).navigate(['/login'], {
      queryParams: { reason: 'session-expired' },
      state: { email: 'someone-else@example.com' },
    });
    harness.detectChanges();

    expect(root().querySelector<HTMLInputElement>('input[type=email]')?.value).toBe('');
  });

  it('open_AfterSessionExpired_ShowsTheNotice', async () => {
    await open('/login?reason=session-expired');

    expect(root().textContent).toContain('Your session has expired. Please sign in again.');
  });

  it('open_AfterAccountDeleted_SaysTheDataIsGone', async () => {
    await open('/login?reason=account-deleted');

    expect(root().textContent).toContain('Your account and all its data were deleted.');
  });

  it('open_UnknownReason_ShowsNoNotice', async () => {
    await open('/login?reason=anything');

    expect(root().querySelector('app-banner')).toBeNull();
  });
});
