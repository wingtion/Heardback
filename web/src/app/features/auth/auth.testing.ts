import { AuthResponse } from './auth.models';

/** Builds an AuthResponse for tests. */
export function authResponse(overrides: { token?: string; isAdmin?: boolean } = {}): AuthResponse {
  return {
    accessToken: overrides.token ?? 'access-token',
    accessTokenExpiresAt: '2026-11-17T12:15:00Z',
    user: {
      id: '0195a3c2-0000-7000-8000-000000000001',
      email: 'ada@example.com',
      displayName: 'Ada',
      isAdmin: overrides.isAdmin ?? false,
    },
  };
}
