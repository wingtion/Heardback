import { inject } from '@angular/core';
import { CanActivateFn, RedirectCommand, Router } from '@angular/router';
import { AUTH_ROUTES, RETURN_URL_PARAM } from './auth-paths';
import { AuthService } from './auth.service';

// Every guard first waits for the startup session restore, so a reload never flashes the wrong page.

/** Lets signed-in users through; sends everyone else to sign in, remembering where they were going. */
export const authGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  await auth.whenRestored();
  return auth.isAuthenticated() ? true : loginWithReturnUrl(router, state.url);
};

/** Sign in and registration are for signed-out users; anyone already signed in goes home. */
export const guestGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  await auth.whenRestored();
  return auth.isAuthenticated() ? router.parseUrl(AUTH_ROUTES.home) : true;
};

/**
 * Admins only. Signed-out users go to sign in. Signed-in members see the "no access" page while the
 * address bar keeps the URL they opened. The API enforces the same rule; this is only the UI.
 */
export const adminGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  await auth.whenRestored();
  if (!auth.isAuthenticated()) {
    return loginWithReturnUrl(router, state.url);
  }

  return auth.isAdmin()
    ? true
    : new RedirectCommand(router.parseUrl(AUTH_ROUTES.noAccess), { skipLocationChange: true });
};

function loginWithReturnUrl(router: Router, returnUrl: string) {
  return router.createUrlTree([AUTH_ROUTES.login], {
    queryParams: { [RETURN_URL_PARAM]: returnUrl },
  });
}
