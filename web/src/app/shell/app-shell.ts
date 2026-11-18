import { A11yModule } from '@angular/cdk/a11y';
import { Component, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import {
  AUTH_ROUTES,
  LOGIN_REASONS,
  LOGIN_REASON_PARAM,
  RETURN_URL_PARAM,
} from '../features/auth/auth-paths';
import { AuthService } from '../features/auth/auth.service';
import { SignInNavigationState } from '../features/auth/sign-in/sign-in-page';
import { Logo } from '../shared/ui/brand/logo';
import { Button } from '../shared/ui/button/button';
import { Icon } from '../shared/ui/icon/icon';
import { NavMenu } from './nav-menu';

/**
 * The signed-in frame (docs/design.md §8–9): sidebar from the lg breakpoint, header with a
 * full-screen menu below it. When the server ends the session it sends the user to sign in.
 */
@Component({
  selector: 'app-shell',
  imports: [A11yModule, RouterOutlet, Logo, Button, Icon, NavMenu],
  templateUrl: './app-shell.html',
})
export class AppShell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly menuOpen = signal(false);
  protected readonly signingOut = signal(false);

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe(() => this.menuOpen.set(false));

    effect(() => {
      const expired = this.auth.expired();
      if (expired) {
        untracked(() => this.goToSignIn(expired.email));
      }
    });
  }

  protected openMenu(): void {
    this.menuOpen.set(true);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected signOut(): void {
    this.signingOut.set(true);
    this.auth.logout().subscribe({
      complete: () => void this.router.navigateByUrl(AUTH_ROUTES.login),
    });
  }

  private goToSignIn(email: string): void {
    const state: SignInNavigationState = { email };
    void this.router.navigate([AUTH_ROUTES.login], {
      queryParams: {
        [LOGIN_REASON_PARAM]: LOGIN_REASONS.sessionExpired,
        [RETURN_URL_PARAM]: this.router.url,
      },
      state,
    });
  }
}
