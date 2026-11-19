/** Wire types for the applications API (see src/Heardback.Modules.Applications). */

export type ApplicationStatus = 'Applied' | 'Interview' | 'Offer' | 'Rejected';

/** Board column order (docs/design.md §14); "No response" is a label on Applied cards, not a column. */
export const APPLICATION_STATUSES: readonly ApplicationStatus[] = [
  'Applied',
  'Interview',
  'Offer',
  'Rejected',
];

export type ApplicationChannel =
  'LinkedInEasyApply' | 'CompanySite' | 'Referral' | 'DirectMessage' | 'KariyerNet' | 'Other';

/** The list's Status filter: "No response" splits Applied the way the table labels do. */
export type ApplicationListStatus = ApplicationStatus | 'NoResponse';

/** Calendar date as the API sends it, "2026-11-24". */
export type IsoDate = string;

export interface ApplicationListItem {
  readonly id: string;
  readonly company: string;
  readonly role: string;
  readonly channel: ApplicationChannel;
  readonly appliedOn: IsoDate;
  readonly status: ApplicationStatus;
  /** Days without a response; null when the "No response" label does not apply. */
  readonly noResponseDays: number | null;
  readonly updatedAt: string;
}

export interface ApplicationPage {
  readonly items: readonly ApplicationListItem[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}

export interface ApplicationEvent {
  readonly id: string;
  /** Null for a note-only update. */
  readonly status: ApplicationStatus | null;
  readonly occurredOn: IsoDate;
  readonly note: string | null;
  /** The event created with the application, dated on the applied date. */
  readonly isSubmission: boolean;
}

export interface Application {
  readonly id: string;
  readonly company: string;
  readonly role: string;
  readonly url: string | null;
  readonly postingText: string | null;
  readonly channel: ApplicationChannel;
  readonly appliedOn: IsoDate;
  readonly status: ApplicationStatus;
  readonly noResponseDays: number | null;
  /** The day "No response" started; dates the computed timeline row. */
  readonly noResponseSince: IsoDate | null;
  readonly notes: string | null;
  readonly updatedAt: string;
  readonly version: number;
  /** Newest first. */
  readonly events: readonly ApplicationEvent[];
}

export interface ApplicationSummary {
  readonly total: number;
  readonly noResponse: number;
}

export interface TrackedApplication {
  readonly id: string;
  readonly company: string;
  readonly role: string;
  readonly status: ApplicationStatus;
  readonly noResponseDays: number | null;
  readonly appliedOn: IsoDate;
}

/** One board column: at most the newest 200 cards, and the real total for "Showing 200 of 340". */
export interface BoardColumn {
  readonly status: ApplicationStatus;
  readonly totalCount: number;
  readonly items: readonly ApplicationListItem[];
}

export interface Board {
  /** In board order: Applied, Interview, Offer, Rejected. */
  readonly columns: readonly BoardColumn[];
}

/** The board's toolbar: no status filter, the columns are the statuses. */
export interface BoardFilter {
  readonly search: string;
  readonly channel: ApplicationChannel | null;
}

export interface ApplicationFilter {
  readonly search: string;
  readonly status: ApplicationListStatus | null;
  readonly channel: ApplicationChannel | null;
}

export interface CreateApplicationRequest {
  readonly company: string;
  readonly role: string;
  readonly url: string | null;
  readonly postingText: string | null;
  readonly channel: ApplicationChannel;
  readonly appliedOn: IsoDate;
}

/** Add update and board moves: a new status, a note, or both. */
export interface AddEventRequest {
  readonly status: ApplicationStatus | null;
  readonly occurredOn: IsoDate;
  readonly note: string | null;
}

/** The whole Edit form. `version` is the one the form was opened with; 409 when it changed since. */
export interface UpdateApplicationRequest extends CreateApplicationRequest {
  readonly notes: string | null;
  readonly version: number;
}

/** Limits the server enforces (PostingLimits, Application.NotesMaxLength, ApplicationEvent.NoteMaxLength). */
export const APPLICATION_LIMITS = {
  company: 200,
  role: 200,
  url: 2048,
  postingText: 20_000,
  notes: 4000,
  eventNote: 280,
  search: 200,
} as const;

/** The oldest date the server accepts (decided 2026-11-18). */
export const EARLIEST_DATE: IsoDate = '2000-01-01';

export const APPLICATIONS_PAGE_SIZE = 20;

/** Error codes from ApplicationsErrors. */
export const APPLICATION_ERROR_CODES = {
  alreadyTracked: 'applications.already_tracked',
  notFound: 'applications.not_found',
  changedElsewhere: 'applications.changed_elsewhere',
  sameStatus: 'applications.same_status',
} as const;

/** Whether the server offers sample applications to admins (on in development only). */
export interface SampleDataStatus {
  readonly enabled: boolean;
}

export interface SampleApplicationsResult {
  /** Samples added now. */
  readonly added: number;
  /** Samples the user already tracked. */
  readonly skipped: number;
}
