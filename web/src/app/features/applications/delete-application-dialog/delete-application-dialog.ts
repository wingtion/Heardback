import { A11yModule } from '@angular/cdk/a11y';
import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { HttpStatusCode } from '@angular/common/http';
import { Component, DestroyRef, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { httpErrorOf } from '../../../core/http-errors';
import { Banner } from '../../../shared/ui/banner/banner';
import { Button } from '../../../shared/ui/button/button';
import { Icon } from '../../../shared/ui/icon/icon';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { ApplicationsApi } from '../applications-api';

export interface DeleteApplicationData {
  readonly id: string;
  readonly company: string;
}

/**
 * "Delete this application?" (docs/design.md §8 Confirm delete): an alert dialog on `envelope` that names
 * what goes, with Cancel focused first and a solid danger confirm. Open it with role "alertdialog".
 * Emits `deleted` once the application is gone, also when it was already deleted elsewhere.
 */
@Component({
  selector: 'app-delete-application-dialog',
  imports: [A11yModule, Banner, Button, Icon, Spinner],
  host: {
    class:
      'flex flex-col gap-3 border-rule-strong bg-envelope p-4 max-lg:rounded-t-sheet max-lg:border-t ' +
      'max-lg:pb-6 lg:w-98 lg:rounded-lg lg:border lg:shadow-dialog',
  },
  template: `
    <h2 [id]="titleId" class="flex items-center gap-2 text-body font-semibold text-ink">
      <app-icon name="warning" [size]="16" class="text-danger" />
      Delete this application?
    </h2>
    <p class="text-small text-ink-2">
      {{ data.company }} and its timeline will be removed. You can’t undo this.
    </p>
    @if (failed()) {
      <app-banner kind="error" heading="Couldn’t delete the application">
        The server didn’t respond. Try again.
      </app-banner>
    }
    <div class="flex gap-2.5 lg:justify-end lg:gap-2">
      <button
        appButton
        type="button"
        cdkFocusInitial
        class="max-lg:h-12 max-lg:flex-1 max-lg:text-ui-lg"
        [disabled]="deleting()"
        (click)="close()"
      >
        Cancel
      </button>
      <button
        appButton="danger-solid"
        type="button"
        class="max-lg:h-12 max-lg:flex-1 max-lg:text-ui-lg"
        [disabled]="deleting()"
        [attr.aria-busy]="deleting() || null"
        (click)="confirm()"
      >
        @if (deleting()) {
          <app-spinner tone="current" />
          Deleting…
        } @else {
          <app-icon name="trash" [size]="14" />
          Delete application
        }
      </button>
    </div>
  `,
})
export class DeleteApplicationDialog {
  private readonly api = inject(ApplicationsApi);
  private readonly dialogRef = inject(DialogRef);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly data = inject<DeleteApplicationData>(DIALOG_DATA);
  protected readonly titleId = this.dialogRef.config.ariaLabelledBy ?? undefined;

  readonly deleted = output();

  protected readonly deleting = signal(false);
  protected readonly failed = signal(false);

  protected confirm(): void {
    this.setBusy(true);
    this.api
      .delete(this.data.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.finish(),
        error: (error: unknown) => {
          // Already gone (another tab, or a double click): the outcome the user asked for.
          if (httpErrorOf(error)?.status === HttpStatusCode.NotFound) {
            this.finish();
            return;
          }
          this.setBusy(false);
          this.failed.set(true);
        },
      });
  }

  protected close(): void {
    this.dialogRef.close();
  }

  private finish(): void {
    this.deleted.emit();
    this.dialogRef.close();
  }

  private setBusy(busy: boolean): void {
    this.deleting.set(busy);
    this.dialogRef.disableClose = busy;
    if (busy) {
      this.failed.set(false);
    }
  }
}
