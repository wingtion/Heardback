/** Wire types for the admin invite code API (see src/Heardback.Modules.Identity/Invites). */

export type InviteCodeStatus = 'Active' | 'Used' | 'Expired';

export interface InviteCode {
  readonly id: string;
  /** Display form, e.g. "K7QM-2XRD-9PLA". */
  readonly code: string;
  readonly createdAt: string;
  readonly expiresAt: string;
  readonly status: InviteCodeStatus;
  readonly usedAt: string | null;
}

export interface InviteCodePage {
  readonly items: readonly InviteCode[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
  /** Codes that can still be used, across all pages. */
  readonly activeCount: number;
}

export interface CreateInviteCodeRequest {
  /** Null means the server default (7 days). */
  readonly expiresInDays: number | null;
}

/** Limits the server enforces on CreateInviteCodeRequest. */
export const INVITE_EXPIRY_DAYS = { min: 1, max: 90, default: 7 } as const;

export const INVITE_CODES_PAGE_SIZE = 20;
