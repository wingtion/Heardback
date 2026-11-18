import { Component, input } from '@angular/core';

const DEFAULT_SIZE_PX = 26;

/** The Heardback mark: a double ring with two waves, in the accent colour. Decorative. */
@Component({
  selector: 'app-logo',
  host: { class: 'inline-flex shrink-0 text-accent', 'aria-hidden': 'true' },
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="0 0 26 26"
      fill="none"
      stroke="currentColor"
      focusable="false"
    >
      <circle cx="13" cy="13" r="11.5" stroke-width="1.25" />
      <circle cx="13" cy="13" r="9" />
      <path
        d="M6.5 11c1.6-1.4 3.2-1.4 4.8 0s3.2 1.4 4.8 0 3.2-1.4 3.4 0M6.5 15c1.6-1.4 3.2-1.4 4.8 0s3.2 1.4 4.8 0 3.2-1.4 3.4 0"
        stroke-width="1.25"
        stroke-linecap="round"
      />
    </svg>
  `,
})
export class Logo {
  readonly size = input(DEFAULT_SIZE_PX);
}
