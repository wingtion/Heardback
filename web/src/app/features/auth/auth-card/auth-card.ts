import { Component, input } from '@angular/core';
import { Logo } from '../../../shared/ui/brand/logo';
import { PostageStamp } from '../../../shared/ui/brand/postage-stamp';

export type AuthCardWidth = 'default' | 'wide';

/**
 * Frame for the sign in and create account pages (docs/design.md §9): a card centred on the
 * surface colour from the sm breakpoint, full-width content on small phones. The wordmark and the
 * postage stamp sit on top; the page puts its heading, form and footer inside.
 */
@Component({
  selector: 'app-auth-card',
  imports: [Logo, PostageStamp],
  host: { class: 'flex min-h-dvh justify-center bg-base sm:items-center sm:bg-surface sm:p-6' },
  template: `
    <main
      class="flex w-full flex-col px-6 pt-12 pb-6 sm:rounded-xl sm:border sm:border-rule-strong sm:bg-base sm:p-8"
      [class]="width() === 'wide' ? 'gap-4.5 sm:w-105' : 'gap-5 sm:w-100'"
    >
      <div class="flex items-center justify-between gap-3">
        <div class="flex items-center gap-2.25">
          <app-logo />
          <span class="font-serif text-wordmark font-semibold text-ink">Heardback</span>
        </div>
        <app-postage-stamp />
      </div>
      <ng-content />
    </main>
  `,
})
export class AuthCard {
  readonly width = input<AuthCardWidth>('default');
}
