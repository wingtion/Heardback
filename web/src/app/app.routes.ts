import { Routes } from '@angular/router';
import { adminGuard, authGuard, guestGuard } from './features/auth/auth.guards';
import { COMPANY_ROUTE } from './features/community/community-routes';

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
    path: 'reset-password',
    title: 'Reset password · Heardback',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password-page').then(
        (page) => page.ResetPasswordPage,
      ),
  },
  {
    // Public: readable before creating an account and after deleting one.
    path: 'privacy',
    title: 'Privacy · Heardback',
    loadComponent: () => import('./features/privacy/privacy-page').then((page) => page.PrivacyPage),
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
        path: COMPANY_ROUTE,
        title: 'Company · Heardback',
        loadComponent: () =>
          import('./features/community/company-page').then((page) => page.CompanyPage),
      },
      {
        path: 'cv',
        title: 'CV · Heardback',
        loadComponent: () => import('./features/cv/cv-page').then((page) => page.CvPage),
      },
      {
        path: 'overview',
        title: 'Overview · Heardback',
        loadComponent: () =>
          import('./features/insights/insights-page').then((page) => page.InsightsPage),
      },
      {
        path: 'settings',
        title: 'Settings · Heardback',
        loadComponent: () =>
          import('./features/settings/settings-page').then((page) => page.SettingsPage),
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
