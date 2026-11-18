import { Directive, ElementRef, booleanAttribute, computed, inject, input } from '@angular/core';
import { Field } from './field';

export type InputSize = 'md' | 'lg' | 'xl';

const BASE =
  'min-w-0 rounded-md border bg-field text-ink focus:border-ink focus-visible:outline-2 ' +
  'focus-visible:outline-offset-2 focus-visible:outline-focus disabled:border-rule disabled:bg-surface ' +
  'disabled:text-disabled';

const SIZES: Record<InputSize, { readonly box: string; readonly text: string }> = {
  md: { box: 'h-9 px-2.5', text: 'text-cell' },
  lg: { box: 'h-10.5 px-3', text: 'text-body' },
  xl: { box: 'h-11 px-3', text: 'text-ui-lg' },
};

const DEFAULT_SIZE: InputSize = 'md';

// Invite codes: DM Mono, wide tracking, upper case (docs/design.md §8).
const CODE_TEXT = 'font-mono text-ui-lg tracking-code-input uppercase';

// Short numbers followed by a unit, e.g. "7 days": DM Mono, 96 px wide (docs/design.md §8).
const NUMBER_TEXT = 'font-mono text-body';

const DATE_TEXT = 'font-mono text-small';

const TEXTAREA = 'TEXTAREA';
const SELECT = 'SELECT';
const TEXTAREA_BOX = 'resize-y px-2.5 py-2 text-small leading-normal';
const SELECT_BOX = 'appearance-none pr-7.5';

/**
 * Input styles from docs/design.md §8. Inside an <app-field> it takes the field's id and links its
 * hint and error for screen readers.
 */
@Directive({
  selector: 'input[appInput], select[appInput], textarea[appInput]',
  host: {
    '[class]': 'classes()',
    '[id]': 'field?.controlId',
    '[attr.aria-invalid]': 'field?.invalid() ? "true" : null',
    '[attr.aria-describedby]': 'field?.describedBy() ?? null',
  },
})
export class Input {
  protected readonly field = inject(Field, { optional: true });

  // A bare `appInput` attribute binds '' and means the default size.
  readonly size = input<InputSize, InputSize | ''>(DEFAULT_SIZE, {
    alias: 'appInput',
    transform: (value) => value || DEFAULT_SIZE,
  });

  /** Monospaced code entry, e.g. an invite code. Replaces the size's text style. */
  readonly code = input(false, { transform: booleanAttribute });

  /** A short number followed by its unit. Fixed width instead of full width. */
  readonly number = input(false, { transform: booleanAttribute });

  /** A date: DM Mono 13 px (docs/design.md §8). */
  readonly date = input(false, { transform: booleanAttribute });

  private readonly tagName = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement.tagName;

  protected readonly classes = computed(() => {
    const size = SIZES[this.size()];
    const typography = this.number()
      ? NUMBER_TEXT
      : this.code()
        ? CODE_TEXT
        : this.date()
          ? DATE_TEXT
          : size.text;
    const width = this.number() ? 'w-24' : 'w-full';
    const border = this.field?.invalid() ? 'border-invalid' : 'border-field-border';
    return `${BASE} ${width} ${this.box(size.box)} ${typography} ${border}`;
  });

  // A textarea grows with its rows; a select leaves room for the chevron that <app-select> draws.
  private box(sizeBox: string): string {
    switch (this.tagName) {
      case TEXTAREA:
        return TEXTAREA_BOX;
      case SELECT:
        return `${sizeBox} ${SELECT_BOX}`;
      default:
        return sizeBox;
    }
  }
}
