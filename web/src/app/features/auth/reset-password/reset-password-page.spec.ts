import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AUTH_API } from '../auth-paths';
import { ResetPasswordPage } from './reset-password-page';

@Component({ selector: 'app-stub-sign-in', template: '<p>Sign in page</p>' })
class StubSignIn {}

describe('ResetPasswordPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'reset-password', component: ResetPasswordPage },
          { path: 'login', component: StubSignIn },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/reset-password', ResetPasswordPage);
  });

  afterEach(() => http.verify());

  function root(): HTMLElement {
    return harness.routeNativeElement as HTMLElement;
  }

  function input(selector: string): HTMLInputElement {
    const element = root().querySelector<HTMLInputElement>(selector);
    if (!element) throw new Error(`No input ${selector}`);
    return element;
  }

  function type(selector: string, value: string): void {
    const element = input(selector);
    element.value = value;
    element.dispatchEvent(new Event('input'));
    harness.detectChanges();
  }

  function submit(): void {
    root().querySelector('form')?.dispatchEvent(new Event('submit'));
    harness.detectChanges();
  }

  function fillAndSubmit(): void {
    type('input[type=text]', 'k7qm2xrd9pla');
    type('input[type=password]', 'correct horse battery');
    submit();
  }

  it('type_CodeInLowerCase_IsFormattedWithDashes', () => {
    type('input[type=text]', 'k7qm2xrd9pla');

    expect(input('input[type=text]').value).toBe('K7QM-2XRD-9PLA');
  });

  it('submit_EmptyForm_ShowsBothErrorsWithoutARequest', () => {
    submit();

    expect(root().textContent).toContain('Enter the full 12-character code');
    expect(root().textContent).toContain('Use at least 12 characters');
    http.expectNone(AUTH_API.resetPassword);
  });

  it('submit_Accepted_SendsTheBareCodeAndGoesToSignInWithTheReason', async () => {
    fillAndSubmit();

    const request = http.expectOne(AUTH_API.resetPassword);
    expect(request.request.body).toEqual({
      code: 'K7QM2XRD9PLA',
      newPassword: 'correct horse battery',
    });
    request.flush(null, { status: 204, statusText: 'No Content' });
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/login?reason=password-reset');
  });

  it('submit_InvalidCode_ShowsItOnTheCodeField', () => {
    fillAndSubmit();

    http.expectOne(AUTH_API.resetPassword).flush(
      {
        code: 'auth.invalid_reset_code',
        errors: { code: ['This reset code is invalid, expired or already used.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    harness.detectChanges();

    expect(input('input[type=text]').getAttribute('aria-invalid')).toBe('true');
    expect(root().textContent).toContain('This reset code is invalid, expired or already used');
  });

  it('edit_AfterInvalidCode_ClearsTheServerError', () => {
    fillAndSubmit();
    http
      .expectOne(AUTH_API.resetPassword)
      .flush(
        { code: 'auth.invalid_reset_code', errors: { code: ['Invalid.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    harness.detectChanges();

    type('input[type=text]', 'K7QM-2XRD-9PLB');

    expect(root().textContent).not.toContain('This reset code is invalid');
  });

  it('submit_RateLimited_DisablesTheButton', () => {
    fillAndSubmit();

    http.expectOne(AUTH_API.resetPassword).flush(null, {
      status: 429,
      statusText: 'Too Many Requests',
      headers: { 'Retry-After': '20' },
    });
    harness.detectChanges();

    expect(root().textContent).toContain('Too many attempts.');
    expect(root().querySelector<HTMLButtonElement>('button[type=submit]')?.disabled).toBe(true);
  });

  it('submit_ServerUnavailable_KeepsTheDetails', () => {
    fillAndSubmit();

    http
      .expectOne(AUTH_API.resetPassword)
      .flush(null, { status: 503, statusText: 'Service Unavailable' });
    harness.detectChanges();

    expect(root().textContent).toContain('Couldn’t reach Heardback');
    expect(input('input[type=text]').value).toBe('K7QM-2XRD-9PLA');
  });
});
