import {
  Application,
  ApplicationListItem,
  ApplicationStatus,
  BoardColumn,
} from '../applications.models';

/** Where a card sits, so a failed move can put it back exactly there. */
export interface CardPlace {
  readonly card: ApplicationListItem;
  readonly index: number;
}

export function findCard(columns: readonly BoardColumn[], id: string): CardPlace | null {
  for (const column of columns) {
    const index = column.items.findIndex((item) => item.id === id);
    if (index !== -1) {
      return { card: column.items[index], index };
    }
  }
  return null;
}

/** Takes the card out of its column; the column's total goes down with it. */
export function removeCard(columns: readonly BoardColumn[], id: string): readonly BoardColumn[] {
  return columns.map((column) =>
    column.items.some((item) => item.id === id)
      ? {
          ...column,
          totalCount: column.totalCount - 1,
          items: column.items.filter((item) => item.id !== id),
        }
      : column,
  );
}

/** Puts the card into the column of its status at `index`; the column's total goes up with it. */
export function insertCard(
  columns: readonly BoardColumn[],
  card: ApplicationListItem,
  index: number,
): readonly BoardColumn[] {
  return columns.map((column) =>
    column.status === card.status
      ? {
          ...column,
          totalCount: column.totalCount + 1,
          items: [...column.items.slice(0, index), card, ...column.items.slice(index)],
        }
      : column,
  );
}

/**
 * The board right after a move, before the server answers: the card leaves its column and goes to the top
 * of the new one, because the board shows the most recently changed first. A move adds a status event dated
 * today, so "No response" no longer applies.
 */
export function moveCard(
  columns: readonly BoardColumn[],
  card: ApplicationListItem,
  status: ApplicationStatus,
  now: Date,
): readonly BoardColumn[] {
  const moved: ApplicationListItem = {
    ...card,
    status,
    noResponseDays: null,
    updatedAt: now.toISOString(),
  };
  return insertCard(removeCard(columns, card.id), moved, 0);
}

/** A failed move: the card goes back to where it was, with its old status and label. */
export function restoreCard(
  columns: readonly BoardColumn[],
  place: CardPlace,
): readonly BoardColumn[] {
  return insertCard(removeCard(columns, place.card.id), place.card, place.index);
}

/** Swaps in the server's version of a card in place, e.g. after a move was saved. */
export function replaceCard(
  columns: readonly BoardColumn[],
  card: ApplicationListItem,
): readonly BoardColumn[] {
  const place = findCard(columns, card.id);
  if (place === null) {
    return columns;
  }
  return insertCard(removeCard(columns, card.id), card, place.index);
}

export function toListItem(application: Application): ApplicationListItem {
  return {
    id: application.id,
    company: application.company,
    role: application.role,
    channel: application.channel,
    appliedOn: application.appliedOn,
    status: application.status,
    noResponseDays: application.noResponseDays,
    updatedAt: application.updatedAt,
  };
}
