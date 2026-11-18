import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AUTH_ROUTES, RETURN_URL_PARAM } from './auth-paths';
import { AuthService } from './auth.service';

/** Lets signed-in users through; sends everyone else to sign in, remembering where they were going. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? true : redirectToLogin(state.url);
};

/** Sign in and registration are for signed-out users; anyone already signed in goes home. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? inject(Router).parseUrl(AUTH_ROUTES.home) : true;
};

/** Admins only. Signed-out users go to sign in; signed-in non-admins go home. */
export const adminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (!auth.isAuthenticated()) {
    return redirectToLogin(state.url);
  }

  return auth.isAdmin() ? true : inject(Router).parseUrl(AUTH_ROUTES.home);
};

function redirectToLogin(returnUrl: string) {
  return inject(Router).createUrlTree([AUTH_ROUTES.login], {
    queryParams: { [RETURN_URL_PARAM]: returnUrl },
  });
}
