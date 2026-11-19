import { Component, computed, input } from '@angular/core';

/**
 * A thin horizontal bar showing a share of 100, for channel rates and the funnel (docs/design.md §17). SVG, so
 * the width is an attribute rather than an inline style. Decorative: the number beside it carries the meaning.
 */
@Component({
  selector: 'app-meter-bar',
  host: { class: 'block' },
  template: `
    <svg class="block h-2 w-full" viewBox="0 0 100 8" preserveAspectRatio="none" aria-hidden="true">
      <rect x="0" y="0" width="100" height="8" rx="2" class="fill-rule" />
      @if (percent() > 0) {
        <rect x="0" y="0" [attr.width]="percent()" height="8" rx="2" class="fill-ink-2" />
      }
    </svg>
  `,
})
export class MeterBar {
  /** 0 to 100. */
  readonly value = input.required<number>();

  protected readonly percent = computed(() => Math.min(100, Math.max(0, this.value())));
}
