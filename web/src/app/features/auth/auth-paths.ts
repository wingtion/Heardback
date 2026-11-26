/** API routes owned by the Identity module. The refresh cookie is scoped to AUTH_API_PREFIX. */
export const AUTH_API_PREFIX = '/api/auth';

export const AUTH_API = {
  register: `${AUTH_API_PREFIX}/register`,
  login: `${AUTH_API_PREFIX}/login`,
  refresh: `${AUTH_API_PREFIX}/refresh`,
  logout: `${AUTH_API_PREFIX}/logout`,
  resetPassword: `${AUTH_API_PREFIX}/reset-password`,
  demo: `${AUTH_API_PREFIX}/demo`,
  me: '/api/me',
} as const;

/** The signed-in user's own account (src/Heardback.Modules.Identity/Endpoints/AccountEndpoints.cs). */
export const ACCOUNT_API = {
  account: '/api/account',
  password: '/api/account/password',
  export: '/api/account/export',
} as const;

/** App routes for signing in and out. */
export const AUTH_ROUTES = {
  login: '/login',
  register: '/register',
  resetPassword: '/reset-password',
  privacy: '/privacy',
  home: '/',
  noAccess: '/no-access',
} as const;

export const RETURN_URL_PARAM = 'returnUrl';

/** Query parameter on the login route saying why the user was sent there. */
export const LOGIN_REASON_PARAM = 'reason';
export const LOGIN_REASONS = {
  sessionExpired: 'session-expired',
  passwordReset: 'password-reset',
  accountDeleted: 'account-deleted',
  demoEnded: 'demo-ended',
} as const;

/**
 * Only same-app paths are followed after sign in, so a crafted link cannot send the user to
 * another site ("//evil.example", "/\evil.example", "https://…").
 */
export function safeReturnUrl(value: string | null | undefined): string {
  if (!value || !value.startsWith('/') || value.startsWith('//') || value.startsWith('/\\')) {
    return AUTH_ROUTES.home;
  }
  return value;
}
