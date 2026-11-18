import { Directive, booleanAttribute, computed, input } from '@angular/core';

export type ButtonVariant =
  'primary' | 'secondary' | 'outline' | 'danger' | 'danger-solid' | 'ghost';
export type ButtonSize = 'sm' | 'md' | 'lg' | 'xl';

const DEFAULT_VARIANT: ButtonVariant = 'secondary';

const BASE =
  'inline-flex items-center justify-center gap-2 rounded-md border whitespace-nowrap no-underline ' +
  'cursor-pointer disabled:cursor-default focus-visible:outline-2 focus-visible:outline-offset-2 ' +
  'focus-visible:outline-focus';

// A busy button (aria-busy) is disabled but keeps its colours, so the "Signing in…" label stays readable.
const VARIANTS: Record<ButtonVariant, string> = {
  primary:
    'border-accent bg-accent text-on-accent font-semibold not-disabled:hover:border-accent-hover ' +
    'not-disabled:hover:bg-accent-hover disabled:not-aria-busy:border-rule-strong ' +
    'disabled:not-aria-busy:bg-surface disabled:not-aria-busy:text-disabled',
  secondary:
    'border-field-border bg-base text-ink not-disabled:hover:bg-secondary-hover ' +
    'disabled:border-rule disabled:text-disabled',
  outline:
    'border-field-border bg-transparent text-ink not-disabled:hover:bg-secondary-hover ' +
    'disabled:border-rule disabled:text-disabled',
  danger:
    'border-danger bg-transparent text-danger font-semibold not-disabled:hover:bg-danger-tint ' +
    'disabled:border-rule disabled:text-disabled',
  'danger-solid':
    'border-danger bg-danger text-on-danger font-semibold disabled:not-aria-busy:opacity-60',
  ghost:
    'border-transparent bg-transparent text-ink not-disabled:hover:bg-secondary-hover disabled:text-disabled',
};

const SIZES: Record<ButtonSize, { readonly text: string; readonly icon: string }> = {
  sm: { text: 'h-7.5 px-3 text-small', icon: 'size-7.5' },
  md: { text: 'h-9 px-3.5 text-cell', icon: 'size-9' },
  lg: { text: 'h-11 px-4 text-ui-md', icon: 'size-11' },
  xl: { text: 'h-12 px-4 text-ui-lg', icon: 'size-12' },
};

/**
 * Button styles from docs/design.md §8 on a native <button> or <a>, so semantics stay native.
 * Icon-only buttons set iconOnly and must carry an aria-label.
 */
@Directive({
  selector: 'button[appButton], a[appButton]',
  host: { '[class]': 'classes()' },
})
export class Button {
  // A bare `appButton` attribute binds '' and means the default variant.
  readonly variant = input<ButtonVariant, ButtonVariant | ''>(DEFAULT_VARIANT, {
    alias: 'appButton',
    transform: (value) => value || DEFAULT_VARIANT,
  });
  readonly size = input<ButtonSize>('md');
  readonly iconOnly = input(false, { transform: booleanAttribute });

  protected readonly classes = computed(() => {
    const size = SIZES[this.size()];
    return `${BASE} ${VARIANTS[this.variant()]} ${this.iconOnly() ? `${size.icon} p-0` : size.text}`;
  });
}
