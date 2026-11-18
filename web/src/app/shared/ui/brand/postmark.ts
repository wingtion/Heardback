import { DatePipe } from '@angular/common';
import { Component, booleanAttribute, input } from '@angular/core';

/**
 * The postmark over the envelope's stamp: three cancellation waves and a double-ring date circle in
 * `postmark` (docs/design.md §6, §8). Decorative; the date is repeated in text elsewhere.
 */
@Component({
  selector: 'app-postmark',
  imports: [DatePipe],
  host: { class: 'flex items-center text-postmark', 'aria-hidden': 'true' },
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
    <span
      class="-ml-1 flex flex-col items-center justify-center rounded-full border-3 border-double border-postmark font-mono leading-none"
      [class]="compact() ? 'size-10.5' : 'size-11.5'"
    >
      <span class="tracking-stamp uppercase" [class]="compact() ? 'text-stamp-sm' : 'text-stamp'">
        {{ date() | date: 'MMM' : 'UTC' }}
      </span>
      <span class="mt-0.5 font-medium" [class]="compact() ? 'text-caption' : 'text-small'">
        {{ date() | date: 'dd' : 'UTC' }}
      </span>
    </span>
  `,
})
export class Postmark {
  /** Calendar date, "2026-11-21". */
  readonly date = input.required<string>();
  /** The smaller version in the mobile sheet. */
  readonly compact = input(false, { transform: booleanAttribute });
}
