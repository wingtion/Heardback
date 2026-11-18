/** Invite code shape shared with the API (InviteCode.CodeLength, groups of four joined by "-"). */
export const INVITE_CODE_LENGTH = 12;
const GROUP_LENGTH = 4;
const SEPARATOR = '-';

/** Length of a fully formatted code, e.g. "K7QM-2XRD-9PLA". */
export const INVITE_CODE_FORMATTED_LENGTH =
  INVITE_CODE_LENGTH + INVITE_CODE_LENGTH / GROUP_LENGTH - 1;

/** Upper case, letters and digits only, the way the server compares codes. */
export function normalizeInviteCode(value: string): string {
  return value
    .toUpperCase()
    .replace(/[^0-9A-Z]/g, '')
    .slice(0, INVITE_CODE_LENGTH);
}

/** Formats while the user types: dashes are added for them ("k7qm2x" → "K7QM-2X"). */
export function formatInviteCode(value: string): string {
  const groups = normalizeInviteCode(value).match(new RegExp(`.{1,${GROUP_LENGTH}}`, 'g'));
  return groups ? groups.join(SEPARATOR) : '';
}
