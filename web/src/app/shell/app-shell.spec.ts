import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom } from 'rxjs';
import { AUTH_API } from '../features/auth/auth-paths';
import { AuthService } from '../features/auth/auth.service';
import { authResponse } from '../features/auth/auth.testing';
import { AppShell } from './app-shell';

@Component({ template: '<p>Applications page</p>' })
class StubPage {}

@Component({ template: '<p>Sign in page</p>' })
class StubLogin {}

describe('AppShell', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'login', component: StubLogin },
          {
            path: '',
            component: AppShell,
            children: [{ path: 'applications', component: StubPage }],
          },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => http.verify());

  async function openAs(options: { isAdmin: boolean }): Promise<HTMLElement> {
    const login = firstValueFrom(auth.login({ email: 'ada@example.com', password: 'pw' }));
    http.expectOne(AUTH_API.login).flush(authResponse(options));
    await login;
    harness = await RouterTestingHarness.create('/applications');
    return harness.fixture.nativeElement as HTMLElement;
  }

  it('render_Member_ShowsTheUserAndMemberRole', async () => {
    const root = await openAs({ isAdmin: false });

    expect(root.textContent).toContain('Ada');
    expect(root.textContent).toContain('Member');
  });

  it('render_Member_HidesTheAdminSection', async () => {
    const root = await openAs({ isAdmin: false });

    expect(root.textContent).not.toContain('Invite codes');
  });

  it('render_Admin_ShowsTheAdminSection', async () => {
    const root = await openAs({ isAdmin: true });

    expect(root.textContent).toContain('Invite codes');
    expect(root.textContent).toContain('Admin');
  });

  it('render_CurrentPage_IsMarkedInTheNavigation', async () => {
    const root = await openAs({ isAdmin: false });

    const current = root.querySelector('a[aria-current=page]');
    expect(current?.textContent).toContain('Applications');
  });

  it('signOut_Clicked_EndsTheSessionAndGoesToSignIn', async () => {
    const root = await openAs({ isAdmin: false });
    const signOut = Array.from(root.querySelectorAll('button')).find((button) =>
      button.textContent?.includes('Sign out'),
    );

    signOut?.click();
    http.expectOne(AUTH_API.logout).flush(null);
    await harness.fixture.whenStable();

    expect(auth.isAuthenticated()).toBe(false);
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('sessionExpired_ServerRejectsRefresh_GoesToSignInWithReasonAndReturnUrl', async () => {
    await openAs({ isAdmin: false });
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate');

    const refresh = firstValueFrom(auth.refresh());
    http.expectOne(AUTH_API.refresh).flush(null, { status: 401, statusText: 'Unauthorized' });
    await expect(refresh).rejects.toBeTruthy();
    harness.detectChanges();

    // No navigation state: browsers keep history.state across reloads, and the email must not linger.
    expect(navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { reason: 'session-expired', returnUrl: '/applications' },
    });
  });

  it('mobileMenu_OpenedThenEscape_Closes', async () => {
    const root = await openAs({ isAdmin: false });
    const menuButton = root.querySelector<HTMLButtonElement>('button[aria-label="Open menu"]');

    menuButton?.click();
    harness.detectChanges();
    const menu = root.querySelector('#mobile-menu');
    expect(menu).not.toBeNull();
    expect(menuButton?.getAttribute('aria-expanded')).toBe('true');

    menu?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    harness.detectChanges();

    expect(root.querySelector('#mobile-menu')).toBeNull();
    expect(menuButton?.getAttribute('aria-expanded')).toBe('false');
  });
});
