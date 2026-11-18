/** Wire types for the CV and match API (see src/Heardback.Modules.Matching). */

export interface Cv {
  readonly uploadedAt: string;
  readonly pageCount: number;
  /** The stored text, with personal details replaced by markers such as "[email]". */
  readonly text: string;
  /** Dictionary skills the CV names, e.g. "C#", ".NET". */
  readonly skills: readonly string[];
}

export type SkillCategory =
  'Language' | 'Framework' | 'Database' | 'CloudAndDevOps' | 'Testing' | 'Practice' | 'Tool';

export interface MatchSkill {
  readonly name: string;
  readonly category: SkillCategory;
}

export type MatchState = 'Ready' | 'NoCv' | 'NoPostingText' | 'NoSkillsInPosting';

export interface ApplicationMatch {
  readonly state: MatchState;
  /** 0–100, only when the state is Ready. */
  readonly score: number | null;
  readonly matched: readonly MatchSkill[];
  readonly missing: readonly MatchSkill[];
}

/** Limits the server enforces (MatchingSettings). */
export const CV_LIMITS = {
  maxBytes: 5 * 1024 * 1024,
  maxPages: 10,
  uploadsPerHour: 5,
} as const;

/** The markers the server puts where it removed personal details (ADR-0006). */
export const PERSONAL_DETAIL_MARKERS = ['[email]', '[phone]', '[link]', '[address]'] as const;

export const PDF_TYPE = 'application/pdf';
export const PDF_EXTENSION = '.pdf';
