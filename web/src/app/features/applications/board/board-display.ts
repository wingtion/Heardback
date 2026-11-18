import { ConnectedPosition } from '@angular/cdk/overlay';
import { ApplicationStatus } from '../applications.models';

/** Glyph colour per column (docs/design.md §3); full class names so Tailwind finds them. */
export const STATUS_GLYPH_COLOURS: Readonly<Record<ApplicationStatus, string>> = {
  Applied: 'text-status-applied',
  Interview: 'text-status-interview',
  Offer: 'text-status-offer',
  Rejected: 'text-danger',
};

/** Phone and tablet cards show "12 Nov"; the desktop board shows the year too (handoff B4). */
export const BOARD_MOBILE_DATE_FORMAT = 'dd MMM';

/** Under the Move to… button, right edges aligned; above it when there is no room below. */
export const MOVE_MENU_POSITIONS: ConnectedPosition[] = [
  { originX: 'end', originY: 'bottom', overlayX: 'end', overlayY: 'top', offsetY: 4 },
  { originX: 'end', originY: 'top', overlayX: 'end', overlayY: 'bottom', offsetY: -4 },
];

/** Marks a column element so a drag can tell which column the pointer is over. */
export const BOARD_STATUS_ATTRIBUTE = 'data-board-status';

/** "No applications in Offer". */
export function emptyColumnTitle(label: string): string {
  return `No applications in ${label}`;
}

/** Read out after a move, since the card leaves the column the user was in. */
export function movedAnnouncement(company: string, label: string): string {
  return `Moved ${company} to ${label}.`;
}
