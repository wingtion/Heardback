import { BoardColumn } from '../applications.models';
import { application, applicationItem } from '../applications.testing';
import { findCard, moveCard, replaceCard, restoreCard, toListItem } from './board-columns';

const NOW = new Date('2026-11-24T09:00:00Z');

function board(): readonly BoardColumn[] {
  return [
    {
      status: 'Applied',
      totalCount: 5,
      items: [
        applicationItem({ id: 'a', status: 'Applied' }),
        applicationItem({ id: 'b', status: 'Applied', noResponseDays: 14 }),
      ],
    },
    { status: 'Interview', totalCount: 1, items: [applicationItem({ id: 'c' })] },
    { status: 'Offer', totalCount: 0, items: [] },
    { status: 'Rejected', totalCount: 0, items: [] },
  ];
}

function ids(columns: readonly BoardColumn[]): Record<string, string[]> {
  return Object.fromEntries(
    columns.map((column) => [column.status, column.items.map((item) => item.id)]),
  );
}

describe('board columns', () => {
  it('moveCard_ToAnotherStatus_GoesToTheTopAndTotalsFollow', () => {
    const columns = board();
    const card = findCard(columns, 'b')!.card;

    const moved = moveCard(columns, card, 'Interview', NOW);

    expect(ids(moved)).toEqual({ Applied: ['a'], Interview: ['b', 'c'], Offer: [], Rejected: [] });
    expect(moved.map((column) => column.totalCount)).toEqual([4, 2, 0, 0]);
    expect(moved[1].items[0]).toEqual(
      expect.objectContaining({
        status: 'Interview',
        noResponseDays: null,
        updatedAt: NOW.toISOString(),
      }),
    );
  });

  it('restoreCard_AfterAFailedMove_PutsTheOriginalBackInPlace', () => {
    const columns = board();
    const place = findCard(columns, 'b')!;
    const moved = moveCard(columns, place.card, 'Offer', NOW);

    const restored = restoreCard(moved, place);

    expect(ids(restored)).toEqual(ids(columns));
    expect(restored.map((column) => column.totalCount)).toEqual([5, 1, 0, 0]);
    expect(restored[0].items[1]).toBe(place.card);
  });

  it('replaceCard_ServerVersion_SwapsInPlaceWithoutChangingTotals', () => {
    const columns = board();
    const saved = applicationItem({ id: 'a', status: 'Applied', role: 'Renamed role' });

    const replaced = replaceCard(columns, saved);

    expect(replaced[0].items[0].role).toBe('Renamed role');
    expect(replaced.map((column) => column.totalCount)).toEqual([5, 1, 0, 0]);
  });

  it('replaceCard_UnknownCard_LeavesTheBoardAlone', () => {
    const columns = board();

    expect(replaceCard(columns, applicationItem({ id: 'zzz' }))).toBe(columns);
  });

  it('toListItem_Application_KeepsTheCardFields', () => {
    const detail = application({ noResponseDays: 3, status: 'Applied' });

    expect(toListItem(detail)).toEqual({
      id: detail.id,
      company: detail.company,
      role: detail.role,
      channel: detail.channel,
      appliedOn: detail.appliedOn,
      status: 'Applied',
      noResponseDays: 3,
      updatedAt: detail.updatedAt,
    });
  });
});
