import { Component } from '@angular/core';

/**
 * Dialog action row (docs/design.md §8): right-aligned on a `surface` bar from the lg breakpoint;
 * on phones the buttons sit side by side at full width, so give them `max-lg:flex-1`.
 */
@Component({
  selector: 'app-dialog-footer',
  host: {
    class:
      'flex gap-2.5 px-4 pt-1 pb-6 lg:justify-end lg:gap-2 lg:rounded-b-lg lg:border-t ' +
      'lg:border-rule lg:bg-surface lg:px-5 lg:py-3.5',
  },
  template: '<ng-content />',
})
export class DialogFooter {}
