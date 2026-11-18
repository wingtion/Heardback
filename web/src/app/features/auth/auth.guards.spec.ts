import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  RedirectCommand,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { adminGuard, authGuard, guestGuard } from './auth.guards';
import { AUTH_API } from './auth-paths';
import { AuthService } from './auth.service';
import { authResponse } from './auth.testing';

describe('auth guards', () => {
  const route = {} as ActivatedRouteSnapshot;
  const state = { url: '/applications/42' } as RouterStateSnapshot;

  let http: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => http.verify());

  function run(guard: CanActivateFn): Promise<unknown> {
    return Promise.resolve(TestBed.runInInjectionContext(() => guard(route, state)));
  }

  it('authGuard_SignedOut_RedirectsToLoginWithReturnUrl', async () => {
    expect(serialize(await run(authGuard))).toBe('/login?returnUrl=%2Fapplications%2F42');
  });

  it('authGuard_SignedIn_AllowsNavigation', async () => {
    await signIn({ isAdmin: false });

    expect(await run(authGuard)).toBe(true);
  });

  it('authGuard_RestoreInProgress_WaitsBeforeDeciding', async () => {
    const auth = TestBed.inject(AuthService);
    void auth.restoreSession();
    let settled = false;
    const result = run(authGuard).then((value) => {
      settled = true;
      return value;
    });

    await Promise.resolve();
    expect(settled).toBe(false);

    http.expectOne(AUTH_API.refresh).flush(authResponse());

    expect(await result).toBe(true);
  });

  it('guestGuard_SignedOut_AllowsNavigation', async () => {
    expect(await run(guestGuard)).toBe(true);
  });

  it('guestGuard_SignedIn_RedirectsHome', async () => {
    await signIn({ isAdmin: false });

    expect(serialize(await run(guestGuard))).toBe('/');
  });

  it('adminGuard_SignedOut_RedirectsToLogin', async () => {
    expect(serialize(await run(adminGuard))).toBe('/login?returnUrl=%2Fapplications%2F42');
  });

  it('adminGuard_Member_ShowsNoAccessWithoutChangingTheUrl', async () => {
    await signIn({ isAdmin: false });

    const result = await run(adminGuard);

    expect(result).toBeInstanceOf(RedirectCommand);
    const command = result as RedirectCommand;
    expect(router.serializeUrl(command.redirectTo)).toBe('/no-access');
    expect(command.navigationBehaviorOptions?.skipLocationChange).toBe(true);
  });

  it('adminGuard_Admin_AllowsNavigation', async () => {
    await signIn({ isAdmin: true });

    expect(await run(adminGuard)).toBe(true);
  });

  async function signIn(options: { isAdmin: boolean }): Promise<void> {
    const login = firstValueFrom(
      TestBed.inject(AuthService).login({ email: 'a@b.c', password: 'pw' }),
    );
    http.expectOne(AUTH_API.login).flush(authResponse(options));
    await login;
  }

  function serialize(result: unknown): string {
    expect(result).toBeInstanceOf(UrlTree);
    return router.serializeUrl(result as UrlTree);
  }
});
