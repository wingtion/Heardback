/** Wire types for the admin member API (see src/Heardback.Modules.Identity/Members). */

export interface Member {
  readonly id: string;
  readonly displayName: string;
  readonly email: string;
  readonly isAdmin: boolean;
  readonly joinedAt: string;
  /** When the member's open reset code expires; null when there is none. */
  readonly resetCodeExpiresAt: string | null;
}

export interface MemberPage {
  readonly items: readonly Member[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}

/** A newly created reset code. Shown once; the server keeps only its hash. */
export interface ResetCode {
  /** Display form, e.g. "K7QM-2XRD-9PLA". */
  readonly code: string;
  readonly expiresAt: string;
}

export const MEMBERS_PAGE_SIZE = 20;

/** Reset codes an admin may create per hour (AccountRateLimiting.ResetCodePolicy). */
export const RESET_CODES_PER_HOUR = 10;

/** Stable error codes from the member API (ProblemDetails "code"). */
export const MEMBER_ERROR_CODES = {
  memberNotFound: 'admin.member_not_found',
  resetAdmin: 'admin.reset_admin',
} as const;
