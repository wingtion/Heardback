import { Routes } from '@angular/router';
import { guestGuard } from './features/auth/auth.guards';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in · Heardback',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/sign-in/sign-in-page').then((page) => page.SignInPage),
  },
  {
    path: 'register',
    title: 'Create account · Heardback',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/register/register-page').then((page) => page.RegisterPage),
  },
];
