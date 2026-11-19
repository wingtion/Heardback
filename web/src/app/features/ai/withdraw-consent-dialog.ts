import { A11yModule } from '@angular/cdk/a11y';
import { DialogRef } from '@angular/cdk/dialog';
import { Component, DestroyRef, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { Icon } from '../../shared/ui/icon/icon';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { AiApi } from './ai-api';
import { AiConsent } from './ai.models';

/**
 * "Stop sending your CV to AI?": the delete confirm (docs/design.md §8), because withdrawing deletes every
 * suggestion. Open it with role "alertdialog".
 */
@Component({
  selector: 'app-withdraw-consent-dialog',
  imports: [A11yModule, Banner, Button, Icon, Spinner],
  host: {
    class:
      'flex flex-col gap-3 border-rule-strong bg-envelope p-4 max-lg:rounded-t-sheet max-lg:border-t ' +
      'max-lg:pb-6 lg:w-98 lg:rounded-lg lg:border lg:shadow-dialog',
  },
  template: `
    <h2 [id]="titleId" class="flex items-center gap-2 text-body font-semibold text-ink">
      <app-icon name="warning" [size]="16" class="text-danger" />
      Stop sending your CV to AI?
    </h2>
    <p class="text-small text-ink-2">
      All suggestions made from your CV are deleted. You can allow it again from any application’s
      Suggestions.
    </p>
    @if (failed()) {
      <app-banner kind="error" heading="Couldn’t save your choice">
        The server didn’t respond. Try again.
      </app-banner>
    }
    <div class="flex gap-2.5 lg:justify-end lg:gap-2">
      <button
        appButton
        type="button"
        cdkFocusInitial
        class="max-lg:h-12 max-lg:flex-1 max-lg:text-ui-lg"
        [disabled]="saving()"
        (click)="close()"
      >
        Cancel
      </button>
      <button
        appButton="danger-solid"
        type="button"
        class="max-lg:h-12 max-lg:flex-1 max-lg:text-ui-lg"
        [disabled]="saving()"
        [attr.aria-busy]="saving() || null"
        (click)="confirm()"
      >
        @if (saving()) {
          <app-spinner tone="current" />
          Stopping…
        } @else {
          <app-icon name="trash" [size]="14" />
          Stop and delete suggestions
        }
      </button>
    </div>
  `,
})
export class WithdrawConsentDialog {
  private readonly api = inject(AiApi);
  private readonly dialogRef = inject(DialogRef);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly titleId = this.dialogRef.config.ariaLabelledBy ?? undefined;

  readonly withdrawn = output<AiConsent>();

  protected readonly saving = signal(false);
  protected readonly failed = signal(false);

  protected confirm(): void {
    this.saving.set(true);
    this.failed.set(false);
    this.dialogRef.disableClose = true;
    this.api
      .setConsent(false)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (consent) => {
          this.withdrawn.emit(consent);
          this.dialogRef.close();
        },
        error: () => {
          this.saving.set(false);
          this.dialogRef.disableClose = false;
          this.failed.set(true);
        },
      });
  }

  protected close(): void {
    this.dialogRef.close();
  }
}
