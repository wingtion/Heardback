import { Component, input } from '@angular/core';

/**
 * Status glyphs from docs/design.md §6, drawn in currentColor: open circle (Applied), half-filled
 * (Interview), filled with a check (Offer), crossed (Rejected), dashed with clock hands (No response,
 * invite Expired), centre dot (invite Active), check (invite Used), two lines (Note), dotted (No change).
 */
export type StatusGlyph =
  'open' | 'half' | 'filled-check' | 'cross' | 'clock' | 'dot' | 'check' | 'note' | 'none';

const DASHED_RING = '2.2 1.9';
const DOTTED_RING = '1.2 2.2';

/** Decorative: the status text always sits next to it. */
@Component({
  selector: 'app-status-glyph',
  host: { class: 'inline-flex shrink-0', 'aria-hidden': 'true' },
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="0 0 16 16"
      fill="none"
      focusable="false"
    >
      <circle
        cx="8"
        cy="8"
        r="5.25"
        stroke="currentColor"
        stroke-width="1.5"
        [attr.fill]="glyph() === 'filled-check' ? 'currentColor' : 'none'"
        [attr.stroke-dasharray]="ringDash()"
      />
      @switch (glyph()) {
        @case ('half') {
          <path d="M8 2.75a5.25 5.25 0 0 1 0 10.5z" fill="currentColor" />
        }
        @case ('filled-check') {
          <path
            d="M5.5 8.2l1.7 1.7 3.3-3.6"
            class="stroke-base"
            stroke-width="1.6"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        }
        @case ('cross') {
          <path
            d="M6.1 6.1l3.8 3.8M9.9 6.1l-3.8 3.8"
            stroke="currentColor"
            stroke-width="1.5"
            stroke-linecap="round"
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
        @case ('note') {
          <path
            d="M5.6 6.6h4.8M5.6 9.4h3.2"
            stroke="currentColor"
            stroke-width="1.5"
            stroke-linecap="round"
          />
        }
      }
    </svg>
  `,
})
export class StatusGlyphIcon {
  readonly glyph = input.required<StatusGlyph>();
  readonly size = input(14);

  protected ringDash(): string | null {
    switch (this.glyph()) {
      case 'clock':
        return DASHED_RING;
      case 'none':
        return DOTTED_RING;
      default:
        return null;
    }
  }
}
