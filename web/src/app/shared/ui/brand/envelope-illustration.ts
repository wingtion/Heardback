import { Component, input } from '@angular/core';

export type EnvelopeIllustrationKind = 'empty' | 'no-access';

/**
 * Envelope drawings from the design (docs/design.md §6): "empty" has an accent stamp and is used in
 * empty states; "no-access" has a muted stamp crossed out in danger. Decorative.
 */
@Component({
  selector: 'app-envelope-illustration',
  host: { class: 'inline-flex shrink-0', 'aria-hidden': 'true' },
  template: `
    @if (kind() === 'empty') {
      <svg width="72" height="50" viewBox="0 0 72 50" fill="none" focusable="false">
        <rect
          x="1"
          y="6"
          width="62"
          height="42"
          rx="2"
          stroke-width="1.25"
          class="stroke-ink-muted"
        />
        <path d="M1 7l31 22 31-22" stroke-width="1.25" class="stroke-ink-muted" />
        <rect
          x="46"
          y="1.5"
          width="22"
          height="26"
          stroke-width="1.5"
          stroke-dasharray="1.5 2"
          class="fill-base stroke-accent"
        />
        <rect x="49.5" y="5" width="15" height="19" class="stroke-accent" />
        <circle cx="57" cy="12" r="3" class="stroke-accent" />
        <path d="M51.5 20c2-1.5 3.5-1.5 5.5 0s3.5 1.5 5.5 0" class="stroke-accent" />
      </svg>
    } @else {
      <svg width="80" height="56" viewBox="0 0 80 56" fill="none" focusable="false">
        <rect
          x="1"
          y="8"
          width="66"
          height="46"
          rx="2"
          stroke-width="1.25"
          class="stroke-ink-muted"
        />
        <path d="M1 9l33 24 33-24" stroke-width="1.25" class="stroke-ink-muted" />
        <rect
          x="52"
          y="1.5"
          width="26"
          height="30"
          stroke-width="1.5"
          stroke-dasharray="1.5 2"
          class="fill-base stroke-ink-muted"
        />
        <rect x="56" y="5.5" width="18" height="22" class="stroke-ink-muted" />
        <path
          d="M60 11l10 11M70 11L60 22"
          stroke-width="1.5"
          stroke-linecap="round"
          class="stroke-danger"
        />
      </svg>
    }
  `,
})
export class EnvelopeIllustration {
  readonly kind = input<EnvelopeIllustrationKind>('empty');
}
