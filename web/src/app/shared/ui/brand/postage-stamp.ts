import { Component } from '@angular/core';

/**
 * The small postage stamp beside the wordmark on the auth pages: a dotted frame, an inner
 * border and a sun over two waves, all in the accent colour. Decorative.
 */
@Component({
  selector: 'app-postage-stamp',
  host: {
    class: 'block h-11.5 w-9.5 shrink-0 border-2 border-dotted border-accent p-0.75 text-accent',
    'aria-hidden': 'true',
  },
  template: `
    <span class="flex h-full items-center justify-center border border-accent">
      <svg
        width="22"
        height="30"
        viewBox="0 0 30 40"
        fill="none"
        stroke="currentColor"
        stroke-width="1.4"
        stroke-linecap="round"
        focusable="false"
      >
        <circle cx="15" cy="14" r="5.5" />
        <path d="M15 4.5v2M15 21.5v2M5.5 14h2M22.5 14h2" />
        <path d="M3 30c4-3 8-3 12 0s8 3 12 0M3 35c4-3 8-3 12 0s8 3 12 0" />
      </svg>
    </span>
  `,
})
export class PostageStamp {}
