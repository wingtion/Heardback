import { Component, booleanAttribute, computed, input, output } from '@angular/core';
import { Button } from '../button/button';
import { Icon } from '../icon/icon';

/**
 * Pagination bar for a list (docs/design.md §8). Desktop: "Showing 1–20 of 34", "Page 1 of 2",
 * Previous / Next. Phones: 44 px icon-only buttons around the "Showing" text.
 */
@Component({
  selector: 'app-pagination',
  imports: [Button, Icon],
  host: {
    role: 'navigation',
    'aria-label': 'Pagination',
    class:
      'flex items-center gap-2 border-t border-rule bg-surface px-4 pt-3 pb-5 ' +
      'lg:h-13 lg:gap-2.5 lg:rounded-b-md lg:border-t-0 lg:py-0',
  },
  template: `
    <span class="flex-1 text-center text-cell text-ink-2 tabular-nums lg:flex-none lg:text-small">
      Showing {{ range() }} of {{ totalCount() }}
    </span>
    <span class="ml-auto text-small text-ink-muted tabular-nums max-lg:hidden">
      Page {{ page() }} of {{ totalPages() }}
    </span>
    <button
      appButton
      size="sm"
      type="button"
      aria-label="Previous page"
      class="max-lg:order-first max-lg:size-11 max-lg:p-0 lg:h-8"
      [disabled]="disabled() || page() <= 1"
      (click)="pageChange.emit(page() - 1)"
    >
      <app-icon name="chevron-left" [size]="12" />
      <span class="max-lg:sr-only">Previous</span>
    </button>
    <button
      appButton
      size="sm"
      type="button"
      aria-label="Next page"
      class="max-lg:size-11 max-lg:p-0 lg:h-8"
      [disabled]="disabled() || page() >= totalPages()"
      (click)="pageChange.emit(page() + 1)"
    >
      <span class="max-lg:sr-only">Next</span>
      <app-icon name="chevron-right" [size]="12" />
    </button>
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly totalPages = input.required<number>();
  /** True while a page loads, so a second click cannot skip ahead of the response. */
  readonly disabled = input(false, { transform: booleanAttribute });

  readonly pageChange = output<number>();

  protected readonly range = computed(() => {
    const first = (this.page() - 1) * this.pageSize() + 1;
    const last = Math.min(this.page() * this.pageSize(), this.totalCount());
    return first === last ? `${first}` : `${first}–${last}`;
  });
}
