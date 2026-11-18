import { DOCUMENT } from '@angular/common';
import {
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Banner } from '../../../shared/ui/banner/banner';
import { Button } from '../../../shared/ui/button/button';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/field/input';
import { Link } from '../../../shared/ui/link/link';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { AuthCard } from '../auth-card/auth-card';
import {
  AUTH_ROUTES,
  LOGIN_REASONS,
  LOGIN_REASON_PARAM,
  RETURN_URL_PARAM,
  safeReturnUrl,
} from '../auth-paths';
import { createAuthSubmission } from '../auth-submission';
import { AuthService } from '../auth.service';

const EMAIL_MAX_LENGTH = 254;

/** Navigation state that can pre-fill the email, e.g. after the session expired. Never in the URL. */
export interface SignInNavigationState {
  readonly email?: string;
}

@Component({
  selector: 'app-sign-in-page',
  imports: [ReactiveFormsModule, RouterLink, AuthCard, Banner, Button, Field, Input, Link, Spinner],
  templateUrl: './sign-in-page.html',
})
export class SignInPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly routes = AUTH_ROUTES;
  protected readonly emailMaxLength = EMAIL_MAX_LENGTH;
  protected readonly submission = createAuthSubmission();

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: [this.emailFromNavigation(), [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  private readonly submitted = signal(false);
  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.value });

  protected readonly sessionExpired =
    this.route.snapshot.queryParamMap.get(LOGIN_REASON_PARAM) === LOGIN_REASONS.sessionExpired;

  protected readonly emailError = computed(
    () =>
      this.serverError('email') ?? (this.showError('email') ? 'Enter a valid email address' : null),
  );
  protected readonly passwordError = computed(
    () =>
      this.serverError('password') ?? (this.showError('password') ? 'Enter your password' : null),
  );

  protected submit(): void {
    this.submitted.set(true);
    if (this.form.invalid) {
      this.focusFirstInvalid();
      return;
    }

    const request = this.form.getRawValue();
    this.form.disable();
    this.submission.run(this.auth.login(request), {
      success: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get(RETURN_URL_PARAM);
        void this.router.navigateByUrl(safeReturnUrl(returnUrl));
      },
      failed: () => this.form.enable(),
    });
  }

  private serverError(field: 'email' | 'password'): string | null {
    const failure = this.submission.failure();
    return failure?.kind === 'fields' ? (failure.fieldErrors[field] ?? null) : null;
  }

  private showError(control: 'email' | 'password'): boolean {
    this.formValue();
    return this.submitted() && this.form.controls[control].invalid;
  }

  private focusFirstInvalid(): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }

  private emailFromNavigation(): string {
    const state: unknown = inject(DOCUMENT).defaultView?.history.state;
    const email =
      typeof state === 'object' && state !== null && 'email' in state ? state.email : null;
    return typeof email === 'string' ? email : '';
  }
}
