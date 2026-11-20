import { DatePipe } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import { Component, computed, inject } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { httpErrorOf } from '../../core/http-errors';
import { Banner } from '../../shared/ui/banner/banner';
import { EnvelopeIllustration } from '../../shared/ui/brand/envelope-illustration';
import { Button } from '../../shared/ui/button/button';
import { Icon } from '../../shared/ui/icon/icon';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { StatusLabel } from '../../shared/ui/status-label/status-label';
import { SHELL_ROUTES } from '../../shell/nav-items';
import {
  APPLICATION_DATE_FORMAT,
  StatusDisplay,
  daysAgo,
  statusDisplay,
} from '../applications/application-display';
import { CommunityApi } from './community-api';
import { showsBadge } from './community-display';
import { CommunityNumbersTable } from './community-numbers';
import { COMPANY_KEY_PARAM, FROM_APPLICATION_PARAM } from './community-routes';
import {
  COMMUNITY_MINIMUM_MEMBERS,
  CompanyApplication,
  CompanyCommunity,
  GhostRating,
} from './community.models';
import { GhostBadge } from './ghost-badge';

/** "10 Nov" beside the role in the phone list (docs/design.md §18). */
const SHORT_DATE_FORMAT = 'dd MMM';

const SKELETON_ROWS = [0, 1, 2] as const;

type PageView =
  | { readonly kind: 'loading' }
  | { readonly kind: 'not-found' }
  | { readonly kind: 'error' }
  | { readonly kind: 'loaded'; readonly company: CompanyCommunity };

interface OwnRow {
  readonly item: CompanyApplication;
  readonly status: StatusDisplay;
  readonly ghost: GhostRating | null;
  readonly updated: string;
}

/**
 * The company page (docs/design.md §18, ADR-0013): the user's own applications at one company and other
 * members' numbers for it. Only reached from the user's own applications; there is no company search or list.
 */
@Component({
  selector: 'app-company-page',
  imports: [
    DatePipe,
    RouterLink,
    Banner,
    Button,
    CommunityNumbersTable,
    EnvelopeIllustration,
    GhostBadge,
    Icon,
    Spinner,
    StatusLabel,
  ],
  templateUrl: './company-page.html',
  host: { class: 'flex flex-col gap-4 p-4 lg:gap-5 lg:px-7 lg:py-5.5' },
})
export class CompanyPage {
  private readonly api = inject(CommunityApi);
  private readonly route = inject(ActivatedRoute);

  protected readonly applicationsPath = SHELL_ROUTES.applications;
  protected readonly dateFormat = APPLICATION_DATE_FORMAT;
  protected readonly shortDateFormat = SHORT_DATE_FORMAT;
  protected readonly minimum = COMMUNITY_MINIMUM_MEMBERS;
  protected readonly skeletonRows = SKELETON_ROWS;

  private readonly companyKey = toSignal(
    this.route.paramMap.pipe(map((params) => params.get(COMPANY_KEY_PARAM) ?? '')),
    { initialValue: '' },
  );

  private readonly fromApplication = toSignal(
    this.route.queryParamMap.pipe(map((params) => params.get(FROM_APPLICATION_PARAM))),
    { initialValue: null },
  );

  /** "Back to Applications" opens the application this page was opened from again. */
  protected readonly backQuery = computed(() => {
    const id = this.fromApplication();
    return id ? { id } : null;
  });

  private readonly resource = rxResource({
    params: () => ({ key: this.companyKey() }),
    stream: ({ params }) => this.api.forCompany(params.key),
  });

  protected readonly view = computed<PageView>(() => {
    if (this.resource.status() === 'error') {
      const status = httpErrorOf(this.resource.error())?.status;
      return status === HttpStatusCode.NotFound ? { kind: 'not-found' } : { kind: 'error' };
    }
    return this.resource.hasValue()
      ? { kind: 'loaded', company: this.resource.value() }
      : { kind: 'loading' };
  });

  protected readonly rows = computed<readonly OwnRow[]>(() => {
    const view = this.view();
    if (view.kind !== 'loaded') {
      return [];
    }
    const now = new Date();
    return view.company.applications.map((item) => ({
      item,
      status: statusDisplay(item.status, item.noResponseDays),
      ghost: showsBadge(item.ghost) ? item.ghost : null,
      updated: daysAgo(item.updatedAt, now),
    }));
  });

  protected retry(): void {
    this.resource.reload();
  }
}
