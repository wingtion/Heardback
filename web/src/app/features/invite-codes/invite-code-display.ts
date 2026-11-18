import { StatusGlyph, StatusTone } from '../../shared/ui/status-label/status-label';
import { InviteCode, InviteCodePage, InviteCodeStatus } from './invite-codes.models';

/** Date formats from the design: "24 Nov 2026" in tables, "24 Nov" in the mobile list. */
export const INVITE_DATE_FORMAT = 'dd MMM yyyy';
export const INVITE_SHORT_DATE_FORMAT = 'dd MMM';

const DAY_MS = 24 * 60 * 60 * 1000;

export interface InviteStatusDisplay {
  readonly label: string;
  readonly tone: StatusTone;
  readonly glyph: StatusGlyph;
}

/** Status colours and glyphs from docs/design.md §3 and §6. */
export const INVITE_STATUS_DISPLAY: Readonly<Record<InviteCodeStatus, InviteStatusDisplay>> = {
  Active: { label: 'Active', tone: 'offer', glyph: 'dot' },
  Used: { label: 'Used', tone: 'applied', glyph: 'check' },
  Expired: { label: 'Expired', tone: 'no-response', glyph: 'clock' },
};

/** The one date the mobile list shows next to the status: "Expires 01 Dec", "Used 22 Nov". */
export function inviteMetaDate(invite: InviteCode): {
  readonly label: string;
  readonly date: string;
} {
  switch (invite.status) {
    case 'Used':
      return { label: 'Used', date: invite.usedAt ?? invite.expiresAt };
    case 'Expired':
      return { label: 'Expired', date: invite.expiresAt };
    case 'Active':
      return { label: 'Expires', date: invite.expiresAt };
  }
}

/**
 * "in 7 days" for a freshly created code. Uses the server's own timestamps, so a wrong clock on
 * this device cannot change it.
 */
export function lifetimeText(invite: InviteCode): string {
  const days = Math.round((Date.parse(invite.expiresAt) - Date.parse(invite.createdAt)) / DAY_MS);
  return `in ${days} ${days === 1 ? 'day' : 'days'}`;
}

/** "34 codes · 9 active". */
export function inviteSummary(page: Pick<InviteCodePage, 'totalCount' | 'activeCount'>): string {
  const codes = `${page.totalCount} ${page.totalCount === 1 ? 'code' : 'codes'}`;
  return `${codes} · ${page.activeCount} active`;
}
