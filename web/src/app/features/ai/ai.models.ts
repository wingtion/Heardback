/** Wire types for the AI endpoints (see src/Heardback.Modules.Applications/Analysis and Matching/Tailoring). */

/** A user's use of one AI feature today; resets at midnight UTC. */
export interface AiUsage {
  readonly used: number;
  readonly limit: number;
  readonly remaining: number;
  /** ISO time of the next reset; shown in the user's own time zone. */
  readonly resetsAt: string;
}

export type PostingAnalysisState =
  | 'Unavailable'
  | 'NoPostingText'
  | 'NotRequested'
  | 'Queued'
  | 'Running'
  | 'WaitingForCapacity'
  | 'Ready'
  | 'Failed';

export type PostingSeniority = 'Unknown' | 'Intern' | 'Junior' | 'Mid' | 'Senior' | 'Lead';

export type PostingWorkMode = 'Unknown' | 'Onsite' | 'Hybrid' | 'Remote';

export interface PostingAnalysisFields {
  readonly company: string | null;
  readonly title: string | null;
  readonly seniority: PostingSeniority;
  readonly workMode: PostingWorkMode;
  readonly skills: readonly string[];
  readonly location: string | null;
  readonly analyzedAt: string;
}

export interface PostingAnalysis {
  readonly state: PostingAnalysisState;
  readonly analysis: PostingAnalysisFields | null;
  readonly usage: AiUsage;
}

export type TailoringState =
  | 'Unavailable'
  | 'NoPostingText'
  | 'NoCv'
  | 'ConsentRequired'
  | 'NotRequested'
  | 'Queued'
  | 'Running'
  | 'WaitingForCapacity'
  | 'Ready'
  | 'Failed';

export interface TailoringAdvice {
  readonly cvSection: string;
  /** A few words from the CV line the suggestion is about; the server checked it is in the CV. */
  readonly cvQuote: string;
  readonly advice: string;
}

export interface TailoringDetails {
  readonly summary: string;
  readonly suggestions: readonly TailoringAdvice[];
  /** The CV or the posting changed since; asking again writes new ones. */
  readonly isOutdated: boolean;
  readonly createdAt: string;
}

export interface Tailoring {
  readonly state: TailoringState;
  /** Also present while out-of-date suggestions are being written again. */
  readonly tailoring: TailoringDetails | null;
  readonly missingKeywords: readonly string[];
  readonly usage: AiUsage;
}

export interface AiConsent {
  readonly allowed: boolean;
  readonly allowedAt: string | null;
  /** False when AI is off on the server; every AI section is hidden then. */
  readonly available: boolean;
}

/** States in which a job is waiting or running, so the panel checks again shortly. */
export const IN_PROGRESS = ['Queued', 'Running'] as const;

/** How often a panel checks a job that is queued or running. */
export const POLL_INTERVAL_MS = 3000;
