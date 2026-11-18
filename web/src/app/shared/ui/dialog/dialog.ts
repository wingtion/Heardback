import { DialogRef } from '@angular/cdk/dialog';
import { Component, booleanAttribute, computed, inject, input } from '@angular/core';
import { Button } from '../button/button';
import { Icon } from '../icon/icon';

/**
 * Width from the lg breakpoint: "narrow" for short dialogs (392 px), "update" for Add update (410 px),
 * "form" for forms (420 px).
 */
export type DialogWidth = 'narrow' | 'update' | 'form';

const WIDTHS: Record<DialogWidth, string> = {
  narrow: 'lg:w-98',
  update: 'lg:w-102.5',
  form: 'lg:w-105',
};

/**
 * The frame of a dialog opened with DialogLauncher (docs/design.md §8): title and close button,
 * then the projected body and <app-dialog-footer>. A line under the title goes in an element marked
 * `appDialogSubtitle`. Centred from the lg breakpoint. Below it, a bottom
 * sheet with rounded top corners, or a full-screen sheet when `fullScreenOnMobile` is set (open it
 * with `mobile: 'full-screen'` too); its content should then fill the height and scroll in the middle.
 */
@Component({
  selector: 'app-dialog',
  imports: [Button, Icon],
  host: { '[class]': 'hostClass()' },
  template: `
    <div [class]="headerClass()">
      <div class="min-w-0 flex-1">
        <h2 [id]="titleId" class="font-serif text-dialog-title font-semibold text-ink">
          {{ heading() }}
        </h2>
        <ng-content select="[appDialogSubtitle]" />
      </div>
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
  readonly width = input<DialogWidth>('narrow');
  readonly fullScreenOnMobile = input(false, { transform: booleanAttribute });

  protected readonly titleId = this.dialogRef.config.ariaLabelledBy ?? undefined;

  protected readonly hostClass = computed(() => {
    const mobile = this.fullScreenOnMobile()
      ? 'max-lg:h-full'
      : 'max-lg:rounded-t-sheet max-lg:border-t';
    return `flex flex-col border-rule-strong bg-base lg:max-h-svh lg:rounded-lg lg:border lg:shadow-dialog ${mobile} ${WIDTHS[this.width()]}`;
  });

  protected readonly headerClass = computed(() =>
    this.fullScreenOnMobile()
      ? 'flex shrink-0 items-center gap-3 border-b border-rule py-2.5 pr-2 pl-4.5 lg:px-5 lg:pt-4 lg:pb-3.5'
      : 'flex items-center gap-2 px-4 pt-5 lg:gap-3 lg:border-b lg:border-rule lg:px-5 lg:pt-4 lg:pb-3.5',
  );

  protected close(): void {
    this.dialogRef.close();
  }
}
