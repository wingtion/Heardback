import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { ACCOUNT_API, AUTH_API } from './auth-paths';
import { AuthService } from './auth.service';
import { authResponse } from './auth.testing';

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('initialState_NoSession_IsSignedOut', () => {
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.accessToken()).toBeNull();
  });

  it('login_ValidResponse_StoresUserAndTokenInMemory', async () => {
    const login = firstValueFrom(auth.login({ email: 'ada@example.com', password: 'pw' }));
    http.expectOne(AUTH_API.login).flush(authResponse({ token: 'fresh', isAdmin: true }));

    const user = await login;

    expect(user.email).toBe('ada@example.com');
    expect(auth.accessToken()).toBe('fresh');
    expect(auth.isAdmin()).toBe(true);
  });

  it('refresh_ConcurrentCallers_ShareOneRequest', async () => {
    const first = firstValueFrom(auth.refresh());
    const second = firstValueFrom(auth.refresh());

    http.expectOne(AUTH_API.refresh).flush(authResponse({ token: 'rotated' }));
    await Promise.all([first, second]);

    expect(auth.accessToken()).toBe('rotated');
  });

  it('refresh_Unauthorized_SignsTheUserOut', async () => {
    await signIn();

    const refresh = firstValueFrom(auth.refresh());
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(refresh).rejects.toBeTruthy();
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('refresh_SupersededByAnotherTab_RetriesOnce', async () => {
    const refresh = firstValueFrom(auth.refresh());

    http
      .expectOne(AUTH_API.refresh)
      .flush({ code: 'auth.refresh_superseded' }, { status: 409, statusText: 'Conflict' });
    http.expectOne(AUTH_API.refresh).flush(authResponse({ token: 'from-other-tab' }));
    await refresh;

    expect(auth.accessToken()).toBe('from-other-tab');
  });

  it('refresh_FailsWithoutUnauthorized_KeepsTheSession', async () => {
    await signIn();

    for (const status of [429, 500, 0]) {
      const refresh = firstValueFrom(auth.refresh());
      http.expectOne(AUTH_API.refresh).flush(null, { status, statusText: 'Failed' });
      await expect(refresh).rejects.toBeTruthy();
    }

    expect(auth.isAuthenticated()).toBe(true);
  });

  it('restoreSession_NoValidCookie_ResolvesSignedOut', async () => {
    const restore = auth.restoreSession();
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(restore).resolves.toBeUndefined();
    expect(auth.isAuthenticated()).toBe(false);
  });

  describe('restoreSession on temporary failures', () => {
    beforeEach(() => vi.useFakeTimers());
    afterEach(() => vi.useRealTimers());

    const unavailable = { status: 503, statusText: 'Service Unavailable' };

    it('restoreSession_ServerBrieflyUnavailable_RetriesAndKeepsTheSession', async () => {
      const restore = auth.restoreSession();
      http.expectOne(AUTH_API.refresh).flush(null, unavailable);

      await vi.advanceTimersByTimeAsync(1000);
      http.expectOne(AUTH_API.refresh).flush(authResponse());
      await restore;

      expect(auth.isAuthenticated()).toBe(true);
      expect(auth.restoreFailed()).toBe(false);
    });

    it('restoreSession_RateLimited_WaitsForRetryAfterBeforeRetrying', async () => {
      const restore = auth.restoreSession();
      http.expectOne(AUTH_API.refresh).flush(null, {
        status: 429,
        statusText: 'Too Many Requests',
        headers: { 'Retry-After': '3' },
      });

      await vi.advanceTimersByTimeAsync(2999);
      http.expectNone(AUTH_API.refresh);
      await vi.advanceTimersByTimeAsync(1);
      http.expectOne(AUTH_API.refresh).flush(authResponse());
      await restore;

      expect(auth.isAuthenticated()).toBe(true);
    });

    it('restoreSession_StillUnavailableAfterRetries_StartsSignedOutAndReportsIt', async () => {
      const restore = auth.restoreSession();
      for (const wait of [1000, 2000, 4000]) {
        http.expectOne(AUTH_API.refresh).flush(null, unavailable);
        await vi.advanceTimersByTimeAsync(wait);
      }
      http.expectOne(AUTH_API.refresh).flush(null, unavailable);
      await restore;

      expect(auth.isAuthenticated()).toBe(false);
      expect(auth.restoreFailed()).toBe(true);
      expect(auth.restoring()).toBe(false);
    });

    it('restoreSession_RetryAfterTooLong_GivesUpWithoutWaiting', async () => {
      const restore = auth.restoreSession();
      http.expectOne(AUTH_API.refresh).flush(null, {
        status: 429,
        statusText: 'Too Many Requests',
        headers: { 'Retry-After': '120' },
      });
      await restore;

      expect(auth.restoreFailed()).toBe(true);
    });

    it('restoreSession_NoValidCookie_DoesNotRetry', async () => {
      const restore = auth.restoreSession();
      http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
      await restore;
      await vi.advanceTimersByTimeAsync(10_000);

      http.expectNone(AUTH_API.refresh);
      expect(auth.restoreFailed()).toBe(false);
    });
  });

  it('restoreSession_ValidCookie_SignsTheUserBackIn', async () => {
    const restore = auth.restoreSession();
    http.expectOne(AUTH_API.refresh).flush(authResponse());

    await restore;

    expect(auth.user()?.displayName).toBe('Ada');
  });

  it('logout_ServerCallFails_StillClearsTheSession', async () => {
    await signIn();

    const logout = firstValueFrom(auth.logout(), { defaultValue: undefined });
    http.expectOne(AUTH_API.logout).flush(null, { status: 500, statusText: 'Server Error' });
    await logout;

    expect(auth.isAuthenticated()).toBe(false);
  });

  it('restoreSession_InFlight_ReportsRestoring', async () => {
    const restore = auth.restoreSession();
    expect(auth.restoring()).toBe(true);

    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
    await restore;

    expect(auth.restoring()).toBe(false);
  });

  it('restoreSession_NoCookie_IsNotReportedAsExpired', async () => {
    const restore = auth.restoreSession();
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
    await restore;

    expect(auth.expired()).toBeNull();
  });

  it('refresh_SignedInSessionRejected_IsReportedAsExpiredWithEmail', async () => {
    await signIn();

    const refresh = firstValueFrom(auth.refresh());
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
    await expect(refresh).rejects.toBeTruthy();

    expect(auth.expired()).toEqual({ email: 'ada@example.com', demo: false });
  });

  it('startDemo_Success_SignsInAsTheDemoVisitor', async () => {
    const start = firstValueFrom(auth.startDemo());
    http.expectOne(AUTH_API.demo).flush(authResponse({ demoEndsAt: '2026-11-27T14:30:00Z' }));
    await start;

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.isDemo()).toBe(true);
  });

  it('refresh_DemoSessionRejected_IsReportedAsAnEndedDemoWithoutEmail', async () => {
    const start = firstValueFrom(auth.startDemo());
    http.expectOne(AUTH_API.demo).flush(authResponse({ demoEndsAt: '2026-11-27T14:30:00Z' }));
    await start;

    const refresh = firstValueFrom(auth.refresh());
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
    await expect(refresh).rejects.toBeTruthy();

    expect(auth.expired()).toEqual({ email: null, demo: true });
  });

  it('logout_SignedIn_IsNotReportedAsExpired', async () => {
    await signIn();

    const logout = firstValueFrom(auth.logout(), { defaultValue: undefined });
    http.expectOne(AUTH_API.logout).flush(null);
    await logout;

    expect(auth.expired()).toBeNull();
  });

  it('changePassword_Succeeds_KeepsThisBrowserSignedInWithTheNewSession', async () => {
    await signIn();

    const change = firstValueFrom(
      auth.changePassword({ currentPassword: 'old', newPassword: 'new-password-1' }),
      { defaultValue: undefined },
    );
    const request = http.expectOne(ACCOUNT_API.password);
    expect(request.request.body).toEqual({ currentPassword: 'old', newPassword: 'new-password-1' });
    request.flush(authResponse({ token: 'after-change' }));
    await change;

    expect(auth.accessToken()).toBe('after-change');
    expect(auth.isAuthenticated()).toBe(true);
  });

  it('changePassword_Fails_KeepsTheOldSession', async () => {
    await signIn();

    const change = firstValueFrom(
      auth.changePassword({ currentPassword: 'wrong', newPassword: 'new-password-1' }),
    );
    http
      .expectOne(ACCOUNT_API.password)
      .flush({ code: 'account.wrong_password' }, { status: 400, statusText: 'Bad Request' });
    await expect(change).rejects.toBeTruthy();

    expect(auth.accessToken()).toBe('access-token');
  });

  it('deleteAccount_Succeeds_SendsThePasswordAndSignsOut', async () => {
    await signIn();

    const deletion = firstValueFrom(auth.deleteAccount('secret'), { defaultValue: undefined });
    const request = http.expectOne(ACCOUNT_API.account);
    expect(request.request.method).toBe('DELETE');
    expect(request.request.body).toEqual({ password: 'secret' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await deletion;

    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.expired()).toBeNull();
  });

  it('deleteAccount_Fails_StaysSignedIn', async () => {
    await signIn();

    const deletion = firstValueFrom(auth.deleteAccount('wrong'));
    http
      .expectOne(ACCOUNT_API.account)
      .flush({ code: 'account.wrong_password' }, { status: 400, statusText: 'Bad Request' });
    await expect(deletion).rejects.toBeTruthy();

    expect(auth.isAuthenticated()).toBe(true);
  });

  it('resetPassword_Succeeds_DoesNotSignIn', async () => {
    const reset = firstValueFrom(
      auth.resetPassword({ code: 'K7QM2XRD9PLA', newPassword: 'new-password-1' }),
      { defaultValue: undefined },
    );
    http.expectOne(AUTH_API.resetPassword).flush(null, { status: 204, statusText: 'No Content' });
    await reset;

    expect(auth.isAuthenticated()).toBe(false);
  });

  async function signIn(): Promise<void> {
    const login = firstValueFrom(auth.login({ email: 'ada@example.com', password: 'pw' }));
    http.expectOne(AUTH_API.login).flush(authResponse());
    await login;
  }
});
