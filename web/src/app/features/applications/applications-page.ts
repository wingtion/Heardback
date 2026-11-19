import { BreakpointObserver } from '@angular/cdk/layout';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { Component, Signal, computed, inject, signal } from '@angular/core';
import { rxResource, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { debounceTime, map } from 'rxjs';
import { DESKTOP_MEDIA_QUERY, WIDE_MEDIA_QUERY } from '../../core/breakpoints';
import { ShellMobileAction } from '../../shell/shell-actions';
import { Banner } from '../../shared/ui/banner/banner';
import { Button } from '../../shared/ui/button/button';
import { DialogLauncher } from '../../shared/ui/dialog/dialog-launcher';
import { Select } from '../../shared/ui/field/select';
import { Icon } from '../../shared/ui/icon/icon';
import { Pagination } from '../../shared/ui/pagination/pagination';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { StatusLabel } from '../../shared/ui/status-label/status-label';
import { ToolbarControl, ToolbarField } from '../../shared/ui/toolbar-field/toolbar-field';
import { AddApplicationDialog } from './add-application-dialog/add-application-dialog';
import { ApplicationDetail } from './application-detail/application-detail';
import { ApplicationsBoard } from './board/applications-board';
import { ApplicationsEmptyState } from './empty-state/applications-empty-state';
import { ApplicationsView, ApplicationsViewToggle } from './view-toggle/applications-view-toggle';
import {
  APPLICATION_DATE_FORMAT,
  CHANNELS,
  CHANNEL_LABELS,
  STATUS_FILTERS,
  applicationsSummary,
  daysAgo,
  statusDisplay,
} from './application-display';
import { ApplicationsApi } from './applications-api';
import { ApplicationsSummaryStore } from './applications-summary';
import { SampleDataButton } from './sample-data/sample-data-button';
import {
  APPLICATION_LIMITS,
  ApplicationChannel,
  ApplicationFilter,
  ApplicationListItem,
  ApplicationListStatus,
  ApplicationPage,
  ApplicationStatus,
  BoardFilter,
} from './applications.models';

/** The selected application's id, in the query string so the browser's Back closes the detail. */
export const SELECTED_PARAM = 'id';

/** "board" in the query string shows the board; the list is the default. */
export const VIEW_PARAM = 'view';

/** Opens the list filtered by status; only read when the page opens. */
export const STATUS_PARAM = 'status';
const BOARD_VIEW: ApplicationsView = 'board';

const SEARCH_DELAY_MS = 300;
const SKELETON_ROWS = 4;

type ListView =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'empty' }
  | { readonly kind: 'no-matches' }
  | { readonly kind: 'list'; readonly data: ApplicationPage };

/**
 * The applications page (docs/design.md §8–10, §14): heading with counts and the List / Board toggle, search
 * and filters, then either the list (a table from the lg breakpoint, a list below it, paged 20 at a time) or
 * the board. The selected application opens as a panel beside them from the xl breakpoint and as a
 * full-screen sheet below it.
 */
@Component({
  selector: 'app-applications-page',
  imports: [
    DatePipe,
    NgTemplateOutlet,
    RouterLink,
    ApplicationDetail,
    ApplicationsBoard,
    ApplicationsEmptyState,
    ApplicationsViewToggle,
    SampleDataButton,
    Banner,
    Button,
    Icon,
    Pagination,
    Select,
    ShellMobileAction,
    Spinner,
    StatusLabel,
    ToolbarControl,
    ToolbarField,
  ],
  templateUrl: './applications-page.html',
  host: { class: 'flex min-h-full xl:h-full' },
})
export class ApplicationsPage {
  private readonly api = inject(ApplicationsApi);
  private readonly dialogs = inject(DialogLauncher);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly summaryStore = inject(ApplicationsSummaryStore);

  protected readonly statusFilters = STATUS_FILTERS;
  protected readonly channels = CHANNELS;
  protected readonly channelLabels = CHANNEL_LABELS;
  protected readonly dateFormat = APPLICATION_DATE_FORMAT;
  protected readonly searchMaxLength = APPLICATION_LIMITS.search;
  protected readonly skeletonRows = Array.from({ length: SKELETON_ROWS }, (_, index) => index);
  protected readonly selectedParam = SELECTED_PARAM;
  protected readonly statusOf = statusDisplay;

  protected readonly search = signal('');
  // A link can open the list filtered, e.g. Insights' "See all" with ?status=NoResponse.
  protected readonly status = signal<ApplicationListStatus | null>(
    STATUS_FILTERS.find(
      (option) => option.value === this.route.snapshot.queryParamMap.get(STATUS_PARAM),
    )?.value ?? null,
  );
  protected readonly channel = signal<ApplicationChannel | null>(null);
  protected readonly page = signal(1);

  // Typing waits a moment before searching; the other filters apply at once.
  private readonly debouncedSearch = toSignal(
    toObservable(this.search).pipe(debounceTime(SEARCH_DELAY_MS)),
    { initialValue: '' },
  );

  private readonly filter = computed<ApplicationFilter>(() => ({
    search: this.debouncedSearch(),
    status: this.status(),
    channel: this.channel(),
  }));

  // A new page or filter cancels the previous request, so a slow response cannot win.
  protected readonly view = toSignal(
    this.route.queryParamMap.pipe(
      map((params): ApplicationsView => (params.get(VIEW_PARAM) === BOARD_VIEW ? 'board' : 'list')),
    ),
    { initialValue: 'list' },
  );

  /** Search and channel apply to the board too; its columns are the statuses. */
  protected readonly boardFilter = computed<BoardFilter>(() => ({
    search: this.debouncedSearch(),
    channel: this.channel(),
  }));

  protected readonly boardFiltered = computed(
    () => this.debouncedSearch().trim() !== '' || this.channel() !== null,
  );

  /** Bumped to reload the board, and the open detail after a board move. */
  protected readonly boardRevision = signal(0);
  protected readonly detailRevision = signal(0);

  // Only the visible view loads; switching back to the list fetches it fresh.
  private readonly applications = rxResource({
    params: () =>
      this.view() === 'list' ? { page: this.page(), filter: this.filter() } : undefined,
    stream: ({ params }) => this.api.list(params.page, params.filter),
  });

  protected readonly hasFilters = computed(() => {
    const filter = this.filter();
    return filter.search.trim() !== '' || filter.status !== null || filter.channel !== null;
  });

  // A reload (after adding or deleting) keeps the rows on screen; a page or filter change shows loading.
  protected readonly listView = computed<ListView>(() => {
    const status = this.applications.status();
    if (status === 'error') {
      return { kind: 'error' };
    }
    if (status === 'loading' || !this.applications.hasValue()) {
      return { kind: 'loading' };
    }
    const data = this.applications.value();
    if (data.items.length > 0) {
      return { kind: 'list', data };
    }
    return this.hasFilters() ? { kind: 'no-matches' } : { kind: 'empty' };
  });

  protected readonly loading = this.applications.isLoading;

  protected readonly summary = computed(() => {
    const summary = this.summaryStore.summary();
    return summary ? applicationsSummary(summary) : null;
  });

  protected readonly selectedId = toSignal(
    this.route.queryParamMap.pipe(map((params) => params.get(SELECTED_PARAM))),
    { initialValue: null },
  );

  private readonly breakpoints = inject(BreakpointObserver);

  /** From xl the detail is a panel beside the list; below it a full-screen sheet. */
  protected readonly wide = this.matches(WIDE_MEDIA_QUERY);
  /** From lg the filters say "All statuses"; on phones just "All" (docs/design.md §8). */
  protected readonly desktop = this.matches(DESKTOP_MEDIA_QUERY);

  protected readonly now = new Date();

  protected updatedAgo(item: ApplicationListItem): string {
    return daysAgo(item.updatedAt, this.now);
  }

  protected setSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
  }

  protected setStatus(value: string): void {
    this.status.set(STATUS_FILTERS.find((option) => option.value === value)?.value ?? null);
    this.page.set(1);
  }

  protected setChannel(value: string): void {
    this.channel.set(CHANNELS.find((channel) => channel === value) ?? null);
    this.page.set(1);
  }

  protected clearFilters(): void {
    this.search.set('');
    this.status.set(null);
    this.channel.set(null);
    this.page.set(1);
  }

  protected goToPage(page: number): void {
    this.page.set(page);
  }

  protected retry(): void {
    this.applications.reload();
  }

  protected select(id: string): void {
    this.navigate({ [SELECTED_PARAM]: id });
  }

  protected closeDetail(): void {
    this.navigate({ [SELECTED_PARAM]: null });
  }

  protected setView(view: ApplicationsView): void {
    this.navigate({ [VIEW_PARAM]: view === BOARD_VIEW ? BOARD_VIEW : null });
  }

  /** "see all in the list" under a full board column: the list, filtered to that status when given. */
  protected seeAllInList(status: ApplicationStatus | null): void {
    this.status.set(status);
    this.page.set(1);
    this.setView('list');
  }

  /** A board move changed the application: counts and the open detail are out of date. */
  protected onBoardMoved(id: string): void {
    this.summaryStore.reload();
    if (id === this.selectedId()) {
      this.detailRevision.update((revision) => revision + 1);
    }
  }

  protected onKeydownRow(event: Event, id: string): void {
    event.preventDefault();
    this.select(id);
  }

  protected openAddDialog(): void {
    const ref = this.dialogs.open(AddApplicationDialog, { mobile: 'full-screen' });
    const dialog = ref.componentInstance;
    dialog?.created.subscribe((application) => {
      this.refresh();
      this.select(application.id);
    });
    dialog?.viewTracked.subscribe((id) => this.select(id));
  }

  /** Add update or Edit in the detail: the row or card and the counts may have changed. */
  protected onDetailChanged(): void {
    this.refresh();
  }

  protected onDeleted(): void {
    this.refresh();
    this.closeDetail();
  }

  private navigate(queryParams: Record<string, string | null>): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge',
    });
  }

  private matches(query: string): Signal<boolean> {
    return toSignal(this.breakpoints.observe(query).pipe(map((state) => state.matches)), {
      initialValue: this.breakpoints.isMatched(query),
    });
  }

  protected samplesAdded(): void {
    this.refresh();
  }

  // New and changed applications sort first, so the first page is where they show up.
  private refresh(): void {
    this.summaryStore.reload();
    if (this.view() === BOARD_VIEW) {
      this.boardRevision.update((revision) => revision + 1);
    } else if (this.page() === 1) {
      this.applications.reload();
    } else {
      this.page.set(1);
    }
  }
}
