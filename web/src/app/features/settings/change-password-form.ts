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
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { Field } from '../../shared/ui/field/field';
import { Input } from '../../shared/ui/field/input';
import { InlineSuccess } from '../../shared/ui/inline-success/inline-success';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { createAuthSubmission } from '../auth/auth-submission';
import { AuthService } from '../auth/auth.service';
import { PASSWORD_LIMITS } from '../auth/auth.models';
import { characterCount } from '../auth/password-count';

type PasswordField = 'currentPassword' | 'newPassword';
const FIELDS: readonly PasswordField[] = ['currentPassword', 'newPassword'];

const CLIENT_MESSAGES: Readonly<Record<PasswordField, string>> = {
  currentPassword: 'Enter your current password',
  newPassword: `Use at least ${PASSWORD_LIMITS.min} characters`,
};

/**
 * Change password on the Settings page (ADR-0015). The server ends every session and gives this
 * browser a new one, so the user stays signed in here. Uses the sign-in submit flow for the per-IP
 * rate limit it shares with sign in.
 */
@Component({
  selector: 'app-change-password-form',
  imports: [ReactiveFormsModule, Banner, Button, Field, Input, InlineSuccess, Spinner],
  host: { class: 'flex flex-col gap-3' },
  template: `
    <h3 class="text-label font-semibold text-ink-2">Change password</h3>

    @switch (submission.failure()?.kind) {
      @case ('rate-limited') {
        <app-banner kind="warning" heading="Too many attempts.">
          Try again in
          <span class="font-mono font-medium">{{ submission.countdown.seconds() }}</span>
          seconds.
        </app-banner>
      }
      @case ('unavailable') {
        <app-banner kind="error" heading="Couldn’t change your password">
          The server didn’t respond. Your password is unchanged. Try again.
        </app-banner>
      }
    }

    <form
      [formGroup]="form"
      (ngSubmit)="submit()"
      novalidate
      class="flex flex-col gap-3.5 lg:max-w-80"
    >
      <!-- Tells password managers which account the new password belongs to. -->
      <input type="email" autocomplete="username" hidden readonly [value]="email()" />
      <app-field label="Current password" [error]="errors().currentPassword ?? null">
        <input
          appInput
          type="password"
          formControlName="currentPassword"
          autocomplete="current-password"
          class="max-lg:h-11"
          [maxlength]="limits.max"
        />
      </app-field>

      <app-field
        label="New password"
        [hint]="limits.min + '–' + limits.max + ' characters'"
        [hintEnd]="newPasswordCount()"
        [error]="errors().newPassword ?? null"
      >
        <input
          appInput
          type="password"
          formControlName="newPassword"
          autocomplete="new-password"
          class="max-lg:h-11"
          [maxlength]="limits.max"
        />
      </app-field>

      <p class="text-caption text-ink-muted">
        This signs you out on your other devices. You stay signed in here.
      </p>

      <div>
        <button
          appButton="primary"
          type="submit"
          class="max-lg:h-11"
          [disabled]="submission.submitting() || submission.locked()"
          [attr.aria-busy]="submission.submitting() || null"
        >
          @if (submission.submitting()) {
            <app-spinner tone="current" />
            Changing password…
          } @else {
            Change password
          }
        </button>
      </div>
    </form>

    <!-- Kept on screen so "Password changed" is announced when it appears. -->
    <div role="status" class="empty:hidden">
      @if (changed()) {
        <app-inline-success
          >Password changed. Your other devices are signed out.</app-inline-success
        >
      }
    </div>
  `,
})
export class ChangePasswordForm {
  private readonly auth = inject(AuthService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly limits = PASSWORD_LIMITS;
  protected readonly email = computed(() => this.auth.user()?.email ?? '');
  protected readonly submission = createAuthSubmission();

  protected readonly form = inject(NonNullableFormBuilder).group({
    currentPassword: ['', Validators.required],
    newPassword: [
      '',
      [
        Validators.required,
        Validators.minLength(PASSWORD_LIMITS.min),
        Validators.maxLength(PASSWORD_LIMITS.max),
      ],
    ],
  });

  protected readonly changed = signal(false);
  private readonly submitted = signal(false);
  private readonly serverErrors = signal<Partial<Record<PasswordField, string>>>({});
  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  protected readonly errors = computed(() => {
    this.formValue();
    const server = this.serverErrors();
    const result: Partial<Record<PasswordField, string>> = {};
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

  protected readonly newPasswordCount = computed(() =>
    characterCount(this.formValue().newPassword ?? ''),
  );

  constructor() {
    for (const field of FIELDS) {
      this.form.controls[field].valueChanges
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => {
          this.changed.set(false);
          if (this.serverErrors()[field] !== undefined) {
            this.serverErrors.update((errors) => ({ ...errors, [field]: undefined }));
          }
        });
    }
  }

  protected submit(): void {
    this.submitted.set(true);
    this.changed.set(false);
    if (this.form.invalid) {
      this.focusFirstInvalid();
      return;
    }

    const value = this.form.getRawValue();
    this.form.disable({ emitEvent: false });
    this.submission.run(this.auth.changePassword(value), {
      success: () => {
        this.form.enable({ emitEvent: false });
        // The reset emits, which also clears the previous "Password changed"; set it after.
        this.form.reset();
        this.submitted.set(false);
        this.changed.set(true);
      },
      failed: () => {
        this.form.enable({ emitEvent: false });
        const failure = this.submission.failure();
        if (failure?.kind === 'fields') {
          this.serverErrors.set(failure.fieldErrors);
          this.focusFirstInvalid();
        }
      },
    });
  }

  private focusFirstInvalid(): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }
}
