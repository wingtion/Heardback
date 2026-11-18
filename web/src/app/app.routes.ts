import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard } from './features/auth/auth.guards';

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
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./shell/app-shell').then((shell) => shell.AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'applications' },
      {
        path: 'applications',
        title: 'Applications · Heardback',
        loadComponent: () =>
          import('./features/applications/applications-page').then((page) => page.ApplicationsPage),
      },
      {
        path: 'admin/invite-codes',
        title: 'Invite codes · Heardback',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/invite-codes/invite-codes-page').then((page) => page.InviteCodesPage),
      },
      {
        path: 'no-access',
        title: 'No access · Heardback',
        loadComponent: () =>
          import('./features/access/no-access-page').then((page) => page.NoAccessPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
