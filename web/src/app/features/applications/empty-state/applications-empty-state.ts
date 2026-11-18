import { Component, booleanAttribute, input, output } from '@angular/core';
import { EnvelopeIllustration } from '../../../shared/ui/brand/envelope-illustration';
import { Button } from '../../../shared/ui/button/button';
import { Icon } from '../../../shared/ui/icon/icon';

/**
 * "No applications yet" with the envelope and Add application, or, when filters are on, "No matching
 * applications" with Clear filters (docs/design.md §8, §14). Shared by the list and the board.
 */
@Component({
  selector: 'app-applications-empty-state',
  imports: [Button, EnvelopeIllustration, Icon],
  host: { class: 'flex flex-col items-start gap-2.5' },
  template: `
    @if (filtered()) {
      <h2 class="font-serif text-empty-title font-semibold text-ink">No matching applications</h2>
      <p class="text-small text-ink-2">Try another search or clear the filters.</p>
      <button appButton size="sm" type="button" class="mt-1" (click)="clearFilters.emit()">
        Clear filters
      </button>
    } @else {
      <app-envelope-illustration />
      <h2 class="font-serif text-empty-title font-semibold text-ink">No applications yet</h2>
      <p class="text-small text-ink-2">
        Add a job you applied for and Heardback will keep track of who replies.
      </p>
      <button appButton="primary" type="button" class="mt-1 h-8.5" (click)="add.emit()">
        <app-icon name="plus" [size]="13" />
        Add application
      </button>
    }
  `,
})
export class ApplicationsEmptyState {
  /** Filters are on, so "nothing" means "nothing matches". */
  readonly filtered = input(false, { transform: booleanAttribute });

  readonly add = output();
  readonly clearFilters = output();
}
