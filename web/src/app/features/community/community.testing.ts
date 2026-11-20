import { Provider } from '@angular/core';
import { of } from 'rxjs';
import { CommunityApi } from './community-api';
import { CommunityBadgesStore } from './community-badges';
import { ApplicationCommunity, CommunityNumbers } from './community.models';

export const TEST_NUMBERS: CommunityNumbers = {
  members: 3,
  applications: 3,
  responded: 0,
  responseRatePercent: 0,
  averageDays: null,
  medianDays: null,
};

/** Fewer than three other members in either group: the section shows "Not enough data yet". */
export function quietCommunity(applicationId: string): ApplicationCommunity {
  return { applicationId, companyKey: 'northwind', posting: null, company: null, ghost: null };
}

/**
 * A CommunityApi with no community data and the badges store, for pages whose own tests are about something
 * else: the Community section and badges then make no HTTP calls.
 */
export function provideCommunityQuiet(): Provider[] {
  return [
    CommunityBadgesStore,
    {
      provide: CommunityApi,
      useValue: {
        forApplication: (id: string) => of(quietCommunity(id)),
        forCompany: () => of(null),
        badges: () => of([]),
      },
    },
  ];
}
