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

  async function signIn(): Promise<void> {
    const login = firstValueFrom(auth.login({ email: 'ada@example.com', password: 'pw' }));
    http.expectOne(AUTH_API.login).flush(authResponse());
    await login;
  }
});
