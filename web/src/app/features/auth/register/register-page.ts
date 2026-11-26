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
import { AUTH_ROUTES } from '../auth-paths';
import { createAuthSubmission } from '../auth-submission';
import { AuthService } from '../auth.service';
import { characterCount } from '../password-count';
import {
  INVITE_CODE_FORMATTED_LENGTH,
  INVITE_CODE_LENGTH,
  formatInviteCode,
  normalizeInviteCode,
} from '../invite-code';

/** Limits shared with the API (User, PasswordPolicy). */
export const REGISTER_LIMITS = {
  displayNameMax: 100,
  emailMax: 254,
  passwordMin: 12,
  passwordMax: 128,
} as const;

type RegisterField = 'inviteCode' | 'displayName' | 'email' | 'password';
const FIELDS: readonly RegisterField[] = ['inviteCode', 'displayName', 'email', 'password'];

/** Client-side messages from the design (docs/design.md §10). */
const CLIENT_MESSAGES: Readonly<Record<RegisterField, string>> = {
  inviteCode: `Enter the full ${INVITE_CODE_LENGTH}-character code`,
  displayName: 'Enter a display name',
  email: 'Enter a valid email address',
  password: `Use at least ${REGISTER_LIMITS.passwordMin} characters`,
};

function fullInviteCode(control: AbstractControl<string>): ValidationErrors | null {
  return normalizeInviteCode(control.value).length === INVITE_CODE_LENGTH
    ? null
    : { inviteCode: true };
}

function notBlank(control: AbstractControl<string>): ValidationErrors | null {
  return control.value.trim().length > 0 ? null : { required: true };
}

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink, AuthCard, Banner, Button, Field, Input, Link, Spinner],
  templateUrl: './register-page.html',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly routes = AUTH_ROUTES;
  protected readonly limits = REGISTER_LIMITS;
  protected readonly inviteCodeMaxLength = INVITE_CODE_FORMATTED_LENGTH;
  protected readonly submission = createAuthSubmission();

  protected readonly form = inject(NonNullableFormBuilder).group({
    inviteCode: ['', fullInviteCode],
    displayName: ['', [notBlank, Validators.maxLength(REGISTER_LIMITS.displayNameMax)]],
    email: ['', [Validators.required, Validators.email]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(REGISTER_LIMITS.passwordMin),
        Validators.maxLength(REGISTER_LIMITS.passwordMax),
      ],
    ],
  });

  private readonly submitted = signal(false);
  /** Server messages per field; each one clears as soon as that field is edited. */
  private readonly serverErrors = signal<Partial<Record<RegisterField, string>>>({});
  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly errors = computed(() => {
    this.formValue();
    const server = this.serverErrors();
    const result: Partial<Record<RegisterField, string>> = {};
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

  /** Only client-side problems get the summary banner; server answers sit on their field. */
  protected readonly invalidCount = computed(() => {
    this.formValue();
    return this.submitted()
      ? FIELDS.filter((field) => this.form.controls[field].invalid).length
      : 0;
  });

  protected readonly passwordCount = computed(() =>
    characterCount(this.formValue().password ?? ''),
  );

  constructor() {
    for (const field of FIELDS) {
      this.form.controls[field].valueChanges
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => this.clearServerError(field));
    }
  }

  protected formatInviteCode(): void {
    const control = this.form.controls.inviteCode;
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
      this.auth.register({
        inviteCode: normalizeInviteCode(value.inviteCode),
        displayName: value.displayName.trim(),
        email: value.email.trim(),
        password: value.password,
      }),
      {
        success: () => void this.router.navigateByUrl(AUTH_ROUTES.home),
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

  private clearServerError(field: RegisterField): void {
    if (this.serverErrors()[field] !== undefined) {
      this.serverErrors.update((errors) => {
        const remaining = { ...errors };
        delete remaining[field];
        return remaining;
      });
    }
  }

  private focusFirstInvalid(): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }
}
