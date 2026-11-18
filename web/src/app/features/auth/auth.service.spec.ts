import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { AUTH_API } from './auth-paths';
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

  it('starts signed out', () => {
    expect(auth.isAuthenticated()).toBe(false);
    expect(auth.accessToken()).toBeNull();
  });

  it('login stores the user and access token in memory', async () => {
    const login = firstValueFrom(auth.login({ email: 'ada@example.com', password: 'pw' }));
    http.expectOne(AUTH_API.login).flush(authResponse({ token: 'fresh', isAdmin: true }));

    const user = await login;

    expect(user.email).toBe('ada@example.com');
    expect(auth.accessToken()).toBe('fresh');
    expect(auth.isAdmin()).toBe(true);
  });

  it('shares one refresh request between concurrent callers', async () => {
    const first = firstValueFrom(auth.refresh());
    const second = firstValueFrom(auth.refresh());

    http.expectOne(AUTH_API.refresh).flush(authResponse({ token: 'rotated' }));
    await Promise.all([first, second]);

    expect(auth.accessToken()).toBe('rotated');
  });

  it('a failed refresh signs the user out', async () => {
    await signIn();

    const refresh = firstValueFrom(auth.refresh());
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(refresh).rejects.toBeTruthy();
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('retries once when another tab refreshed first (superseded)', async () => {
    const refresh = firstValueFrom(auth.refresh());

    http
      .expectOne(AUTH_API.refresh)
      .flush({ code: 'auth.refresh_superseded' }, { status: 409, statusText: 'Conflict' });
    http.expectOne(AUTH_API.refresh).flush(authResponse({ token: 'from-other-tab' }));
    await refresh;

    expect(auth.accessToken()).toBe('from-other-tab');
  });

  it('keeps the session when the refresh fails for a reason other than 401', async () => {
    await signIn();

    for (const status of [429, 500, 0]) {
      const refresh = firstValueFrom(auth.refresh());
      http.expectOne(AUTH_API.refresh).flush(null, { status, statusText: 'Failed' });
      await expect(refresh).rejects.toBeTruthy();
    }

    expect(auth.isAuthenticated()).toBe(true);
  });

  it('restoreSession resolves signed out when there is no valid cookie', async () => {
    const restore = auth.restoreSession();
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(restore).resolves.toBeUndefined();
    expect(auth.isAuthenticated()).toBe(false);
  });

  it('restoreSession signs the user back in from the cookie', async () => {
    const restore = auth.restoreSession();
    http.expectOne(AUTH_API.refresh).flush(authResponse());

    await restore;

    expect(auth.user()?.displayName).toBe('Ada');
  });

  it('logout clears the session even when the server call fails', async () => {
    await signIn();

    const logout = firstValueFrom(auth.logout(), { defaultValue: undefined });
    http.expectOne(AUTH_API.logout).flush(null, { status: 500, statusText: 'Server Error' });
    await logout;

    expect(auth.isAuthenticated()).toBe(false);
  });

  it('restoreSession reports restoring until the refresh answers', async () => {
    const restore = auth.restoreSession();
    expect(auth.restoring()).toBe(true);

    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
    await restore;

    expect(auth.restoring()).toBe(false);
  });

  it('a startup restore without a cookie is not reported as an expired session', async () => {
    const restore = auth.restoreSession();
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
    await restore;

    expect(auth.expired()).toBeNull();
  });

  it('a signed-in session rejected by the server is reported as expired with its email', async () => {
    await signIn();

    const refresh = firstValueFrom(auth.refresh());
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
    await expect(refresh).rejects.toBeTruthy();

    expect(auth.expired()).toEqual({ email: 'ada@example.com' });
  });

  it('signing out is not reported as an expired session', async () => {
    await signIn();

    const logout = firstValueFrom(auth.logout(), { defaultValue: undefined });
    http.expectOne(AUTH_API.logout).flush(null);
    await logout;

    expect(auth.expired()).toBeNull();
  });

  async function signIn(): Promise<void> {
    const login = firstValueFrom(auth.login({ email: 'ada@example.com', password: 'pw' }));
    http.expectOne(AUTH_API.login).flush(authResponse());
    await login;
  }
});
