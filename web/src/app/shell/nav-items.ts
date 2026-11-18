import { IconName } from '../shared/ui/icon/icons';

export interface NavItem {
  readonly label: string;
  readonly path: string;
  readonly icon?: IconName;
}

/** App routes inside the shell. */
export const SHELL_ROUTES = {
  applications: '/applications',
  inviteCodes: '/admin/invite-codes',
} as const;

/**
 * Only pages that exist are listed (docs/design.md §8). Dashboard, CV, Companies and Settings are
 * added in the phase that builds them.
 */
export const MAIN_NAV: readonly NavItem[] = [
  { label: 'Applications', path: SHELL_ROUTES.applications },
];

export const ADMIN_NAV: readonly NavItem[] = [
  { label: 'Invite codes', path: SHELL_ROUTES.inviteCodes, icon: 'mail' },
];
