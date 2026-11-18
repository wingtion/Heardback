import { Component, booleanAttribute, computed, input } from '@angular/core';
import { Icon } from '../icon/icon';
import { IconName } from '../icon/icons';

export type BannerKind = 'error' | 'warning' | 'info';

interface BannerStyle {
  readonly icon: IconName;
  readonly box: string;
  readonly title: string;
  readonly body: string;
  readonly role: 'alert' | 'status';
}

const STYLES: Record<BannerKind, BannerStyle> = {
  error: {
    icon: 'warning',
    box: 'border-danger bg-danger-tint',
    title: 'text-danger',
    body: 'text-ink-2',
    role: 'alert',
  },
  warning: {
    icon: 'clock',
    box: 'border-status-no-response bg-warn-tint',
    title: 'text-status-no-response',
    body: 'text-ink',
    role: 'status',
  },
  info: {
    icon: 'info',
    box: 'border-field-border bg-surface',
    title: 'text-ink',
    body: 'text-ink-2',
    role: 'status',
  },
};

/**
 * Error, warning and info banners (docs/design.md §8). The title is optional for one-line messages;
 * extra content (a body line, a Try again button) is projected.
 */
@Component({
  selector: 'app-banner',
  imports: [Icon],
  host: {
    '[class]': '"flex gap-2.5 rounded-md border px-3 py-2.5 " + style().box',
    '[attr.role]': 'alert() ? "alert" : style().role',
  },
  template: `
    <app-icon [name]="icon() ?? style().icon" [class]="'mt-0.5 ' + iconColour()" />
    <div class="flex flex-col items-start gap-0.5">
      @if (heading(); as text) {
        <span [class]="'text-cell font-semibold ' + style().title">{{ text }}</span>
      }
      <div [class]="'text-small ' + style().body"><ng-content /></div>
    </div>
  `,
})
export class Banner {
  readonly kind = input.required<BannerKind>();
  readonly heading = input<string | null>(null);
  /** Replaces the kind's icon, e.g. refresh on the 'changed elsewhere' warning. */
  readonly icon = input<IconName | null>(null);
  /** Announces the banner at once, for a warning that blocks what the user just tried. */
  readonly alert = input(false, { transform: booleanAttribute });

  protected readonly style = computed(() => STYLES[this.kind()]);
  protected readonly iconColour = computed(() =>
    this.kind() === 'info' ? 'text-ink-2' : this.style().title,
  );
}
