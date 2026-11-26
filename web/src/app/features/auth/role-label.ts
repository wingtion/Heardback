import { CurrentUser } from './auth.models';

/** The role shown under the user's name and in Settings. */
export function roleLabel(user: CurrentUser): 'Admin' | 'Demo' | 'Member' {
  if (user.isAdmin) {
    return 'Admin';
  }
  return user.isDemo ? 'Demo' : 'Member';
}
