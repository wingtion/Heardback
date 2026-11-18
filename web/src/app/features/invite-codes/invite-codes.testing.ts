import { InviteCode, InviteCodePage } from './invite-codes.models';

/** Builds an InviteCode for tests. */
export function inviteCode(overrides: Partial<InviteCode> = {}): InviteCode {
  return {
    id: '0195a3c2-0000-7000-8000-0000000000a1',
    code: 'K7QM-2XRD-9PLA',
    createdAt: '2026-11-24T10:00:00Z',
    expiresAt: '2026-12-01T10:00:00Z',
    status: 'Active',
    usedAt: null,
    ...overrides,
  };
}

/** Builds a page of invite codes for tests. */
export function invitePage(overrides: Partial<InviteCodePage> = {}): InviteCodePage {
  const items = overrides.items ?? [inviteCode()];
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: items.length === 0 ? 0 : 1,
    activeCount: items.filter((invite) => invite.status === 'Active').length,
    ...overrides,
  };
}
