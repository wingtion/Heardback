import { Component, computed, input } from '@angular/core';

/** Status colours from docs/design.md §3. */
export type StatusTone = 'applied' | 'no-response' | 'interview' | 'offer' | 'danger';

/**
 * Status glyphs from docs/design.md §6. Only the ones a built screen uses exist; the application
 * glyphs (open circle, half-filled, crossed) are added with the applications list.
 */
export type StatusGlyph = 'dot' | 'check' | 'clock';

// Full class names so Tailwind finds them in the source.
const TONES: Record<StatusTone, string> = {
  applied: 'border-status-applied text-status-applied',
  'no-response': 'border-status-no-response text-status-no-response',
  interview: 'border-status-interview text-status-interview',
  offer: 'border-status-offer text-status-offer',
  danger: 'border-danger text-danger',
};

const DASHED_RING = '2.2 1.9';

/**
 * Table and list form of a status (docs/design.md §8): glyph and plain text in the status colour
 * inside a fine border. Colour is never the only signal, so the text is always shown.
 */
@Component({
  selector: 'app-status-label',
  host: {
    '[class]':
      '"inline-flex h-5.5 items-center gap-1.5 rounded-sm border pr-2 pl-1.5 text-label font-medium whitespace-nowrap " + toneClass()',
  },
  template: `
    <svg
      width="14"
      height="14"
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      focusable="false"
    >
      <circle
        cx="8"
        cy="8"
        r="5.25"
        stroke="currentColor"
        stroke-width="1.5"
        [attr.stroke-dasharray]="glyph() === 'clock' ? dashedRing : null"
      />
      @switch (glyph()) {
        @case ('dot') {
          <circle cx="8" cy="8" r="2.4" fill="currentColor" />
        }
        @case ('check') {
          <path
            d="M5.6 8.1l1.7 1.7 3.2-3.5"
            stroke="currentColor"
            stroke-width="1.4"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        }
        @case ('clock') {
          <path
            d="M8 5.3V8l1.9 1.3"
            stroke="currentColor"
            stroke-width="1.4"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        }
      }
    </svg>
    <span>{{ label() }}</span>
  `,
})
export class StatusLabel {
  readonly label = input.required<string>();
  readonly tone = input.required<StatusTone>();
  readonly glyph = input.required<StatusGlyph>();

  protected readonly dashedRing = DASHED_RING;
  protected readonly toneClass = computed(() => TONES[this.tone()]);
}
