import { formatInviteCode, normalizeInviteCode } from './invite-code';

describe('invite code', () => {
  it('formatInviteCode_PartialLowercaseInput_AddsDashesAndUppercases', () => {
    expect(formatInviteCode('k7qm2x')).toBe('K7QM-2X');
  });

  it('formatInviteCode_PastedCodeWithSpacesAndDashes_KeepsOnlyTwelveCharacters', () => {
    expect(formatInviteCode(' k7qm - 2xrd 9pla extra')).toBe('K7QM-2XRD-9PLA');
  });

  it('formatInviteCode_Empty_ReturnsEmpty', () => {
    expect(formatInviteCode('')).toBe('');
  });

  it('normalizeInviteCode_FormattedCode_RemovesSeparators', () => {
    expect(normalizeInviteCode('K7QM-2XRD-9PLA')).toBe('K7QM2XRD9PLA');
  });
});
