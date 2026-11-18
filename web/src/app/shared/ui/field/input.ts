import { Directive, booleanAttribute, computed, inject, input } from '@angular/core';
import { Field } from './field';

export type InputSize = 'md' | 'lg' | 'xl';

const BASE =
  'w-full min-w-0 rounded-md border bg-field text-ink focus:border-ink focus-visible:outline-2 ' +
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

  protected readonly classes = computed(() => {
    const size = SIZES[this.size()];
    const typography = this.code() ? CODE_TEXT : size.text;
    const border = this.field?.invalid() ? 'border-invalid' : 'border-field-border';
    return `${BASE} ${size.box} ${typography} ${border}`;
  });
}
