import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AUTH_API } from '../auth-paths';
import { authResponse } from '../auth.testing';
import { RegisterPage } from './register-page';

describe('RegisterPage', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'register', component: RegisterPage }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/register', RegisterPage);
  });

  afterEach(() => http.verify());

  function root(): HTMLElement {
    return harness.routeNativeElement as HTMLElement;
  }

  function input(name: string): HTMLInputElement {
    const element = root().querySelector<HTMLInputElement>(`input[formcontrolname=${name}]`);
    if (!element) throw new Error(`No input ${name}`);
    return element;
  }

  function type(name: string, value: string): void {
    const element = input(name);
    element.value = value;
    element.dispatchEvent(new Event('input'));
    harness.detectChanges();
  }

  function submit(): void {
    root().querySelector('form')?.dispatchEvent(new Event('submit'));
    harness.detectChanges();
  }

  function fillValidForm(): void {
    type('inviteCode', 'k7qm2xrd9pla');
    type('displayName', '  Doğan Süle ');
    type('email', 'dogan@example.com');
    type('password', 'river-postcard-1907');
  }

  it('inviteCode_Typing_AddsDashesAndUppercases', () => {
    type('inviteCode', 'k7qm2x');

    expect(input('inviteCode').value).toBe('K7QM-2X');
  });

  it('password_Typing_ShowsTheCharacterCount', () => {
    type('password', 'abc');

    expect(root().textContent).toContain('3 characters');
  });

  it('submit_InvalidForm_ShowsEveryFieldErrorAndTheSummary', () => {
    type('inviteCode', 'ABCD-EFG');
    type('password', 'short-pw');

    submit();

    const text = root().textContent ?? '';
    expect(text).toContain('Enter the full 12-character code');
    expect(text).toContain('Enter a display name');
    expect(text).toContain('Enter a valid email address');
    expect(text).toContain('Use at least 12 characters');
    expect(text).toContain('4 fields need attention');
    http.expectNone(AUTH_API.register);
  });

  it('submit_ValidForm_SendsNormalizedValuesAndGoesHome', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    fillValidForm();

    submit();
    const request = http.expectOne(AUTH_API.register);
    expect(request.request.body).toEqual({
      inviteCode: 'K7QM2XRD9PLA',
      displayName: 'Doğan Süle',
      email: 'dogan@example.com',
      password: 'river-postcard-1907',
    });
    request.flush(authResponse());

    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('submit_InviteRejected_ShowsTheErrorOnTheInviteField_UntilItIsEdited', () => {
    fillValidForm();
    submit();

    http
      .expectOne(AUTH_API.register)
      .flush(
        { code: 'auth.invalid_invite_code', errors: { inviteCode: ['Invalid.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    harness.detectChanges();

    expect(input('inviteCode').getAttribute('aria-invalid')).toBe('true');
    expect(root().textContent).toContain('This invite code is invalid, expired or already used');

    type('inviteCode', 'QX7M2RDK9PLB');

    expect(input('inviteCode').getAttribute('aria-invalid')).toBeNull();
  });

  it('submit_EmailTaken_ShowsTheErrorOnTheEmailField', () => {
    fillValidForm();
    submit();

    http
      .expectOne(AUTH_API.register)
      .flush({ code: 'auth.email_taken' }, { status: 409, statusText: 'Conflict' });
    harness.detectChanges();

    expect(input('email').getAttribute('aria-invalid')).toBe('true');
    expect(root().textContent).toContain('An account with this email already exists');
  });

  it('submit_ServerUnavailable_KeepsTheDetailsAndShowsTheError', () => {
    fillValidForm();
    submit();

    http.expectOne(AUTH_API.register).flush(null, { status: 500, statusText: 'Server Error' });
    harness.detectChanges();

    expect(root().textContent).toContain('Couldn’t create your account');
    expect(input('email').value).toBe('dogan@example.com');
  });
});
