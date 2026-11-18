import { Component, input, output } from '@angular/core';
import { Icon } from '../../../shared/ui/icon/icon';
import { IconName } from '../../../shared/ui/icon/icons';

export type ApplicationsView = 'list' | 'board';

interface ViewOption {
  readonly value: ApplicationsView;
  readonly label: string;
  readonly icon: IconName;
}

const OPTIONS: readonly ViewOption[] = [
  { value: 'list', label: 'List', icon: 'list' },
  { value: 'board', label: 'Board', icon: 'board' },
];

const BUTTON =
  'inline-flex h-11 cursor-pointer items-center gap-1.5 px-3 text-cell focus-visible:outline-2 ' +
  'focus-visible:-outline-offset-2 focus-visible:outline-focus lg:h-8.5';
const ON = 'bg-nav-active font-semibold text-ink inset-shadow-toggle';
const OFF = 'bg-base text-ink-2';

/**
 * The List / Board segmented control (docs/design.md §14): two buttons with aria-pressed, the "on" one on
 * `nav-active` with an accent bar at the bottom. Icons from the lg breakpoint; 44 px tall below it.
 */
@Component({
  selector: 'app-applications-view-toggle',
  imports: [Icon],
  host: {
    role: 'group',
    'aria-label': 'View',
    class: 'inline-flex shrink-0 overflow-hidden rounded-md border border-field-border',
  },
  template: `
    @for (option of options; track option.value; let first = $first) {
      <button
        type="button"
        [class]="buttonClass(option.value, first)"
        [attr.aria-pressed]="value() === option.value"
        (click)="select(option.value)"
      >
        <app-icon [name]="option.icon" [size]="14" class="max-lg:hidden" />
        {{ option.label }}
      </button>
    }
  `,
})
export class ApplicationsViewToggle {
  readonly value = input.required<ApplicationsView>();
  readonly valueChange = output<ApplicationsView>();

  protected readonly options = OPTIONS;

  protected buttonClass(view: ApplicationsView, first: boolean): string {
    const divider = first ? '' : 'border-l border-field-border';
    return `${BUTTON} ${divider} ${this.value() === view ? ON : OFF}`;
  }

  protected select(view: ApplicationsView): void {
    if (view !== this.value()) {
      this.valueChange.emit(view);
    }
  }
}
