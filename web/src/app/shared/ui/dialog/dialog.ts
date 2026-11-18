import { DialogRef } from '@angular/cdk/dialog';
import { Component, booleanAttribute, inject, input } from '@angular/core';
import { Button } from '../button/button';
import { Icon } from '../icon/icon';

/**
 * The frame of a dialog opened with DialogLauncher (docs/design.md §8): title and close button,
 * then the projected body and <app-dialog-footer>. 392 px and centred from the lg breakpoint;
 * a full-width bottom sheet with rounded top corners below it.
 */
@Component({
  selector: 'app-dialog',
  imports: [Button, Icon],
  host: {
    class:
      'flex flex-col border-rule-strong bg-base max-lg:rounded-t-sheet max-lg:border-t ' +
      'lg:w-98 lg:rounded-lg lg:border lg:shadow-dialog',
  },
  template: `
    <div
      class="flex items-center gap-2 px-4 pt-5 lg:gap-3 lg:border-b lg:border-rule lg:px-5 lg:pt-4 lg:pb-3.5"
    >
      <h2 [id]="titleId" class="flex-1 font-serif text-dialog-title font-semibold text-ink">
        {{ heading() }}
      </h2>
      <button
        appButton="outline"
        size="sm"
        iconOnly
        type="button"
        aria-label="Close"
        class="max-lg:size-11 max-lg:border-transparent lg:size-8"
        [disabled]="closeDisabled()"
        (click)="close()"
      >
        <app-icon name="close" [size]="14" class="text-ink-2 max-lg:text-ink" />
      </button>
    </div>
    <ng-content />
  `,
})
export class AppDialog {
  private readonly dialogRef = inject(DialogRef);

  readonly heading = input.required<string>();
  /** True while a request runs, so the dialog cannot be dismissed halfway. */
  readonly closeDisabled = input(false, { transform: booleanAttribute });

  protected readonly titleId = this.dialogRef.config.ariaLabelledBy ?? undefined;

  protected close(): void {
    this.dialogRef.close();
  }
}
