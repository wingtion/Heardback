import { Component } from '@angular/core';
import { Logo } from '../shared/ui/brand/logo';
import { Spinner } from '../shared/ui/spinner/spinner';

/** Full-page screen shown while the session is restored on reload (docs/design.md §8). */
@Component({
  selector: 'app-session-loading',
  imports: [Logo, Spinner],
  host: {
    class: 'fixed inset-0 z-50 flex flex-col items-center justify-center gap-4 bg-base lg:gap-4.5',
    role: 'status',
    'aria-live': 'polite',
  },
  template: `
    <div class="flex items-center gap-2.5 lg:gap-3">
      <app-logo [size]="32" class="lg:hidden" />
      <app-logo [size]="40" class="max-lg:hidden" />
      <span
        class="font-serif text-display-sm font-semibold tracking-title text-ink lg:text-display"
      >
        Heardback
      </span>
    </div>
    <div class="flex items-center gap-2 text-body text-ink-2">
      <app-spinner />
      Restoring your session…
    </div>
  `,
})
export class SessionLoading {}
