/** Wire types for the community API (see src/Heardback.Modules.Insights/Community; rules in ADR-0013). */
import { ApplicationStatus } from '../applications/applications.models';

export type GhostLevel = 'Low' | 'Medium' | 'High';

export type GhostReasonKind =
  | 'LongListed'
  | 'NoPostingResponse'
  | 'LowPostingResponse'
  | 'LowCompanyResponse'
  | 'SlowCompanyResponse'
  | 'RoleReposted';

export interface GhostReason {
  readonly kind: GhostReasonKind;
  readonly points: number;
  /** The plain sentence the UI shows. */
  readonly text: string;
}

export interface GhostSignal {
  readonly score: number;
  readonly maxScore: number;
  readonly level: GhostLevel;
  /** Largest point first; empty when nothing stood out. */
  readonly reasons: readonly GhostReason[];
}

/** Other members' numbers for one group; the viewer is never part of them. */
export interface CommunityNumbers {
  readonly members: number;
  readonly applications: number;
  readonly responded: number;
  readonly responseRatePercent: number;
  readonly averageDays: number | null;
  readonly medianDays: number | null;
}

export interface ApplicationCommunity {
  readonly applicationId: string;
  /** Null on applications saved before company keys existed: no company numbers or page. */
  readonly companyKey: string | null;
  /** Null when fewer than 3 other members applied to the posting. */
  readonly posting: CommunityNumbers | null;
  /** Null when fewer than 3 other members applied to the company. */
  readonly company: CommunityNumbers | null;
  /** Null when neither group has enough members. */
  readonly ghost: GhostSignal | null;
}

/** Only Medium and High get a badge. */
export interface CommunityBadge extends GhostRating {
  readonly applicationId: string;
}

export interface CommunityBadgePage {
  readonly items: readonly CommunityBadge[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}

/** A hint without its reasons, for a badge. */
export interface GhostRating {
  readonly level: GhostLevel;
  readonly score: number;
}

export interface CompanyApplication {
  readonly applicationId: string;
  readonly role: string;
  readonly status: ApplicationStatus;
  readonly appliedOn: string;
  readonly noResponseDays: number | null;
  readonly updatedAt: string;
  /** Null when there is not enough community data; Low gets no badge. */
  readonly ghost: GhostRating | null;
}

export interface CompanyCommunity {
  readonly companyKey: string;
  /** The name as the viewer last typed it. */
  readonly company: string;
  /** Null when fewer than 3 other members applied to the company. */
  readonly numbers: CommunityNumbers | null;
  readonly applications: readonly CompanyApplication[];
}

/** Every ghost-risk score is out of this many points (ADR-0013). */
export const GHOST_MAX_SCORE = 5;

/** The fewest other members a group needs before numbers show (ADR-0013); only used in copy. */
export const COMMUNITY_MINIMUM_MEMBERS = 3;
