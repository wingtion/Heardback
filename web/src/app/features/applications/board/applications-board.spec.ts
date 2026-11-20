import { provideCommunityQuiet } from '../../community/community.testing';
import { CdkDragEnd } from '@angular/cdk/drag-drop';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { localToday } from '../application-display';
import { APPLICATIONS_API } from '../applications-api';
import { ApplicationListItem, ApplicationStatus, Board, BoardFilter } from '../applications.models';
import { application, applicationItem } from '../applications.testing';
import { ApplicationsBoard } from './applications-board';

const BOARD_URL = `${APPLICATIONS_API}/board`;

@Component({
  imports: [ApplicationsBoard],
  template: `
    <app-applications-board
      [filter]="filter()"
      [filtered]="filtered()"
      [desktop]="desktop()"
      [revision]="revision()"
      (moved)="moved.push($event)"
      (seeAll)="seeAll.push($event)"
    />
  `,
})
class BoardHost {
  readonly filter = signal<BoardFilter>({ search: '', channel: null });
  readonly filtered = signal(false);
  readonly desktop = signal(true);
  readonly revision = signal(0);
  readonly moved: string[] = [];
  readonly seeAll: (ApplicationStatus | null)[] = [];
}

function boardOf(...cards: ApplicationListItem[]): Board {
  const statuses: ApplicationStatus[] = ['Applied', 'Interview', 'Offer', 'Rejected'];
  return {
    columns: statuses.map((status) => {
      const items = cards.filter((card) => card.status === status);
      return { status, totalCount: items.length, items };
    }),
  };
}

const LARKSPUR = applicationItem({
  id: 'larkspur',
  company: 'Larkspur Labs',
  role: 'Backend Engineering Intern',
  status: 'Applied',
});
const BRIGHTLINE = applicationItem({
  id: 'brightline',
  company: 'Brightline Payments',
  status: 'Applied',
  noResponseDays: 14,
});
const NORTHWIND = applicationItem({ id: 'northwind', status: 'Interview' });

describe('ApplicationsBoard', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        ...provideCommunityQuiet(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    document.querySelectorAll('.cdk-overlay-container').forEach((element) => element.remove());
    http.verify();
  });

  async function render(): Promise<void> {
    TestBed.tick();
    await new Promise((resolve) => setTimeout(resolve));
    TestBed.tick();
  }

  async function create(
    body: Board | null = boardOf(LARKSPUR, BRIGHTLINE, NORTHWIND),
    setup: (host: BoardHost) => void = () => undefined,
  ): Promise<{ host: BoardHost; element: HTMLElement }> {
    const fixture = TestBed.createComponent(BoardHost);
    setup(fixture.componentInstance);
    await render();
    if (body) {
      boardRequest().flush(body);
      await render();
    }
    return { host: fixture.componentInstance, element: fixture.nativeElement as HTMLElement };
  }

  function boardRequest(): TestRequest {
    return http.expectOne((request) => request.url === BOARD_URL);
  }

  function eventRequest(id: string): TestRequest {
    return http.expectOne(`${APPLICATIONS_API}/${id}/events`);
  }

  function cardsIn(element: HTMLElement, status: ApplicationStatus): string[] {
    return Array.from(
      element.querySelectorAll(`[data-board-status="${status}"] article`),
      (card) => card.getAttribute('aria-label') ?? '',
    );
  }

  function button(container: ParentNode, name: string): HTMLButtonElement {
    const found = Array.from(container.querySelectorAll('button')).find(
      (candidate) =>
        candidate.textContent?.trim() === name || candidate.getAttribute('aria-label') === name,
    );
    if (!found) throw new Error(`No button "${name}"`);
    return found;
  }

  async function moveWithMenu(element: HTMLElement, company: string, to: string): Promise<void> {
    button(element, `Move ${company} to…`).click();
    await render();
    const overlay = document.querySelector('.cdk-overlay-container');
    if (!overlay) throw new Error('No menu');
    button(overlay, to).click();
    await render();
  }

  it('load_InFlight_ShowsLoadingState', async () => {
    const { element } = await create(null);

    expect(element.querySelector('[role="status"]:not(.sr-only)')?.textContent).toContain(
      'Loading your board…',
    );
    boardRequest().flush(boardOf());
  });

  it('load_WithCards_ShowsFourColumnsWithTotalsAndNoResponseLabels', async () => {
    const { element } = await create();

    const headings = Array.from(element.querySelectorAll('h2'), (h) =>
      h.textContent?.replace(/\s+/g, ' ').trim(),
    );
    expect(headings).toEqual(['Applied 2', 'Interview 1', 'Offer 0', 'Rejected 0']);
    expect(cardsIn(element, 'Applied')).toEqual([
      'Larkspur Labs, Backend Engineering Intern',
      'Brightline Payments, Backend Engineer (.NET)',
    ]);
    expect(element.textContent).toContain('No response · 14 days');
    expect(element.textContent).toContain('No applications in Offer');
  });

  it('load_FilterSet_SendsSearchAndChannel', async () => {
    await create(null, (host) => host.filter.set({ search: ' north ', channel: 'Referral' }));

    const request = boardRequest();
    expect(request.request.params.get('search')).toBe('north');
    expect(request.request.params.get('channel')).toBe('Referral');
    request.flush(boardOf());
  });

  it('load_ServerDown_ShowsErrorAndTryAgainReloads', async () => {
    const { element } = await create(null);
    boardRequest().flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(element.textContent).toContain('Couldn’t load your board');
    button(element, 'Try again').click();
    await render();
    boardRequest().flush(boardOf(NORTHWIND));
    await render();

    expect(cardsIn(element, 'Interview')).toHaveLength(1);
  });

  it('load_NoApplications_ShowsTheEmptyState', async () => {
    const { element } = await create(boardOf());

    expect(element.textContent).toContain('No applications yet');
  });

  it('load_FilteredWithoutResults_ShowsNoMatches', async () => {
    const { element } = await create(boardOf(), (host) => host.filtered.set(true));

    expect(element.textContent).toContain('No matching applications');
  });

  it('load_ColumnOverTheLimit_ShowsTheCountAndSeeAllOpensTheList', async () => {
    const full = boardOf(LARKSPUR, NORTHWIND);
    const columns = full.columns.map((column) => ({ ...column, totalCount: 340 }));
    const { host, element } = await create({ columns });

    expect(element.textContent).toContain('Showing 1 of 340');
    const seeAll = Array.from(element.querySelectorAll('button')).filter(
      (candidate) => candidate.textContent?.trim() === 'see all in the list',
    );
    seeAll[0].click();
    seeAll[1].click();

    // Applied also holds the "No response" cards, so its list has no status filter.
    expect(host.seeAll).toEqual([null, 'Interview']);
  });

  it('revision_Bumped_ReloadsTheBoard', async () => {
    const { host } = await create();

    host.revision.set(1);
    await render();

    boardRequest().flush(boardOf());
  });

  it('moveWithMenu_Saved_MovesAtOnceAndAddsAnEventDatedToday', async () => {
    const { host, element } = await create();

    await moveWithMenu(element, 'Larkspur Labs', 'Offer');

    expect(cardsIn(element, 'Offer')).toEqual(['Larkspur Labs, Backend Engineering Intern']);
    expect(element.querySelector('.sr-only')?.textContent).toBe('Moved Larkspur Labs to Offer.');
    const request = eventRequest('larkspur');
    expect(request.request.body).toEqual({
      status: 'Offer',
      occurredOn: localToday(new Date()),
      note: null,
    });
    request.flush(application({ id: 'larkspur', status: 'Offer', company: 'Larkspur Labs' }));
    await render();

    expect(host.moved).toEqual(['larkspur']);
    expect(cardsIn(element, 'Offer')).toHaveLength(1);
  });

  it('moveWithMenu_OffersOnlyTheOtherStatuses', async () => {
    const { element } = await create();

    button(element, 'Move Northwind Logistics to…').click();
    await render();

    const items = Array.from(
      document.querySelectorAll('[role="menuitem"]'),
      (item) => item.textContent?.trim() ?? '',
    );
    expect(items).toEqual(['Applied', 'Offer', 'Rejected']);
  });

  it('move_ServerError_PutsTheCardBackWithAnAlertAndTryAgainRetries', async () => {
    const { host, element } = await create();
    await moveWithMenu(element, 'Brightline Payments', 'Rejected');

    eventRequest('brightline').flush(null, { status: 500, statusText: 'Server Error' });
    await render();

    expect(cardsIn(element, 'Applied')).toHaveLength(2);
    expect(cardsIn(element, 'Rejected')).toEqual([]);
    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Couldn’t move this application. Try again.',
    );
    expect(element.textContent).toContain('No response · 14 days');

    button(element, 'Try again').click();
    await render();
    expect(eventRequest('brightline').request.body).toEqual(
      expect.objectContaining({ status: 'Rejected' }),
    );
    expect(cardsIn(element, 'Rejected')).toHaveLength(1);
    expect(element.querySelector('[role="alert"]')).toBeNull();
    expect(host.moved).toEqual([]);
  });

  it('move_DeletedElsewhere_ReloadsTheBoardInsteadOfOfferingTryAgain', async () => {
    const { element } = await create();
    await moveWithMenu(element, 'Larkspur Labs', 'Interview');

    eventRequest('larkspur').flush(
      { code: 'applications.not_found' },
      { status: 404, statusText: 'Not Found' },
    );
    await render();

    boardRequest().flush(boardOf(BRIGHTLINE, NORTHWIND));
    await render();
    expect(element.querySelector('[role="alert"]')).toBeNull();
    expect(cardsIn(element, 'Applied')).toEqual(['Brightline Payments, Backend Engineer (.NET)']);
  });

  describe('dragging', () => {
    let elementFromPoint: typeof document.elementFromPoint;

    beforeEach(() => {
      elementFromPoint = document.elementFromPoint;
    });

    afterEach(() => {
      document.elementFromPoint = elementFromPoint;
    });

    function pointAt(element: HTMLElement, status: ApplicationStatus): void {
      const column = element.querySelector(`[data-board-status="${status}"] h2`);
      document.elementFromPoint = () => column;
    }

    function dragEnd(card: ApplicationListItem): CdkDragEnd<ApplicationListItem> {
      return {
        source: { data: card },
        event: new MouseEvent('mouseup', { clientX: 10, clientY: 10 }),
      } as unknown as CdkDragEnd<ApplicationListItem>;
    }

    function board(element: HTMLElement): ApplicationsBoard {
      const debug = element.querySelector('app-applications-board');
      if (!debug) throw new Error('No board');
      return (
        window as unknown as { ng: { getComponent(el: Element): ApplicationsBoard } }
      ).ng.getComponent(debug);
    }

    it('dragEnded_OverAnotherColumn_MovesTheCard', async () => {
      const { element } = await create();
      pointAt(element, 'Interview');

      board(element)['onDragEnded'](dragEnd(LARKSPUR));
      await render();

      expect(cardsIn(element, 'Interview')[0]).toBe('Larkspur Labs, Backend Engineering Intern');
      eventRequest('larkspur').flush(application({ id: 'larkspur', status: 'Interview' }));
    });

    it('dragEnded_AfterEscape_LeavesTheCardWhereItWas', async () => {
      const { element } = await create();
      const component = board(element);
      pointAt(element, 'Interview');
      component['dragging'].set(LARKSPUR);

      document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
      component['onDragEnded'](dragEnd(LARKSPUR));
      await render();

      expect(cardsIn(element, 'Applied')).toContain('Larkspur Labs, Backend Engineering Intern');
    });
  });

  describe('phones and tablets', () => {
    function tabs(element: HTMLElement): HTMLButtonElement[] {
      return Array.from(element.querySelectorAll('[role="tab"]'));
    }

    it('render_Mobile_ShowsStatusTabsAndOneColumn', async () => {
      const { element } = await create(undefined, (host) => host.desktop.set(false));

      expect(tabs(element).map((tab) => tab.textContent?.replace(/\s+/g, ' ').trim())).toEqual([
        'Applied 2',
        'Interview 1',
        'Offer 0',
        'Rejected 0',
      ]);
      expect(tabs(element)[0].getAttribute('aria-selected')).toBe('true');
      expect(element.querySelectorAll('[role="tabpanel"] article')).toHaveLength(2);
    });

    it('tabKeydown_ArrowRight_SelectsTheNextTab', async () => {
      const { element } = await create(undefined, (host) => host.desktop.set(false));

      tabs(element)[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
      await render();

      expect(tabs(element)[1].getAttribute('aria-selected')).toBe('true');
      expect(element.querySelector('[role="tabpanel"]')?.textContent).toContain(
        'Northwind Logistics',
      );
    });

    it('selectTab_EmptyColumn_SaysHowToMoveCardsThere', async () => {
      const { element } = await create(undefined, (host) => host.desktop.set(false));

      tabs(element)[2].click();
      await render();

      expect(element.textContent).toContain('No applications in Offer');
      expect(element.textContent).toContain('Use Move to… on any card to move it here.');
    });

    it('moveSheet_StatusChosen_MovesTheCard', async () => {
      const { element } = await create(undefined, (host) => host.desktop.set(false));

      button(element, 'Move Larkspur Labs to…').click();
      await render();
      const sheet = document.querySelector('app-move-to-sheet');
      if (!sheet) throw new Error('No sheet');
      expect(sheet.textContent).toContain(
        'Larkspur Labs · Backend Engineering Intern · now in Applied',
      );
      button(sheet, 'Interview').click();
      await render();

      expect(tabs(element)[1].textContent).toContain('2');
      eventRequest('larkspur').flush(application({ id: 'larkspur', status: 'Interview' }));
    });
  });
});
