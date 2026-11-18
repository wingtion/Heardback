import { Clipboard } from '@angular/cdk/clipboard';
import { DialogRef } from '@angular/cdk/dialog';
import { DatePipe } from '@angular/common';
import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
} from '@angular/forms';
import { Router } from '@angular/router';
import { httpErrorOf, isForbidden, validationErrors } from '../../../core/http-errors';
import { AUTH_ROUTES } from '../../auth/auth-paths';
import { Banner } from '../../../shared/ui/banner/banner';
import { Button } from '../../../shared/ui/button/button';
import { AppDialog } from '../../../shared/ui/dialog/dialog';
import { DialogFooter } from '../../../shared/ui/dialog/dialog-footer';
import { Field } from '../../../shared/ui/field/field';
import { Input } from '../../../shared/ui/field/input';
import { Icon } from '../../../shared/ui/icon/icon';
import { InlineSuccess } from '../../../shared/ui/inline-success/inline-success';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { INVITE_DATE_FORMAT, lifetimeText } from '../invite-code-display';
import { InviteCodesApi } from '../invite-codes-api';
import { INVITE_EXPIRY_DAYS, InviteCode } from '../invite-codes.models';

const WHOLE_NUMBER = /^\d+$/;
const EXPIRY_FIELD = 'expiresInDays';
const EXPIRY_ERROR = `Enter a whole number of days from ${INVITE_EXPIRY_DAYS.min} to ${INVITE_EXPIRY_DAYS.max}`;

/** Empty is allowed (the server default applies); anything else must be a whole number in range. */
function expiryDaysValidator(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value.trim();
  if (value === '') {
    return null;
  }
  const days = Number(value);
  return WHOLE_NUMBER.test(value) &&
    days >= INVITE_EXPIRY_DAYS.min &&
    days <= INVITE_EXPIRY_DAYS.max
    ? null
    : { expiryDays: true };
}

/**
 * Create invite code, then show the new code with Copy (docs/design.md §10). Both steps live in one
 * dialog so focus stays inside it when the step changes. Emits `created` as soon as the code exists,
 * so the table behind can show it even if the dialog is dismissed with Escape.
 */
@Component({
  selector: 'app-invite-code-dialog',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    AppDialog,
    Banner,
    Button,
    DialogFooter,
    Field,
    Icon,
    InlineSuccess,
    Input,
    Spinner,
  ],
  templateUrl: './invite-code-dialog.html',
})
export class InviteCodeDialog {
  private readonly api = inject(InviteCodesApi);
  private readonly dialogRef = inject(DialogRef);
  private readonly clipboard = inject(Clipboard);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly created = output<InviteCode>();

  protected readonly dateFormat = INVITE_DATE_FORMAT;
  protected readonly hint = `${INVITE_EXPIRY_DAYS.min}–${INVITE_EXPIRY_DAYS.max} days. Leave as ${INVITE_EXPIRY_DAYS.default} if you’re not sure.`;

  protected readonly form = inject(NonNullableFormBuilder).group({
    [EXPIRY_FIELD]: [String(INVITE_EXPIRY_DAYS.default), expiryDaysValidator],
  });

  protected readonly invite = signal<InviteCode | null>(null);
  protected readonly submitting = signal(false);
  protected readonly unavailable = signal(false);
  protected readonly copied = signal(false);

  private readonly submitted = signal(false);
  private readonly serverFieldError = signal<string | null>(null);
  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.value });

  private readonly copyButton = viewChild<ElementRef<HTMLButtonElement>>('copyButton');

  protected readonly expiryError = computed(() => {
    this.formValue();
    if (this.serverFieldError()) {
      return this.serverFieldError();
    }
    return this.submitted() && this.form.invalid ? EXPIRY_ERROR : null;
  });

  protected readonly lifetime = computed(() => {
    const invite = this.invite();
    return invite ? lifetimeText(invite) : '';
  });

  protected create(): void {
    this.submitted.set(true);
    this.serverFieldError.set(null);
    if (this.form.invalid || this.submitting()) {
      return;
    }

    const value = this.form.getRawValue()[EXPIRY_FIELD].trim();
    this.setBusy(true);
    this.api
      .create({ expiresInDays: value === '' ? null : Number(value) })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (invite) => {
          this.setBusy(false);
          this.invite.set(invite);
          this.created.emit(invite);
          afterNextRender(() => this.copyButton()?.nativeElement.focus(), {
            injector: this.injector,
          });
        },
        error: (error: unknown) => {
          this.setBusy(false);
          this.handleCreateError(error);
        },
      });
  }

  protected copy(code: string): void {
    this.copied.set(this.clipboard.copy(code));
  }

  protected close(): void {
    this.dialogRef.close();
  }

  private setBusy(busy: boolean): void {
    this.submitting.set(busy);
    this.dialogRef.disableClose = busy;
    if (busy) {
      this.unavailable.set(false);
      this.form.disable();
    } else {
      this.form.enable();
    }
  }

  private handleCreateError(error: unknown): void {
    if (isForbidden(error)) {
      this.dialogRef.close();
      void this.router.navigateByUrl(AUTH_ROUTES.noAccess, { skipLocationChange: true });
      return;
    }

    // The server's own message is generic, so the field shows the same copy as the client check.
    if (EXPIRY_FIELD in validationErrors(httpErrorOf(error)?.error)) {
      this.serverFieldError.set(EXPIRY_ERROR);
    } else {
      this.unavailable.set(true);
    }
  }
}
