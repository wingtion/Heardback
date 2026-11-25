import { Clipboard } from '@angular/cdk/clipboard';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { DatePipe } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { httpErrorOf, problemCode } from '../../core/http-errors';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { CodeDisplay } from '../../shared/ui/code-display/code-display';
import { AppDialog } from '../../shared/ui/dialog/dialog';
import { DialogFooter } from '../../shared/ui/dialog/dialog-footer';
import { Icon } from '../../shared/ui/icon/icon';
import { InlineSuccess } from '../../shared/ui/inline-success/inline-success';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { retryAfterSeconds } from '../auth/auth-errors';
import { AUTH_ROUTES } from '../auth/auth-paths';
import { MembersApi } from './members-api';
import { MEMBER_ERROR_CODES, Member, RESET_CODES_PER_HOUR, ResetCode } from './members.models';

/** "26 Nov 2026, 14:30" in the admin's own time zone, so they can tell the member when it ends. */
export const RESET_CODE_EXPIRY_FORMAT = 'dd MMM yyyy, HH:mm';

const SECONDS_PER_MINUTE = 60;

type CreateProblem =
  | { readonly kind: 'gone' }
  | { readonly kind: 'admin' }
  | { readonly kind: 'limit'; readonly minutes: number }
  | { readonly kind: 'unavailable' };

export interface ResetCodeDialogData {
  readonly member: Member;
}

/**
 * Create a password reset code for a member, then show it once with Copy (ADR-0015, docs/design.md §20).
 * The first step says what the code does, because it lets whoever holds it into the member's account.
 * Emits `created` as soon as the code exists and `gone` when the member no longer exists, so the list
 * behind can update.
 */
@Component({
  selector: 'app-reset-code-dialog',
  imports: [
    DatePipe,
    AppDialog,
    Banner,
    Button,
    CodeDisplay,
    DialogFooter,
    Icon,
    InlineSuccess,
    Spinner,
  ],
  templateUrl: './reset-code-dialog.html',
})
export class ResetCodeDialog {
  private readonly api = inject(MembersApi);
  private readonly dialogRef = inject(DialogRef);
  private readonly clipboard = inject(Clipboard);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  protected readonly member = inject<ResetCodeDialogData>(DIALOG_DATA).member;
  protected readonly expiryFormat = RESET_CODE_EXPIRY_FORMAT;
  protected readonly perHour = RESET_CODES_PER_HOUR;

  readonly created = output<ResetCode>();
  readonly gone = output();

  protected readonly resetCode = signal<ResetCode | null>(null);
  protected readonly creating = signal(false);
  protected readonly problem = signal<CreateProblem | null>(null);
  protected readonly copied = signal(false);

  private readonly copyButton = viewChild<ElementRef<HTMLButtonElement>>('copyButton');

  protected create(): void {
    if (this.creating()) {
      return;
    }
    this.setBusy(true);
    this.problem.set(null);
    this.api
      .createResetCode(this.member.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (code) => {
          this.setBusy(false);
          this.resetCode.set(code);
          this.created.emit(code);
          afterNextRender(() => this.copyButton()?.nativeElement.focus(), {
            injector: this.injector,
          });
        },
        error: (error: unknown) => {
          this.setBusy(false);
          this.handleError(error);
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
    this.creating.set(busy);
    this.dialogRef.disableClose = busy;
  }

  private handleError(error: unknown): void {
    const response = httpErrorOf(error);
    const code = problemCode(response?.error);
    if (response?.status === HttpStatusCode.NotFound) {
      this.problem.set({ kind: 'gone' });
      this.gone.emit();
    } else if (code === MEMBER_ERROR_CODES.resetAdmin) {
      this.problem.set({ kind: 'admin' });
    } else if (response?.status === HttpStatusCode.Forbidden) {
      // Admin rights were removed while the page was open.
      this.dialogRef.close();
      void this.router.navigateByUrl(AUTH_ROUTES.noAccess, { skipLocationChange: true });
    } else if (response?.status === HttpStatusCode.TooManyRequests) {
      this.problem.set({
        kind: 'limit',
        minutes: Math.max(1, Math.ceil(retryAfterSeconds(response) / SECONDS_PER_MINUTE)),
      });
    } else {
      this.problem.set({ kind: 'unavailable' });
    }
  }
}
