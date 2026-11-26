import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Banner } from '../../../shared/ui/banner/banner';
import { Button } from '../../../shared/ui/button/button';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/field/input';
import { Link } from '../../../shared/ui/link/link';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { AuthCard } from '../auth-card/auth-card';
import { AUTH_ROUTES, LOGIN_REASONS, LOGIN_REASON_PARAM } from '../auth-paths';
import { createAuthSubmission } from '../auth-submission';
import { AuthService } from '../auth.service';
import { PASSWORD_LIMITS } from '../auth.models';
import {
  INVITE_CODE_FORMATTED_LENGTH,
  INVITE_CODE_LENGTH,
  formatInviteCode,
  normalizeInviteCode,
} from '../invite-code';
import { characterCount } from '../password-count';

type ResetField = 'code' | 'newPassword';
const FIELDS: readonly ResetField[] = ['code', 'newPassword'];

const CLIENT_MESSAGES: Readonly<Record<ResetField, string>> = {
  code: `Enter the full ${INVITE_CODE_LENGTH}-character code`,
  newPassword: `Use at least ${PASSWORD_LIMITS.min} characters`,
};

// Reset codes have the invite-code shape (ADR-0015), so they are typed and formatted the same way.
function fullCode(control: AbstractControl<string>): ValidationErrors | null {
  return normalizeInviteCode(control.value).length === INVITE_CODE_LENGTH ? null : { code: true };
}

/**
 * Set a new password with the one-time code an admin gave you (ADR-0015). Signed-out users only. On
 * success the server has ended every session, so the user signs in again with the new password.
 */
@Component({
  selector: 'app-reset-password-page',
  imports: [ReactiveFormsModule, RouterLink, AuthCard, Banner, Button, Field, Input, Link, Spinner],
  templateUrl: './reset-password-page.html',
})
export class ResetPasswordPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly routes = AUTH_ROUTES;
  protected readonly limits = PASSWORD_LIMITS;
  protected readonly codeMaxLength = INVITE_CODE_FORMATTED_LENGTH;
  protected readonly submission = createAuthSubmission();

  protected readonly form = inject(NonNullableFormBuilder).group({
    code: ['', fullCode],
    newPassword: [
      '',
      [
        Validators.required,
        Validators.minLength(PASSWORD_LIMITS.min),
        Validators.maxLength(PASSWORD_LIMITS.max),
      ],
    ],
  });

  private readonly submitted = signal(false);
  private readonly serverErrors = signal<Partial<Record<ResetField, string>>>({});
  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly errors = computed(() => {
    this.formValue();
    const server = this.serverErrors();
    const result: Partial<Record<ResetField, string>> = {};
    for (const field of FIELDS) {
      const message =
        server[field] ??
        (this.submitted() && this.form.controls[field].invalid
          ? CLIENT_MESSAGES[field]
          : undefined);
      if (message) {
        result[field] = message;
      }
    }
    return result;
  });

  protected readonly passwordCount = computed(() =>
    characterCount(this.formValue().newPassword ?? ''),
  );

  constructor() {
    for (const field of FIELDS) {
      this.form.controls[field].valueChanges
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => {
          if (this.serverErrors()[field] !== undefined) {
            this.serverErrors.update((errors) => ({ ...errors, [field]: undefined }));
          }
        });
    }
  }

  protected formatCode(): void {
    const control = this.form.controls.code;
    const formatted = formatInviteCode(control.value);
    if (formatted !== control.value) {
      control.setValue(formatted);
    }
  }

  protected submit(): void {
    this.submitted.set(true);
    if (this.form.invalid) {
      this.focusFirstInvalid();
      return;
    }

    const value = this.form.getRawValue();
    this.form.disable({ emitEvent: false });
    this.submission.run(
      this.auth.resetPassword({
        code: normalizeInviteCode(value.code),
        newPassword: value.newPassword,
      }),
      {
        success: () =>
          void this.router.navigate([AUTH_ROUTES.login], {
            queryParams: { [LOGIN_REASON_PARAM]: LOGIN_REASONS.passwordReset },
          }),
        failed: () => {
          this.form.enable({ emitEvent: false });
          const failure = this.submission.failure();
          if (failure?.kind === 'fields') {
            this.serverErrors.set(failure.fieldErrors);
            this.focusFirstInvalid();
          }
        },
      },
    );
  }

  private focusFirstInvalid(): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }
}
