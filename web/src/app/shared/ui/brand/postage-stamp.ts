import { Component, computed, input } from '@angular/core';

/** "auth" beside the wordmark; "envelope" in the detail panel, "envelope-compact" in the mobile sheet. */
export type PostageStampSize = 'auth' | 'envelope' | 'envelope-compact';

const SIZES: Record<
  PostageStampSize,
  { readonly box: string; readonly art: readonly [number, number] }
> = {
  auth: { box: 'h-11.5 w-9.5', art: [22, 30] },
  envelope: { box: 'h-15 w-12.5 bg-base', art: [30, 40] },
  'envelope-compact': { box: 'h-14 w-11.5 bg-base', art: [27, 36] },
};

/**
 * The postage stamp: a dotted frame, an inner border and a sun over two waves, all in the accent
 * colour (docs/design.md §6). Decorative.
 */
@Component({
  selector: 'app-postage-stamp',
  host: {
    '[class]':
      '"block shrink-0 border-2 border-dotted border-accent p-0.75 text-accent " + style().box',
    'aria-hidden': 'true',
  },
  template: `
    <span class="flex h-full items-center justify-center border border-accent">
      <svg
        [attr.width]="style().art[0]"
        [attr.height]="style().art[1]"
        viewBox="0 0 30 40"
        fill="none"
        stroke="currentColor"
        [attr.stroke-width]="size() === 'auth' ? 1.4 : 1.2"
        stroke-linecap="round"
        focusable="false"
      >
        <circle cx="15" cy="14" r="5.5" />
        <path d="M15 4.5v2M15 21.5v2M5.5 14h2M22.5 14h2" />
        @if (size() !== 'auth') {
          <path d="M8.3 7.3l1.4 1.4M20.3 19.3l1.4 1.4M8.3 20.7l1.4-1.4M20.3 8.7l1.4-1.4" />
        }
        <path d="M3 30c4-3 8-3 12 0s8 3 12 0M3 35c4-3 8-3 12 0s8 3 12 0" />
      </svg>
    </span>
  `,
})
export class PostageStamp {
  readonly size = input<PostageStampSize>('auth');

  protected readonly style = computed(() => SIZES[this.size()]);
}
