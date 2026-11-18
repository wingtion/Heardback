import { Component, input } from '@angular/core';

export type SpinnerTone = 'accent' | 'current';

/**
 * Loading ring: a faint track with a turning arc. "accent" is used in loading states,
 * "current" inside busy buttons. The text next to it carries the meaning, so it is aria-hidden.
 */
@Component({
  selector: 'app-spinner',
  host: { class: 'inline-flex shrink-0', 'aria-hidden': 'true' },
  template: `
    <svg
      width="16"
      height="16"
      viewBox="0 0 16 16"
      fill="none"
      stroke-width="1.75"
      stroke-linecap="round"
      focusable="false"
      class="motion-safe:animate-spin"
    >
      @if (tone() === 'accent') {
        <circle cx="8" cy="8" r="6" class="stroke-rule" />
        <path d="M14 8a6 6 0 0 0-6-6" class="stroke-accent" />
      } @else {
        <circle cx="8" cy="8" r="6" stroke="currentColor" class="opacity-35" />
        <path d="M14 8a6 6 0 0 0-6-6" stroke="currentColor" />
      }
    </svg>
  `,
})
export class Spinner {
  readonly tone = input<SpinnerTone>('accent');
}
