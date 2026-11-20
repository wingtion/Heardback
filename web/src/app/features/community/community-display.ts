import { CommunityNumbers, GhostLevel, GhostRating } from './community.models';

/** Shown for an answer time when nobody has heard back (docs/design.md §18). */
export const NO_TIME = '—';

export interface StatCell {
  readonly text: string;
  /** What a screen reader hears instead of "3 · 21%" or "—". */
  readonly aria: string;
}

export interface StatRow {
  readonly label: string;
  /** One cell per column, in column order. */
  readonly cells: readonly StatCell[];
}

const STAT_LABELS = [
  'Other members',
  'Applications',
  'Heard back',
  'Average days to first answer',
  'Median days to first answer',
] as const;

/** Only Medium and High get a badge; Low shows nothing in lists. */
export function showsBadge(rating: GhostRating | null): rating is GhostRating {
  return rating !== null && rating.level !== 'Low';
}

/** The line under the ghost-risk heading for Medium and High. */
export function ghostHint(level: GhostLevel): string {
  return level === 'High'
    ? 'This posting is likely inactive. Here is why:'
    : 'This posting may be inactive. Here is why:';
}

/** The five table rows for one or two groups. */
export function statRows(columns: readonly CommunityNumbers[]): readonly StatRow[] {
  const cellsFor = (pick: (numbers: CommunityNumbers) => StatCell): readonly StatCell[] =>
    columns.map(pick);
  return [
    { label: STAT_LABELS[0], cells: cellsFor((numbers) => count(numbers.members)) },
    { label: STAT_LABELS[1], cells: cellsFor((numbers) => count(numbers.applications)) },
    { label: STAT_LABELS[2], cells: cellsFor(heardBack) },
    { label: STAT_LABELS[3], cells: cellsFor((numbers) => days(numbers.averageDays)) },
    { label: STAT_LABELS[4], cells: cellsFor((numbers) => days(numbers.medianDays)) },
  ];
}

/** True when some answer time shows "—", so the caption explaining it is needed. */
export function hasMissingTime(columns: readonly CommunityNumbers[]): boolean {
  return columns.some((numbers) => numbers.averageDays === null);
}

function count(value: number): StatCell {
  return { text: String(value), aria: String(value) };
}

function heardBack(numbers: CommunityNumbers): StatCell {
  return {
    text: `${numbers.responded} · ${numbers.responseRatePercent}%`,
    aria: `${numbers.responded}, ${numbers.responseRatePercent} percent`,
  };
}

function days(value: number | null): StatCell {
  return value === null
    ? { text: NO_TIME, aria: 'No answers yet' }
    : { text: String(value), aria: `${value} days` };
}
