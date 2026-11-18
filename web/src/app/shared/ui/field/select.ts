import { Component } from '@angular/core';
import { Icon } from '../icon/icon';

/**
 * Wraps a native <select appInput> and draws the chevron at the right (docs/design.md §8). The
 * select keeps its native keyboard and screen reader behaviour.
 */
@Component({
  selector: 'app-select',
  imports: [Icon],
  host: { class: 'relative flex' },
  template: `
    <ng-content />
    <app-icon
      name="chevron-down"
      [size]="10"
      class="pointer-events-none absolute top-1/2 right-2.5 -translate-y-1/2 text-ink-2"
    />
  `,
})
export class Select {}
