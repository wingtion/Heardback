import {
  CdkDrag,
  CdkDragEnd,
  CdkDragMove,
  CdkDragPlaceholder,
  CdkDragPreview,
  CdkDragStart,
  CdkDropList,
} from '@angular/cdk/drag-drop';
import { CdkMenu, CdkMenuItem, CdkMenuTrigger } from '@angular/cdk/menu';
import { DOCUMENT, DatePipe, NgTemplateOutlet } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  booleanAttribute,
  computed,
  effect,
  inject,
  input,
  linkedSignal,
  output,
  signal,
  untracked,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { httpErrorOf, problemCode } from '../../../core/http-errors';
import { Banner } from '../../../shared/ui/banner/banner';
import { Button } from '../../../shared/ui/button/button';
import { DialogLauncher } from '../../../shared/ui/dialog/dialog-launcher';
import { Icon } from '../../../shared/ui/icon/icon';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { StatusGlyphIcon } from '../../../shared/ui/status-label/status-glyph';
import { StatusLabel } from '../../../shared/ui/status-label/status-label';
import {
  APPLICATION_DATE_FORMAT,
  CHANNEL_LABELS,
  STATUS_DISPLAY,
  localToday,
  statusDisplay,
} from '../application-display';
import { CommunityBadgesStore } from '../../community/community-badges';
import { showsBadge } from '../../community/community-display';
import { CommunityBadge } from '../../community/community.models';
import { GhostBadge } from '../../community/ghost-badge';
import { ApplicationsApi } from '../applications-api';
import {
  APPLICATION_ERROR_CODES,
  APPLICATION_STATUSES,
  ApplicationListItem,
  ApplicationStatus,
  BoardColumn,
  BoardFilter,
} from '../applications.models';
import { ApplicationsEmptyState } from '../empty-state/applications-empty-state';
import {
  CardPlace,
  findCard,
  moveCard,
  replaceCard,
  restoreCard,
  toListItem,
} from './board-columns';
import {
  BOARD_MOBILE_DATE_FORMAT,
  BOARD_STATUS_ATTRIBUTE,
  MOVE_MENU_POSITIONS,
  STATUS_GLYPH_COLOURS,
  emptyColumnTitle,
  movedAnnouncement,
} from './board-display';
import { MoveToSheet, MoveToSheetData } from './move-to-sheet';

type BoardView = 'loading' | 'error' | 'empty' | 'no-matches' | 'board';

interface Point {
  readonly x: number;
  readonly y: number;
}

/** Skeleton cards per column while the board loads: 3 / 2 / 1 / 1 (docs/design.md §14). */
const SKELETON_COLUMNS: readonly (readonly number[])[] = [3, 2, 1, 1].map((count) =>
  Array.from({ length: count }, (_, index) => index),
);

const TAB_KEYS: Readonly<Record<string, number>> = { ArrowLeft: -1, ArrowRight: 1 };

/**
 * The board view (docs/design.md §14). From the lg breakpoint: four columns, a card's "Move to…" menu, and
 * dragging a card to another column. Below it: one column at a time behind status tabs, and "Move to…"
 * opens a bottom sheet. A move shows at once and adds a status event dated today; if saving fails the card
 * goes back with an inline alert and Try again.
 */
@Component({
  selector: 'app-applications-board',
  imports: [
    CdkDrag,
    CdkDragPlaceholder,
    CdkDragPreview,
    CdkDropList,
    CdkMenu,
    CdkMenuItem,
    CdkMenuTrigger,
    DatePipe,
    NgTemplateOutlet,
    RouterLink,
    ApplicationsEmptyState,
    Banner,
    Button,
    GhostBadge,
    Icon,
    Spinner,
    StatusGlyphIcon,
    StatusLabel,
  ],
  templateUrl: './applications-board.html',
  host: { class: 'flex flex-col', '(document:keydown.escape)': 'cancelDrag()' },
})
export class ApplicationsBoard {
  private readonly api = inject(ApplicationsApi);
  private readonly dialogs = inject(DialogLauncher);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly badges = inject(CommunityBadgesStore);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly filter = input.required<BoardFilter>();
  /** Search or channel is set, so an empty board means "nothing matches". */
  readonly filtered = input(false, { transform: booleanAttribute });
  /** From the lg breakpoint: columns and dragging; below it: tabs. */
  readonly desktop = input.required<boolean>();
  /** Bumped by the page after an add, edit, update or delete, so the board reloads. */
  readonly revision = input(0);
  readonly selectedId = input<string | null>(null);

  /** A move was saved; the id of the application that changed. */
  readonly moved = output<string>();
  /** "see all in the list": the status to filter the list by, or null for no status filter. */
  readonly seeAll = output<ApplicationStatus | null>();
  readonly add = output();
  readonly clearFilters = output();

  protected readonly statusLabels = STATUS_DISPLAY;
  protected readonly glyphColours = STATUS_GLYPH_COLOURS;
  protected readonly channelLabels = CHANNEL_LABELS;
  protected readonly dateFormat = APPLICATION_DATE_FORMAT;
  protected readonly mobileDateFormat = BOARD_MOBILE_DATE_FORMAT;
  protected readonly menuPositions = MOVE_MENU_POSITIONS;
  protected readonly statusAttribute = BOARD_STATUS_ATTRIBUTE;
  protected readonly skeletonColumns = SKELETON_COLUMNS;
  protected readonly statuses = APPLICATION_STATUSES;
  protected readonly statusOf = statusDisplay;
  protected readonly emptyTitle = emptyColumnTitle;

  /** The ghost-risk badge of a card; Medium and High only (docs/design.md §18). */
  protected badgeFor(id: string): CommunityBadge | null {
    const badge = this.badges.badgeFor(id);
    return showsBadge(badge) ? badge : null;
  }

  // A new filter cancels the previous request, so a slow response cannot win.
  private readonly resource = rxResource({
    params: () => this.filter(),
    stream: ({ params }) => this.api.board(params),
  });

  /** The columns on screen: the server's, plus moves that are not saved yet. */
  protected readonly columns = linkedSignal<readonly BoardColumn[]>(() =>
    this.resource.hasValue() ? this.resource.value().columns : [],
  );

  protected readonly view = computed<BoardView>(() => {
    const status = this.resource.status();
    if (status === 'error') {
      return 'error';
    }
    if (status === 'loading' || !this.resource.hasValue()) {
      return 'loading';
    }
    if (this.columns().every((column) => column.totalCount === 0)) {
      return this.filtered() ? 'no-matches' : 'empty';
    }
    return 'board';
  });

  /** Mobile: the column shown under the tabs. */
  protected readonly selectedTab = signal<ApplicationStatus>('Applied');
  protected readonly selectedColumn = computed(
    () => this.columns().find((column) => column.status === this.selectedTab()) ?? null,
  );

  /** Moves waiting for the server; those cards cannot be moved again meanwhile. */
  protected readonly pending = signal<ReadonlySet<string>>(new Set());
  /** Cards whose last move failed, with the status it was going to, for Try again. */
  protected readonly failed = signal<ReadonlyMap<string, ApplicationStatus>>(new Map());
  protected readonly announcement = signal('');

  /** The card being dragged and the column it would land in, if not its own. */
  protected readonly dragging = signal<ApplicationListItem | null>(null);
  protected readonly dropTarget = signal<ApplicationStatus | null>(null);
  private dragCancelled = false;
  private dragByMouse = false;

  constructor() {
    let first = true;
    effect(() => {
      this.revision();
      if (first) {
        first = false;
        return;
      }
      untracked(() => this.resource.reload());
    });
  }

  protected retryLoad(): void {
    this.resource.reload();
  }

  protected targetsFor(status: ApplicationStatus): readonly ApplicationStatus[] {
    return APPLICATION_STATUSES.filter((target) => target !== status);
  }

  protected moveFromMenu(card: ApplicationListItem, status: ApplicationStatus): void {
    this.move(card, status);
    // The card is drawn again in its new column; keep the keyboard user on its Move to… button.
    this.focusAfterRender(`[data-move-button="${card.id}"]`);
  }

  protected openMoveSheet(card: ApplicationListItem): void {
    const data: MoveToSheetData = { company: card.company, role: card.role, status: card.status };
    const ref = this.dialogs.open(MoveToSheet, { data });
    ref.componentInstance?.chosen.subscribe((status) => {
      this.move(card, status);
      // The card leaves this tab, and with it the button focus would return to.
      this.focusAfterRender('[role="tab"][aria-selected="true"]');
    });
  }

  protected retryMove(card: ApplicationListItem): void {
    const status = this.failed().get(card.id);
    if (status) {
      this.move(card, status);
    }
  }

  protected selectTab(status: ApplicationStatus): void {
    this.selectedTab.set(status);
  }

  protected onTabKeydown(event: KeyboardEvent): void {
    const index = APPLICATION_STATUSES.indexOf(this.selectedTab());
    const last = APPLICATION_STATUSES.length - 1;
    let next: number;
    if (event.key in TAB_KEYS) {
      next =
        (index + TAB_KEYS[event.key] + APPLICATION_STATUSES.length) % APPLICATION_STATUSES.length;
    } else if (event.key === 'Home') {
      next = 0;
    } else if (event.key === 'End') {
      next = last;
    } else {
      return;
    }
    event.preventDefault();
    this.selectedTab.set(APPLICATION_STATUSES[next]);
    this.focusAfterRender('[role="tab"][aria-selected="true"]');
  }

  protected onDragStarted(event: CdkDragStart<ApplicationListItem>): void {
    this.dragCancelled = false;
    this.dragByMouse = !event.event.type.startsWith('touch');
    this.dragging.set(event.source.data);
  }

  protected onDragMoved(event: CdkDragMove<ApplicationListItem>): void {
    const target = this.columnAt(clientPoint(event.event));
    this.dropTarget.set(target === event.source.data.status ? null : target);
  }

  protected onDragEnded(event: CdkDragEnd<ApplicationListItem>): void {
    const card = event.source.data;
    const target = this.dragCancelled ? null : this.columnAt(clientPoint(event.event));
    this.dragging.set(null);
    this.dropTarget.set(null);
    if (target !== null && target !== card.status) {
      this.move(card, target);
    }
  }

  /** Escape during a drag puts the card back. CDK has no cancel, so end the drag where it is. */
  protected cancelDrag(): void {
    if (this.dragging() === null) {
      return;
    }
    this.dragCancelled = true;
    this.dropTarget.set(null);
    if (this.dragByMouse) {
      this.document.dispatchEvent(new MouseEvent('mouseup'));
    }
  }

  private move(card: ApplicationListItem, status: ApplicationStatus): void {
    if (card.status === status || this.pending().has(card.id)) {
      return;
    }
    const place = findCard(this.columns(), card.id);
    if (place === null) {
      return;
    }

    this.setFailed(card.id, null);
    this.setPending(card.id, true);
    this.columns.update((columns) => moveCard(columns, place.card, status, new Date()));
    this.announcement.set(movedAnnouncement(card.company, STATUS_DISPLAY[status].label));

    this.api
      .addEvent(card.id, { status, occurredOn: localToday(new Date()), note: null })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (application) => {
          this.setPending(card.id, false);
          this.columns.update((columns) => replaceCard(columns, toListItem(application)));
          this.moved.emit(card.id);
        },
        error: (error: unknown) => {
          this.setPending(card.id, false);
          this.moveFailed(error, place, status);
        },
      });
  }

  private moveFailed(error: unknown, place: CardPlace, status: ApplicationStatus): void {
    const response = httpErrorOf(error);
    // Deleted, or already moved, elsewhere: this board is out of date and Try again cannot help.
    if (
      response?.status === HttpStatusCode.NotFound ||
      problemCode(response?.error) === APPLICATION_ERROR_CODES.sameStatus
    ) {
      this.announcement.set('');
      this.resource.reload();
      return;
    }
    this.columns.update((columns) => restoreCard(columns, place));
    this.setFailed(place.card.id, status);
    this.announcement.set('');
  }

  private columnAt(point: Point | null): ApplicationStatus | null {
    if (point === null) {
      return null;
    }
    const column = this.document
      .elementFromPoint(point.x, point.y)
      ?.closest(`[${BOARD_STATUS_ATTRIBUTE}]`)
      ?.getAttribute(BOARD_STATUS_ATTRIBUTE);
    return APPLICATION_STATUSES.find((status) => status === column) ?? null;
  }

  private setPending(id: string, pending: boolean): void {
    this.pending.update((ids) => {
      const next = new Set(ids);
      if (pending) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }

  private setFailed(id: string, status: ApplicationStatus | null): void {
    this.failed.update((moves) => {
      const next = new Map(moves);
      if (status === null) {
        next.delete(id);
      } else {
        next.set(id, status);
      }
      return next;
    });
  }

  private focusAfterRender(selector: string): void {
    afterNextRender(() => this.host.nativeElement.querySelector<HTMLElement>(selector)?.focus(), {
      injector: this.injector,
    });
  }
}

/** Where the pointer is in the viewport, for elementFromPoint. */
function clientPoint(event: MouseEvent | TouchEvent): Point | null {
  if ('changedTouches' in event) {
    const touch = event.changedTouches.item(0) ?? event.touches.item(0);
    return touch ? { x: touch.clientX, y: touch.clientY } : null;
  }
  return { x: event.clientX, y: event.clientY };
}
