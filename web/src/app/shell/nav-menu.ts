import { Component, computed, inject, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../features/auth/auth.service';
import { Button } from '../shared/ui/button/button';
import { Icon } from '../shared/ui/icon/icon';
import { ADMIN_NAV, MAIN_NAV } from './nav-items';

/** "sidebar" is the desktop column; "menu" is the full-screen mobile menu with larger targets. */
export type NavMenuLayout = 'sidebar' | 'menu';

const FOCUS = 'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus';

const ITEM: Record<NavMenuLayout, string> = {
  sidebar: `flex h-8.5 items-center gap-2 rounded-sm px-2.5 text-body text-ink-2 no-underline hover:bg-nav-active ${FOCUS}`,
  menu: `flex h-12 items-center gap-2.5 rounded-md px-3.5 text-ui-xl text-ink-2 no-underline hover:bg-nav-active ${FOCUS}`,
};

const ACTIVE = 'bg-nav-active font-semibold text-ink! inset-shadow-selection';

/**
 * Navigation items, the admin section (admins only) and the signed-in user with Sign out
 * (docs/design.md §8). Used by both the desktop sidebar and the mobile menu.
 */
@Component({
  selector: 'app-nav-menu',
  imports: [RouterLink, RouterLinkActive, Button, Icon],
  host: { class: 'flex flex-1 flex-col' },
  template: `
    @for (item of mainNav; track item.path) {
      <a
        [routerLink]="item.path"
        [routerLinkActive]="activeClass"
        ariaCurrentWhenActive="page"
        [class]="itemClass()"
      >
        {{ item.label }}
        @let count = counts()[item.path];
        @if (count !== undefined) {
          <span class="ml-auto font-mono text-caption font-normal text-ink-muted">{{ count }}</span>
        }
      </a>
    }

    @if (auth.isAdmin()) {
      <div [class]="adminHeadingClass()">Admin</div>
      @for (item of adminNav; track item.path) {
        <a
          [routerLink]="item.path"
          [routerLinkActive]="activeClass"
          ariaCurrentWhenActive="page"
          [class]="itemClass()"
        >
          @if (item.icon) {
            <app-icon [name]="item.icon" [size]="layout() === 'menu' ? 17 : 15" />
          }
          {{ item.label }}
        </a>
      }
    }

    @if (auth.user(); as user) {
      <div [class]="footerClass()">
        <div class="min-w-0">
          <div
            class="truncate font-semibold text-ink"
            [class]="layout() === 'menu' ? 'text-body' : 'text-cell'"
          >
            {{ user.displayName }}
          </div>
          <div class="text-ink-muted" [class]="layout() === 'menu' ? 'text-label' : 'text-caption'">
            {{ user.isAdmin ? 'Admin' : 'Member' }}
          </div>
        </div>
        <button
          [appButton]="layout() === 'menu' ? 'secondary' : 'outline'"
          [size]="layout() === 'menu' ? 'lg' : 'sm'"
          type="button"
          class="shrink-0"
          [class.self-start]="layout() === 'sidebar'"
          [disabled]="signingOut()"
          (click)="signOut.emit()"
        >
          Sign out
        </button>
      </div>
    }
  `,
})
export class NavMenu {
  protected readonly auth = inject(AuthService);
  protected readonly mainNav = MAIN_NAV;
  protected readonly adminNav = ADMIN_NAV;

  readonly layout = input<NavMenuLayout>('sidebar');
  readonly signingOut = input(false);
  /** A count shown at the right of an item, keyed by its path (Applications, docs/design.md §8). */
  readonly counts = input<Readonly<Record<string, number>>>({});
  readonly signOut = output();

  protected readonly itemClass = computed(() => ITEM[this.layout()]);
  protected readonly activeClass = ACTIVE;
  protected readonly adminHeadingClass = computed(() =>
    this.layout() === 'menu'
      ? 'mt-3 border-t border-rule px-3.5 pt-3.5 pb-1.5 text-label font-semibold text-ink-muted'
      : 'mt-3.5 border-t border-rule px-2.5 pt-3 pb-1 text-caption font-semibold text-ink-muted',
  );
  protected readonly footerClass = computed(() =>
    this.layout() === 'menu'
      ? 'mt-auto flex items-center justify-between gap-3 border-t border-rule px-3.5 pt-4 pb-6'
      : 'mt-auto flex flex-col gap-2 border-t border-rule px-2.5 pt-3.5',
  );
}
