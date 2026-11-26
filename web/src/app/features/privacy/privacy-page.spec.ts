import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom } from 'rxjs';
import { AUTH_API } from '../auth/auth-paths';
import { AuthService } from '../auth/auth.service';
import { authResponse } from '../auth/auth.testing';
import { PrivacyPage } from './privacy-page';

describe('PrivacyPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'privacy', component: PrivacyPage }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
  });

  afterEach(() => http.verify());

  function root(): HTMLElement {
    return harness.routeNativeElement as HTMLElement;
  }

  function headerLink(): HTMLAnchorElement {
    const link = root().querySelector<HTMLAnchorElement>('header a');
    if (!link) throw new Error('No header link');
    return link;
  }

  it('open_SignedOut_OffersSignIn', async () => {
    await harness.navigateByUrl('/privacy', PrivacyPage);

    expect(headerLink().textContent?.trim()).toBe('Sign in');
    expect(headerLink().getAttribute('href')).toBe('/login');
  });

  it('open_SignedIn_LeadsBackToApplications', async () => {
    const auth = TestBed.inject(AuthService);
    const login = firstValueFrom(auth.login({ email: 'ada@example.com', password: 'pw' }));
    http.expectOne(AUTH_API.login).flush(authResponse());
    await login;

    await harness.navigateByUrl('/privacy', PrivacyPage);

    expect(headerLink().textContent?.trim()).toBe('Go to Applications');
    expect(headerLink().getAttribute('href')).toBe('/applications');
  });

  it('open_Anyone_CoversStorageSharingAiAndDeletion', async () => {
    await harness.navigateByUrl('/privacy', PrivacyPage);

    const headings = Array.from(root().querySelectorAll('h2')).map((h) => h.textContent?.trim());
    expect(headings).toEqual([
      'What Heardback keeps',
      'Who can see it',
      'What is sent to AI providers',
      'Cookies and this browser',
      'Taking your data out or deleting it',
      'Questions',
    ]);
  });
});
