import { Directive, computed, inject, input } from '@angular/core';
import { Field } from './field';

export type InputSize = 'md' | 'lg' | 'xl';

const BASE =
  'w-full min-w-0 rounded-md border bg-field text-ink focus:border-ink focus-visible:outline-2 ' +
  'focus-visible:outline-offset-2 focus-visible:outline-focus disabled:border-rule disabled:bg-surface ' +
  'disabled:text-disabled';

const SIZES: Record<InputSize, string> = {
  md: 'h-9 px-2.5 text-cell',
  lg: 'h-10.5 px-3 text-body',
  xl: 'h-11 px-3 text-ui-lg',
};

const DEFAULT_SIZE: InputSize = 'md';

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

  protected readonly classes = computed(() => {
    const border = this.field?.invalid() ? 'border-invalid' : 'border-field-border';
    return `${BASE} ${SIZES[this.size()]} ${border}`;
  });
}
