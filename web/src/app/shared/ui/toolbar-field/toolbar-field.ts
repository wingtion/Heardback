import { Component, Directive, booleanAttribute, inject, input } from '@angular/core';
import { Icon } from '../icon/icon';
import { IconName } from '../icon/icons';

let nextToolbarFieldId = 0;

/**
 * Search and filter boxes above a list (docs/design.md §8 "Toolbar field group"): one bordered box with
 * a label cell on the left and the projected control on the right, without its own border. The focus
 * ring sits on the whole box. 36 px from the lg breakpoint, 44 px below it. Put appToolbarControl on
 * the control so it is labelled.
 */
@Component({
  selector: 'app-toolbar-field',
  imports: [Icon],
  host: {
    class:
      'flex h-11 min-w-0 items-center rounded-md border border-field-border bg-field ' +
      'focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-focus lg:h-9',
  },
  template: `
    <label
      [attr.for]="controlId"
      class="flex h-full shrink-0 items-center gap-1.5 text-label text-ink-2 lg:border-r lg:border-rule lg:px-2.5"
      [class]="iconOnlyOnMobile() ? 'max-lg:px-3' : 'max-lg:pr-1 max-lg:pl-2.5'"
    >
      @if (icon(); as name) {
        <app-icon [name]="name" [size]="14" class="max-lg:size-4" />
      }
      <span [class.max-lg:sr-only]="iconOnlyOnMobile()">{{ label() }}</span>
    </label>
    <ng-content />
  `,
})
export class ToolbarField {
  readonly label = input.required<string>();
  readonly icon = input<IconName | null>(null);
  /** Phones show only the icon; the label stays for screen readers (the search box). */
  readonly iconOnlyOnMobile = input(false, { transform: booleanAttribute });

  readonly controlId = `toolbar-field-${nextToolbarFieldId++}`;
}

/** The input or select inside an <app-toolbar-field>: borderless, labelled by the field. */
@Directive({
  selector: 'input[appToolbarControl], select[appToolbarControl]',
  host: {
    '[id]': 'field.controlId',
    class:
      'h-full min-w-0 flex-1 bg-transparent px-2.5 text-cell text-ink outline-none ' +
      'placeholder:text-placeholder max-lg:text-ui-lg',
  },
})
export class ToolbarControl {
  protected readonly field = inject(ToolbarField);
}
