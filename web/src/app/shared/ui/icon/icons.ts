/**
 * The icon set from docs/design.md §6, as path data taken from the design handoff.
 * Circles and rectangles are written as paths so every icon renders the same way.
 */
export interface IconDefinition {
  readonly viewBox: number;
  readonly strokeWidth: number;
  readonly paths: readonly string[];
}

const DEFAULT_VIEW_BOX = 16;
const DEFAULT_STROKE = 1.5;

const icon = (
  paths: readonly string[],
  strokeWidth = DEFAULT_STROKE,
  viewBox = DEFAULT_VIEW_BOX,
): IconDefinition => ({ viewBox, strokeWidth, paths });

const CIRCLE_R6 = 'M2 8a6 6 0 1 0 12 0a6 6 0 1 0-12 0';

export const ICONS = {
  plus: icon(['M8 3v10M3 8h10'], 1.75),
  trash: icon(['M2.5 4.5h11M6 4.5V3h4v1.5M4 4.5l.7 9h6.6l.7-9M6.8 7v4.5M9.2 7v4.5']),
  close: icon(['M4 4l8 8M12 4l-8 8']),
  search: icon(['M2.5 7a4.5 4.5 0 1 0 9 0a4.5 4.5 0 1 0-9 0', 'M10.5 10.5L14 14']),
  'chevron-down': icon(['M2 3.5l3 3 3-3'], DEFAULT_STROKE, 10),
  'chevron-left': icon(['M7.5 2.5L4 6l3.5 3.5'], DEFAULT_STROKE, 12),
  'chevron-right': icon(['M4.5 2.5L8 6 4.5 9.5'], DEFAULT_STROKE, 12),
  calendar: icon(
    [
      'M3.5 3.5h9a1 1 0 0 1 1 1v8a1 1 0 0 1-1 1h-9a1 1 0 0 1-1-1v-8a1 1 0 0 1 1-1z',
      'M2.5 6.5h11M5.5 2v3M10.5 2v3',
    ],
    1.4,
  ),
  copy: icon(
    [
      'M6 5h6.5a1 1 0 0 1 1 1v6.5a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1z',
      'M11 5V3.5a1 1 0 0 0-1-1H3.5a1 1 0 0 0-1 1V10a1 1 0 0 0 1 1H5',
    ],
    1.4,
  ),
  check: icon(['M3.5 8.5l3 3 6-7'], 1.8),
  warning: icon(['M8 2.5l6 10.5H2z', 'M8 6.6v3M8 11.4v.1']),
  info: icon([CIRCLE_R6, 'M8 7.2v4M8 4.8v.1']),
  clock: icon([CIRCLE_R6, 'M8 4.8V8l2.2 1.5']),
  menu: icon(['M3 6h14M3 10h14M3 14h14'], 1.6, 20),
  mail: icon(
    ['M3 4h10a1 1 0 0 1 1 1v7a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1z', 'M2 5l6 4.5L14 5'],
    1.4,
  ),
} as const satisfies Record<string, IconDefinition>;

export type IconName = keyof typeof ICONS;
