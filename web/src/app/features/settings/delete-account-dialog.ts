import { A11yModule } from '@angular/cdk/a11y';
import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { Field } from '../../shared/ui/field/field';
import { Input } from '../../shared/ui/field/input';
import { Icon } from '../../shared/ui/icon/icon';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { AUTH_ROUTES, LOGIN_REASONS, LOGIN_REASON_PARAM } from '../auth/auth-paths';
import { createAuthSubmission } from '../auth/auth-submission';
import { AuthService } from '../auth/auth.service';
import { PASSWORD_LIMITS } from '../auth/auth.models';

const PASSWORD_FIELD = 'password';

/**
 * "Delete your account?" (ADR-0015): the confirm-delete box (docs/design.md §8) with the current password,
 * which the server requires. Open it with role "alertdialog". On success the session is gone, so it goes
 * to sign in, which says the account was deleted.
 */
@Component({
  selector: 'app-delete-account-dialog',
  imports: [A11yModule, ReactiveFormsModule, Banner, Button, Field, Icon, Input, Spinner],
  host: {
    class:
      'flex flex-col gap-3 border-rule-strong bg-envelope p-4 max-lg:rounded-t-sheet max-lg:border-t ' +
      'max-lg:pb-6 lg:w-105 lg:rounded-lg lg:border lg:shadow-dialog',
  },
  template: `
    <h2 [id]="titleId" class="flex items-center gap-2 text-body font-semibold text-ink">
      <app-icon name="warning" [size]="16" class="text-danger" />
      Delete your account?
    </h2>
    <p class="text-small text-ink-2">
      Your account, applications, notes, CV and AI suggestions are deleted for good. You can’t undo
      this.
    </p>

    @switch (submission.failure()?.kind) {
      @case ('last-admin') {
        <app-banner kind="error" heading="You’re the only admin">
          Heardback needs at least one admin, so this account can’t be deleted.
        </app-banner>
      }
      @case ('rate-limited') {
        <app-banner kind="warning" heading="Too many attempts.">
          Try again in
          <span class="font-mono font-medium">{{ submission.countdown.seconds() }}</span>
          seconds.
        </app-banner>
      }
      @case ('unavailable') {
        <app-banner kind="error" heading="Couldn’t delete your account">
          The server didn’t respond. Try again.
        </app-banner>
      }
    }

    <form [formGroup]="form" (ngSubmit)="confirm()" novalidate class="flex flex-col gap-3">
      <!-- Lets a password manager fill the password for this account. -->
      <input type="email" autocomplete="username" hidden readonly [value]="email" />
      <app-field label="Your password" hint="To confirm it’s you." [error]="passwordError()">
        <input
          appInput
          type="password"
          cdkFocusInitial
          formControlName="password"
          autocomplete="current-password"
          class="max-lg:h-11"
          [maxlength]="passwordMax"
        />
      </app-field>

      <div class="flex gap-2.5 lg:justify-end lg:gap-2">
        <button
          appButton
          type="button"
          class="max-lg:h-12 max-lg:flex-1 max-lg:text-ui-lg"
          [disabled]="submission.submitting()"
          (click)="close()"
        >
          Cancel
        </button>
        <button
          appButton="danger-solid"
          type="submit"
          class="max-lg:h-12 max-lg:flex-1 max-lg:text-ui-lg"
          [disabled]="submission.submitting() || submission.locked()"
          [attr.aria-busy]="submission.submitting() || null"
        >
          @if (submission.submitting()) {
            <app-spinner tone="current" />
            Deleting…
          } @else {
            <app-icon name="trash" [size]="14" />
            Delete account
          }
        </button>
      </div>
    </form>
  `,
})
export class DeleteAccountDialog {
  private readonly auth = inject(AuthService);
  private readonly dialogRef = inject(DialogRef);
  private readonly router = inject(Router);

  protected readonly titleId = this.dialogRef.config.ariaLabelledBy ?? undefined;
  protected readonly passwordMax = PASSWORD_LIMITS.max;
  protected readonly email = this.auth.user()?.email ?? '';
  protected readonly submission = createAuthSubmission();

  protected readonly form = inject(NonNullableFormBuilder).group({
    [PASSWORD_FIELD]: ['', Validators.required],
  });

  private readonly submitted = signal(false);
  private readonly serverError = signal<string | null>(null);
  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.value });

  protected readonly passwordError = computed(() => {
    this.formValue();
    if (this.serverError()) {
      return this.serverError();
    }
    return this.submitted() && this.form.invalid ? 'Enter your password' : null;
  });

  constructor() {
    this.form.controls.password.valueChanges.subscribe(() => this.serverError.set(null));
  }

  protected confirm(): void {
    this.submitted.set(true);
    if (this.form.invalid) {
      return;
    }

    this.form.disable({ emitEvent: false });
    this.dialogRef.disableClose = true;
    this.submission.run(this.auth.deleteAccount(this.form.getRawValue().password), {
      success: () => {
        void this.router.navigate([AUTH_ROUTES.login], {
          queryParams: { [LOGIN_REASON_PARAM]: LOGIN_REASONS.accountDeleted },
        });
      },
      failed: () => {
        this.form.enable({ emitEvent: false });
        this.dialogRef.disableClose = false;
        const failure = this.submission.failure();
        if (failure?.kind === 'fields') {
          this.serverError.set(failure.fieldErrors[PASSWORD_FIELD] ?? null);
        }
      },
    });
  }

  protected close(): void {
    this.dialogRef.close();
  }
}
