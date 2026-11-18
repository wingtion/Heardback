import { Component } from '@angular/core';

/**
 * "Copied to clipboard"-style confirmation (docs/design.md §8): check in a filled circle and the
 * projected text in the Offer colour. Place it inside a persistent role="status" region so the
 * change is announced; a region that appears together with its text is often not read out.
 */
@Component({
  selector: 'app-inline-success',
  host: { class: 'flex items-center gap-1.5 text-small font-semibold text-status-offer' },
  template: `
    <svg width="16" height="16" viewBox="0 0 16 16" aria-hidden="true" focusable="false">
      <circle cx="8" cy="8" r="6" fill="currentColor" />
      <path
        d="M5.5 8.2l1.7 1.7 3.3-3.6"
        fill="none"
        stroke-width="1.6"
        stroke-linecap="round"
        stroke-linejoin="round"
        class="stroke-base"
      />
    </svg>
    <span><ng-content /></span>
  `,
})
export class InlineSuccess {}
