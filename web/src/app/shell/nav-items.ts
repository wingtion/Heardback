import { IconName } from '../shared/ui/icon/icons';

export interface NavItem {
  readonly label: string;
  readonly path: string;
  readonly icon?: IconName;
}

/** App routes inside the shell. */
export const SHELL_ROUTES = {
  applications: '/applications',
  cv: '/cv',
  overview: '/overview',
  settings: '/settings',
  inviteCodes: '/admin/invite-codes',
} as const;

/** Only pages that exist are listed (docs/design.md §8). */
export const MAIN_NAV: readonly NavItem[] = [
  { label: 'Applications', path: SHELL_ROUTES.applications },
  { label: 'CV', path: SHELL_ROUTES.cv },
  { label: 'Overview', path: SHELL_ROUTES.overview },
  { label: 'Settings', path: SHELL_ROUTES.settings },
];

export const ADMIN_NAV: readonly NavItem[] = [
  { label: 'Invite codes', path: SHELL_ROUTES.inviteCodes, icon: 'mail' },
];
