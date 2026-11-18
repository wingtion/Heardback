import { Component, computed, input } from '@angular/core';
import { ICONS, IconName } from './icons';

const DEFAULT_SIZE_PX = 16;

/** A decorative stroke icon in currentColor. Icon-only buttons put their aria-label on the button. */
@Component({
  selector: 'app-icon',
  host: { class: 'inline-flex shrink-0', 'aria-hidden': 'true' },
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      [attr.viewBox]="viewBox()"
      [attr.stroke-width]="definition().strokeWidth"
      fill="none"
      stroke="currentColor"
      stroke-linecap="round"
      stroke-linejoin="round"
      focusable="false"
    >
      @for (path of definition().paths; track $index) {
        <path [attr.d]="path" />
      }
    </svg>
  `,
})
export class Icon {
  readonly name = input.required<IconName>();
  readonly size = input(DEFAULT_SIZE_PX);

  protected readonly definition = computed(() => ICONS[this.name()]);
  protected readonly viewBox = computed(() => {
    const box = this.definition().viewBox;
    return `0 0 ${box} ${box}`;
  });
}
