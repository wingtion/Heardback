import { A11yModule } from '@angular/cdk/a11y';
import { DialogRef } from '@angular/cdk/dialog';
import { Component, DestroyRef, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { Icon } from '../../shared/ui/icon/icon';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { CvApi } from './cv-api';

/**
 * "Delete your CV?": the same confirm as deleting an application (docs/design.md §8 Confirm delete). Open it
 * with role "alertdialog". Deleting when there is no CV also succeeds on the server.
 */
@Component({
  selector: 'app-delete-cv-dialog',
  imports: [A11yModule, Banner, Button, Icon, Spinner],
  host: {
    class:
      'flex flex-col gap-3 border-rule-strong bg-envelope p-4 max-lg:rounded-t-sheet max-lg:border-t ' +
      'max-lg:pb-6 lg:w-98 lg:rounded-lg lg:border lg:shadow-dialog',
  },
  template: `
    <h2 [id]="titleId" class="flex items-center gap-2 text-body font-semibold text-ink">
      <app-icon name="warning" [size]="16" class="text-danger" />
      Delete your CV?
    </h2>
    <p class="text-small text-ink-2">
      The stored text is removed for good. Matches show “No CV” until you upload one again.
    </p>
    @if (failed()) {
      <app-banner kind="error" heading="Couldn’t delete your CV">
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
          Delete CV
        }
      </button>
    </div>
  `,
})
export class DeleteCvDialog {
  private readonly api = inject(CvApi);
  private readonly dialogRef = inject(DialogRef);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly titleId = this.dialogRef.config.ariaLabelledBy ?? undefined;

  readonly deleted = output();

  protected readonly deleting = signal(false);
  protected readonly failed = signal(false);

  protected confirm(): void {
    this.deleting.set(true);
    this.failed.set(false);
    this.dialogRef.disableClose = true;
    this.api
      .delete()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.deleted.emit();
          this.dialogRef.close();
        },
        error: () => {
          this.deleting.set(false);
          this.dialogRef.disableClose = false;
          this.failed.set(true);
        },
      });
  }

  protected close(): void {
    this.dialogRef.close();
  }
}
