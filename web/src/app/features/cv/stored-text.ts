import { PERSONAL_DETAIL_MARKERS } from './cv.models';

export interface TextPart {
  readonly text: string;
  /** A "[email]"-style marker where a personal detail was removed. */
  readonly marker: boolean;
}

const MARKER_PATTERN = new RegExp(
  `(${PERSONAL_DETAIL_MARKERS.map((marker) => marker.replace(/[[\]]/g, '\\$&')).join('|')})`,
);

/** Splits the stored text so the markers can be shown apart from the user's own words. */
export function storedTextParts(text: string): readonly TextPart[] {
  return text
    .split(MARKER_PATTERN)
    .filter((part) => part !== '')
    .map((part) => ({
      text: part,
      marker: (PERSONAL_DETAIL_MARKERS as readonly string[]).includes(part),
    }));
}
