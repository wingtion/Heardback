/** Wire types for the insights API (see src/Heardback.Modules.Insights; definitions in ADR-0011). */
import { ApplicationChannel } from '../applications/applications.models';

export interface WeeklyCount {
  /** The Monday the week starts on (YYYY-MM-DD). */
  readonly weekStart: string;
  readonly applications: number;
}

export interface ChannelResponse {
  readonly channel: ApplicationChannel;
  readonly applications: number;
  readonly responded: number;
  readonly responseRatePercent: number;
}

export interface ResponseTime {
  readonly responded: number;
  readonly averageDays: number | null;
  readonly medianDays: number | null;
}

export interface Funnel {
  readonly applied: number;
  readonly interview: number;
  readonly offer: number;
  readonly rejected: number;
}

export interface Dashboard {
  readonly totalApplications: number;
  readonly responded: number;
  readonly responseRatePercent: number | null;
  readonly weeks: readonly WeeklyCount[];
  readonly channels: readonly ChannelResponse[];
  readonly responseTime: ResponseTime;
  readonly funnel: Funnel;
}

export interface FollowUp {
  readonly applicationId: string;
  readonly company: string;
  readonly role: string;
  readonly channel: ApplicationChannel;
  readonly appliedOn: string;
  readonly lastActivityOn: string;
  readonly noResponseDays: number;
}

export interface FollowUpPage {
  readonly items: readonly FollowUp[];
  readonly totalCount: number;
}

/** Follow-ups shown on the page; the rest are one click away in the filtered list. */
export const FOLLOW_UPS_SHOWN = 5;

/** The note "Mark followed up" adds; it moves the application's last activity. */
export const FOLLOWED_UP_NOTE = 'Followed up';
