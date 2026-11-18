import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
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

  it('authGuard sends signed-out users to login with a return URL', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(route, state));

    expect(serialize(result)).toBe('/login?returnUrl=%2Fapplications%2F42');
  });

  it('authGuard lets signed-in users through', async () => {
    await signIn({ isAdmin: false });

    expect(TestBed.runInInjectionContext(() => authGuard(route, state))).toBe(true);
  });

  it('adminGuard sends signed-in non-admins home', async () => {
    await signIn({ isAdmin: false });

    const result = TestBed.runInInjectionContext(() => adminGuard(route, state));

    expect(serialize(result)).toBe('/');
  });

  it('adminGuard lets admins through', async () => {
    await signIn({ isAdmin: true });

    expect(TestBed.runInInjectionContext(() => adminGuard(route, state))).toBe(true);
  });

  it('guestGuard lets signed-out users open sign in', () => {
    expect(TestBed.runInInjectionContext(() => guestGuard(route, state))).toBe(true);
  });

  it('guestGuard sends signed-in users home', async () => {
    await signIn({ isAdmin: false });

    const result = TestBed.runInInjectionContext(() => guestGuard(route, state));

    expect(serialize(result)).toBe('/');
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
