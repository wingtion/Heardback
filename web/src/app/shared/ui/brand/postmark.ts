import { Component } from '@angular/core';

/**
 * The postmark over the envelope's stamp: three cancellation waves in `postmark` (docs/design.md §6, §8).
 * The handoff also drew a dated double-ring circle; the author removed it (2026-11-18) because its 8 px
 * month was hard to read and the date is already in the timeline. Decorative.
 */
@Component({
  selector: 'app-postmark',
  host: { class: 'flex text-postmark', 'aria-hidden': 'true' },
  template: `
    <svg
      width="44"
      height="22"
      viewBox="0 0 44 22"
      fill="none"
      stroke="currentColor"
      stroke-width="1.2"
      stroke-linecap="round"
      focusable="false"
    >
      <path
        d="M2 5c3.5-3 7-3 10.5 0s7 3 10.5 0 7-3 10.5 0 5 2 8 0M2 11c3.5-3 7-3 10.5 0s7 3 10.5 0 7-3 10.5 0 5 2 8 0M2 17c3.5-3 7-3 10.5 0s7 3 10.5 0 7-3 10.5 0 5 2 8 0"
      />
    </svg>
  `,
})
export class Postmark {}
