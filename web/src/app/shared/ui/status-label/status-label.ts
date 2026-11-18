import { Component, booleanAttribute, computed, input } from '@angular/core';
import { StatusGlyph, StatusGlyphIcon } from './status-glyph';

export type { StatusGlyph } from './status-glyph';

/** Status colours from docs/design.md §3. */
export type StatusTone = 'applied' | 'no-response' | 'interview' | 'offer' | 'danger';

// Full class names so Tailwind finds them in the source.
const TONES: Record<StatusTone, string> = {
  applied: 'border-status-applied text-status-applied',
  'no-response': 'border-status-no-response text-status-no-response',
  interview: 'border-status-interview text-status-interview',
  offer: 'border-status-offer text-status-offer',
  danger: 'border-danger text-danger',
};

const LABEL =
  'inline-flex items-center gap-1.5 border pr-2 pl-1.5 text-label font-medium whitespace-nowrap';

/**
 * A status (docs/design.md §8): glyph and plain text in the status colour inside a fine border.
 * The postmark form, used only in the detail panel and sheet, adds a dotted outer border.
 * Colour is never the only signal, so the text is always shown.
 */
@Component({
  selector: 'app-status-label',
  imports: [StatusGlyphIcon],
  host: { '[class]': 'hostClass()' },
  template: `
    <span [class]="labelClass()">
      <app-status-glyph [glyph]="glyph()" />
      <span>{{ label() }}</span>
    </span>
  `,
})
export class StatusLabel {
  readonly label = input.required<string>();
  readonly tone = input.required<StatusTone>();
  readonly glyph = input.required<StatusGlyph>();
  readonly postmark = input(false, { transform: booleanAttribute });

  protected readonly hostClass = computed(() =>
    this.postmark()
      ? `inline-flex rounded-xs border-dotted-thin p-0.5 ${TONES[this.tone()]}`
      : 'inline-flex',
  );

  protected readonly labelClass = computed(() =>
    this.postmark()
      ? `${LABEL} h-5 ${TONES[this.tone()]}`
      : `${LABEL} h-5.5 rounded-sm ${TONES[this.tone()]}`,
  );
}
