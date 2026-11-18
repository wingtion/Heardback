import { StatusGlyph, StatusTone } from '../../shared/ui/status-label/status-label';
import {
  ApplicationChannel,
  ApplicationListStatus,
  ApplicationStatus,
  ApplicationSummary,
  IsoDate,
} from './applications.models';

/**
 * "03 Nov 2026" in tables and the envelope; "3 Nov" in timeline lines (docs/design.md §8). Calendar dates
 * ("2026-11-03") go to DatePipe without a time zone: Angular reads a date-only string as local midnight,
 * so it shows the same day everywhere. Passing "UTC" would shift it a day back east of Greenwich.
 */
export const APPLICATION_DATE_FORMAT = 'dd MMM yyyy';
export const TIMELINE_DATE_FORMAT = 'd MMM';

const DAY_MS = 24 * 60 * 60 * 1000;

export const CHANNEL_LABELS: Readonly<Record<ApplicationChannel, string>> = {
  LinkedInEasyApply: 'LinkedIn Easy Apply',
  CompanySite: 'Company site',
  Referral: 'Referral',
  DirectMessage: 'Direct message',
  KariyerNet: 'Kariyer.net',
  Other: 'Other',
};

/** Channel order in selects (PLAN.md). */
export const CHANNELS = Object.keys(CHANNEL_LABELS) as readonly ApplicationChannel[];

export interface StatusDisplay {
  readonly label: string;
  readonly tone: StatusTone;
  readonly glyph: StatusGlyph;
}

/** Status colours and glyphs from docs/design.md §3 and §6. */
export const STATUS_DISPLAY: Readonly<Record<ApplicationStatus, StatusDisplay>> = {
  Applied: { label: 'Applied', tone: 'applied', glyph: 'open' },
  Interview: { label: 'Interview', tone: 'interview', glyph: 'half' },
  Offer: { label: 'Offer', tone: 'offer', glyph: 'filled-check' },
  Rejected: { label: 'Rejected', tone: 'danger', glyph: 'cross' },
};

/** The label a row shows: "No response · 14 days" replaces Applied once it applies. */
export function statusDisplay(
  status: ApplicationStatus,
  noResponseDays: number | null,
): StatusDisplay {
  return noResponseDays === null
    ? STATUS_DISPLAY[status]
    : { label: noResponseLabel(noResponseDays), tone: 'no-response', glyph: 'clock' };
}

export function noResponseLabel(days: number): string {
  return `No response · ${days} ${days === 1 ? 'day' : 'days'}`;
}

/** Status filter options in the order the design lists them. */
export const STATUS_FILTERS: readonly {
  readonly value: ApplicationListStatus;
  readonly label: string;
}[] = [
  { value: 'Applied', label: 'Applied' },
  { value: 'NoResponse', label: 'No response' },
  { value: 'Interview', label: 'Interview' },
  { value: 'Rejected', label: 'Rejected' },
  { value: 'Offer', label: 'Offer' },
];

/** "8 applications · 2 with no response". */
export function applicationsSummary(summary: ApplicationSummary): string {
  const total = `${summary.total} ${summary.total === 1 ? 'application' : 'applications'}`;
  return `${total} · ${summary.noResponse} with no response`;
}

/** "today", "1 day ago", "14 days ago": whole days since a server timestamp. */
export function daysAgo(timestamp: string, now: Date): string {
  const days = Math.max(0, Math.floor((now.getTime() - Date.parse(timestamp)) / DAY_MS));
  if (days === 0) {
    return 'today';
  }
  return days === 1 ? '1 day ago' : `${days} days ago`;
}

/** Today's calendar date on this device, "2026-11-24": the browser checks "today or earlier" locally. */
export function localToday(now: Date): IsoDate {
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}
