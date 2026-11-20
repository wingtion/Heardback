import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { Icon } from '../../shared/ui/icon/icon';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { FROM_APPLICATION_PARAM, companyPath } from './community-routes';
import { NumbersColumn, CommunityNumbersTable } from './community-numbers';
import { COMMUNITY_MINIMUM_MEMBERS, ApplicationCommunity } from './community.models';
import { ghostHint } from './community-display';
import { CommunityApi } from './community-api';
import { GhostWaves } from './ghost-waves';

type SectionView =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'not-enough'; readonly community: ApplicationCommunity }
  | {
      readonly kind: 'numbers';
      readonly community: ApplicationCommunity;
      readonly columns: readonly NumbersColumn[];
      /** Why only one column shows, when only one group has enough members. */
      readonly missingGroup: string | null;
    };

/**
 * "Community" in the application detail (docs/design.md §18, ADR-0013): the ghost-risk box with every reason,
 * other members' numbers for the posting and the company, the privacy line and the link to the company page.
 * Reloads when the application's version changes (after Edit or Add update).
 */
@Component({
  selector: 'app-community-section',
  imports: [
    NgTemplateOutlet,
    RouterLink,
    Banner,
    Button,
    CommunityNumbersTable,
    GhostWaves,
    Icon,
    Spinner,
  ],
  host: {
    class: 'flex flex-col gap-3 border-t border-dashed border-rule-strong pt-3',
    role: 'region',
    'aria-labelledby': 'community-title',
  },
  template: `
    <div>
      <h3 id="community-title" class="text-body font-semibold text-ink">
        Stats from other members
      </h3>
      <p class="text-caption text-ink-muted">Members who applied to the same posting or company.</p>
    </div>

    @let current = view();
    @switch (current.kind) {
      @case ('loading') {
        <div role="status" class="flex flex-col gap-2.5 rounded-sm border border-rule bg-base p-3">
          <span class="flex items-center gap-2 text-small text-ink-2">
            <app-spinner />
            Loading community numbers…
          </span>
          <span aria-hidden="true" class="h-2.25 w-7/10 rounded-xs bg-skeleton"></span>
          <span aria-hidden="true" class="h-2 w-9/10 rounded-xs bg-skeleton"></span>
          <span aria-hidden="true" class="h-2 w-11/20 rounded-xs bg-skeleton"></span>
        </div>
      }
      @case ('error') {
        <app-banner kind="error" heading="Couldn’t load community numbers">
          <p>Your own application details are fine.</p>
          <button appButton size="sm" type="button" class="mt-1 max-lg:h-11" (click)="retry()">
            Try again
          </button>
        </app-banner>
      }
      @case ('not-enough') {
        <div
          class="flex flex-col gap-1 rounded-md border-thick border-dashed border-rule-strong p-3"
        >
          <span class="text-cell font-semibold text-ink">Not enough data yet</span>
          <span class="text-small text-ink-2">
            Fewer than {{ minimum }} other members have applied to this posting. To keep everyone
            anonymous, no numbers are shown until at least {{ minimum }} have.
          </span>
        </div>
        <ng-container
          [ngTemplateOutlet]="companyLink"
          [ngTemplateOutletContext]="{ $implicit: current.community }"
        />
      }
      @case ('numbers') {
        @if (current.community.ghost; as ghost) {
          @if (ghost.level === 'Low') {
            <div class="flex items-center gap-2.5 rounded-sm border border-rule bg-base px-3 py-2">
              <app-ghost-waves level="Low" [score]="1" size="box" />
              <span class="text-small text-ink-2">
                <span class="font-semibold text-ink">Ghost risk: Low.</span>
                No warning signs from other members so far.
              </span>
            </div>
          } @else {
            <div
              class="flex flex-col gap-2 rounded-sm border border-rule-strong bg-base px-3 py-2.5"
            >
              <div class="flex flex-wrap items-center gap-x-3 gap-y-1">
                <span
                  class="text-cell font-semibold whitespace-nowrap"
                  [class]="ghost.level === 'High' ? 'text-danger' : 'text-status-no-response'"
                >
                  Ghost risk: {{ ghost.level }}
                </span>
                <app-ghost-waves [level]="ghost.level" [score]="ghost.score" size="box" />
                <span class="ml-auto font-mono text-caption whitespace-nowrap text-ink-muted">
                  {{ ghost.score }} of {{ ghost.maxScore }}
                </span>
              </div>
              <p class="text-small text-ink">{{ hint(ghost.level) }}</p>
              <ul class="flex list-disc flex-col gap-1 pl-4.5 text-small text-ink-2">
                @for (reason of ghost.reasons; track reason.kind) {
                  <li>{{ reason.text }}</li>
                }
              </ul>
            </div>
          }
        }

        @if (current.missingGroup; as missing) {
          <p class="text-small text-ink-2">{{ missing }}</p>
        }
        <app-community-numbers [columns]="current.columns" [label]="tableLabel(current.columns)" />

        <p class="flex gap-2 text-caption text-ink-2">
          <app-icon name="info" [size]="14" class="mt-px shrink-0" />
          <span>
            Based on other members only. You are not counted. Numbers appear once at least
            {{ minimum }} other members have applied.
          </span>
        </p>
        <ng-container
          [ngTemplateOutlet]="companyLink"
          [ngTemplateOutletContext]="{ $implicit: current.community }"
        />
      }
    }

    <ng-template #companyLink let-community>
      @if (community.companyKey; as key) {
        <a
          [routerLink]="companyPath(key)"
          [queryParams]="companyQuery()"
          class="inline-flex items-center gap-1.5 self-start rounded-xs text-cell font-semibold text-link underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus"
        >
          All your applications at {{ company() }}
          <app-icon name="chevron-right" [size]="12" />
        </a>
      } @else {
        <p class="border-t border-dashed border-rule-strong pt-2 text-label text-ink-2">
          Company stats and the list of your applications at this company aren’t available here,
          because this application was added before companies were linked.
        </p>
      }
    </ng-template>
  `,
})
export class CommunitySection {
  private readonly api = inject(CommunityApi);

  readonly applicationId = input.required<string>();
  /** The application's row version: a new one (after Edit or Add update) loads the numbers again. */
  readonly version = input.required<number>();
  /** The company name as the user typed it, for the link to their applications there. */
  readonly company = input.required<string>();

  protected readonly minimum = COMMUNITY_MINIMUM_MEMBERS;
  protected readonly companyPath = companyPath;

  private readonly resource = rxResource({
    params: () => ({ id: this.applicationId(), version: this.version() }),
    stream: ({ params }) => this.api.forApplication(params.id),
  });

  protected readonly view = computed<SectionView>(() => {
    if (this.resource.status() === 'error') {
      return { kind: 'error' };
    }
    if (!this.resource.hasValue()) {
      return { kind: 'loading' };
    }
    const community = this.resource.value();
    const columns: NumbersColumn[] = [];
    if (community.posting) {
      columns.push({ heading: 'This posting', numbers: community.posting });
    }
    if (community.company) {
      columns.push({ heading: 'This company', numbers: community.company });
    }
    if (columns.length === 0) {
      return { kind: 'not-enough', community };
    }
    return { kind: 'numbers', community, columns, missingGroup: missingGroup(community) };
  });

  protected hint = ghostHint;

  /** Lets the company page open this application again on "Back to Applications". */
  protected companyQuery(): Record<string, string> {
    return { [FROM_APPLICATION_PARAM]: this.applicationId() };
  }

  protected tableLabel(columns: readonly NumbersColumn[]): string {
    return `Stats from other members for ${columns.map((column) => column.heading?.toLowerCase()).join(' and ')}`;
  }

  protected retry(): void {
    this.resource.reload();
  }
}

// An application without a company key never has company numbers; the section explains that separately.
function missingGroup(community: ApplicationCommunity): string | null {
  if (!community.posting) {
    return `Fewer than ${COMMUNITY_MINIMUM_MEMBERS} other members applied to this exact posting, so only company numbers are shown.`;
  }
  if (!community.company && community.companyKey) {
    return `Fewer than ${COMMUNITY_MINIMUM_MEMBERS} other members applied to this company, so only posting numbers are shown.`;
  }
  return null;
}
