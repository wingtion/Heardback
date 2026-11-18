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
  clipboard: icon(
    ['M4 3.5h8a1 1 0 0 1 1 1v9a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1v-9a1 1 0 0 1 1-1z', 'M6 3.5V2.5h4v1'],
    1.4,
  ),
  check: icon(['M3.5 8.5l3 3 6-7'], 1.8),
  warning: icon(['M8 2.5l6 10.5H2z', 'M8 6.6v3M8 11.4v.1']),
  info: icon([CIRCLE_R6, 'M8 7.2v4M8 4.8v.1']),
  clock: icon([CIRCLE_R6, 'M8 4.8V8l2.2 1.5']),
  menu: icon(['M3 6h14M3 10h14M3 14h14'], 1.6, 20),
  move: icon(['M2.5 5.5h9M9 3l2.5 2.5L9 8M13.5 10.5h-9M7 8l-2.5 2.5L7 13'], 1.4),
  list: icon(['M2.5 4h11M2.5 8h11M2.5 12h11'], 1.4),
  board: icon(
    [
      'M2.5 2.5H5a.5.5 0 0 1 .5.5v10a.5.5 0 0 1-.5.5H2.5a.5.5 0 0 1-.5-.5V3a.5.5 0 0 1 .5-.5z',
      'M6.75 2.5h2.5a.5.5 0 0 1 .5.5v6.5a.5.5 0 0 1-.5.5h-2.5a.5.5 0 0 1-.5-.5V3a.5.5 0 0 1 .5-.5z',
      'M11 2.5h2.5a.5.5 0 0 1 .5.5v8a.5.5 0 0 1-.5.5H11a.5.5 0 0 1-.5-.5V3a.5.5 0 0 1 .5-.5z',
    ],
    1.4,
  ),
  // Six dots as zero-length strokes with round caps: the handoff's 1.2 px circles in a stroke icon.
  grip: icon(['M6 3h0M10 3h0M6 8h0M10 8h0M6 13h0M10 13h0'], 2.4),
  refresh: icon(['M13 8a5 5 0 1 1-1.5-3.6', 'M13 2.5v2.6h-2.6']),
  mail: icon(
    ['M3 4h10a1 1 0 0 1 1 1v7a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1z', 'M2 5l6 4.5L14 5'],
    1.4,
  ),
} as const satisfies Record<string, IconDefinition>;

export type IconName = keyof typeof ICONS;
