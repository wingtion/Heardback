import { Member, MemberPage } from './members.models';

/** Builds a Member for tests. */
export function member(overrides: Partial<Member> = {}): Member {
  return {
    id: '0195a3c2-0000-7000-8000-0000000000b1',
    displayName: 'Grace Hopper',
    email: 'grace@example.com',
    isAdmin: false,
    joinedAt: '2026-11-12T09:00:00Z',
    resetCodeExpiresAt: null,
    ...overrides,
  };
}

/** Builds a page of members for tests. */
export function memberPage(overrides: Partial<MemberPage> = {}): MemberPage {
  const items = overrides.items ?? [member()];
  return {
    items,
    page: 1,
    pageSize: 20,
    totalCount: items.length,
    totalPages: items.length === 0 ? 0 : 1,
    ...overrides,
  };
}
