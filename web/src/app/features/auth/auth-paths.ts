/** API routes owned by the Identity module. The refresh cookie is scoped to AUTH_API_PREFIX. */
export const AUTH_API_PREFIX = '/api/auth';

export const AUTH_API = {
  register: `${AUTH_API_PREFIX}/register`,
  login: `${AUTH_API_PREFIX}/login`,
  refresh: `${AUTH_API_PREFIX}/refresh`,
  logout: `${AUTH_API_PREFIX}/logout`,
  me: '/api/me',
} as const;

/** App routes used by the auth guards. The pages themselves arrive after the design checkpoint. */
export const AUTH_ROUTES = {
  login: '/login',
  home: '/',
} as const;

export const RETURN_URL_PARAM = 'returnUrl';
