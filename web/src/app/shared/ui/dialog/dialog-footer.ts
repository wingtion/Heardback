import { Component, booleanAttribute, computed, input } from '@angular/core';

/**
 * Dialog action row (docs/design.md §8): right-aligned on a `surface` bar from the lg breakpoint;
 * on phones the buttons sit side by side at full width, so give them `max-lg:flex-1`. In a full-screen
 * sheet (`sticky`) the bar stays pinned at the bottom on `surface` with a top rule.
 */
@Component({
  selector: 'app-dialog-footer',
  host: { '[class]': 'hostClass()' },
  template: '<ng-content />',
})
export class DialogFooter {
  readonly sticky = input(false, { transform: booleanAttribute });

  protected readonly hostClass = computed(() => {
    const mobile = this.sticky()
      ? 'shrink-0 border-t border-rule bg-surface px-4 pt-3 pb-5'
      : 'px-4 pt-1 pb-6';
    return `flex gap-2.5 lg:justify-end lg:gap-2 lg:rounded-b-lg lg:border-t lg:border-rule lg:bg-surface lg:px-5 lg:py-3.5 ${mobile}`;
  });
}
