import { inviteMetaDate, inviteSummary, lifetimeText } from './invite-code-display';
import { inviteCode } from './invite-codes.testing';

describe('invite code display', () => {
  it('inviteSummary_SeveralCodes_UsesPlural', () => {
    expect(inviteSummary({ totalCount: 34, activeCount: 9 })).toBe('34 codes · 9 active');
  });

  it('inviteSummary_OneCode_UsesSingular', () => {
    expect(inviteSummary({ totalCount: 1, activeCount: 0 })).toBe('1 code · 0 active');
  });

  it('lifetimeText_SevenDayCode_SaysInSevenDays', () => {
    expect(lifetimeText(inviteCode())).toBe('in 7 days');
  });

  it('lifetimeText_OneDayCode_UsesSingular', () => {
    const invite = inviteCode({
      createdAt: '2026-11-24T10:00:00Z',
      expiresAt: '2026-11-25T10:00:00Z',
    });

    expect(lifetimeText(invite)).toBe('in 1 day');
  });

  it('inviteMetaDate_EachStatus_ShowsTheDateThatMatters', () => {
    const used = inviteCode({ status: 'Used', usedAt: '2026-11-22T09:00:00Z' });
    const expired = inviteCode({ status: 'Expired' });

    expect(inviteMetaDate(inviteCode())).toEqual({
      label: 'Expires',
      date: inviteCode().expiresAt,
    });
    expect(inviteMetaDate(used)).toEqual({ label: 'Used', date: '2026-11-22T09:00:00Z' });
    expect(inviteMetaDate(expired)).toEqual({ label: 'Expired', date: expired.expiresAt });
  });
});
