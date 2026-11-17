/** Wire types for the Identity API (see src/Heardback.Modules.Identity/Endpoints). */

export interface CurrentUser {
  readonly id: string;
  readonly email: string;
  readonly displayName: string;
  readonly isAdmin: boolean;
}

export interface AuthResponse {
  readonly accessToken: string;
  readonly accessTokenExpiresAt: string;
  readonly user: CurrentUser;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface RegisterRequest {
  readonly email: string;
  readonly password: string;
  readonly displayName: string;
  readonly inviteCode: string;
}
