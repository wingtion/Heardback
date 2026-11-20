import { Injectable, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { CommunityApi } from './community-api';
import { CommunityBadge } from './community.models';

/**
 * Ghost-risk badges for the user's applications, shared by the list and the board. Provided by the shell, so a
 * new sign-in never sees the previous user's badges. A failed load simply shows no badges: they are a hint, and
 * the detail's Community section has its own error state.
 */
@Injectable()
export class CommunityBadgesStore {
  private readonly api = inject(CommunityApi);

  private readonly resource = rxResource({ stream: () => this.api.badges() });

  private readonly byId = computed<ReadonlyMap<string, CommunityBadge>>(() =>
    this.resource.hasValue()
      ? new Map(this.resource.value().map((badge) => [badge.applicationId, badge]))
      : new Map(),
  );

  badgeFor(applicationId: string): CommunityBadge | null {
    return this.byId().get(applicationId) ?? null;
  }

  /** After adding, editing or deleting an application: its group may have changed. */
  reload(): void {
    this.resource.reload();
  }
}
