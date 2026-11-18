import { TimelineStampTone } from '../../../shared/ui/brand/timeline-stamp';
import { StatusGlyph } from '../../../shared/ui/status-label/status-label';
import { CHANNEL_LABELS, STATUS_DISPLAY, noResponseLabel } from '../application-display';
import { Application, IsoDate } from '../applications.models';

const DAY_MS = 24 * 60 * 60 * 1000;

// Status name colours; Applied and notes stay ink (docs/design.md §8). Full names so Tailwind finds them.
const TITLE_CLASSES: Record<TimelineStampTone, string> = {
  applied: 'text-ink',
  note: 'text-ink',
  'no-response': 'text-status-no-response',
  interview: 'text-status-interview',
  offer: 'text-status-offer',
  danger: 'text-danger',
};

// Applied and note glyphs keep their own colour while the title stays ink.
const GLYPH_CLASSES: Partial<Record<TimelineStampTone, string>> = {
  applied: 'text-status-applied',
  note: 'text-ink-2',
};

export interface TimelineRow {
  readonly key: string;
  readonly date: IsoDate;
  readonly tone: TimelineStampTone;
  readonly glyph: StatusGlyph;
  readonly title: string;
  /** Colour of the title line. */
  readonly titleClass: string;
  /** Colour of the glyph when it differs from the title. */
  readonly glyphClass: string;
  /** Text after the date on the second line, e.g. "Sent via Referral"; null for a bare status change. */
  readonly description: string | null;
}

/**
 * The timeline as the envelope shows it (docs/design.md §8, §14), newest first: every stored event, plus
 * the computed "No response · N days" row on the day the label started. That row is derived from dates
 * and never stored.
 */
export function timelineRows(application: Application): readonly TimelineRow[] {
  const rows = application.events.map((event): TimelineRow => {
    if (event.status === null) {
      return {
        key: event.id,
        date: event.occurredOn,
        tone: 'note',
        glyph: 'note',
        title: 'Note',
        ...colours('note'),
        description: event.note,
      };
    }

    const display = STATUS_DISPLAY[event.status];
    return {
      key: event.id,
      date: event.occurredOn,
      tone: display.tone,
      glyph: display.glyph,
      title: display.label,
      ...colours(display.tone),
      description: event.isSubmission
        ? `Sent via ${CHANNEL_LABELS[application.channel]}`
        : event.note,
    };
  });

  const noResponse = noResponseRow(application);
  if (noResponse === null) {
    return rows;
  }

  // Events are newest first; the computed row goes before the first event that is not later than it.
  const index = rows.findIndex((row) => row.date <= noResponse.date);
  return index === -1
    ? [...rows, noResponse]
    : [...rows.slice(0, index), noResponse, ...rows.slice(index)];
}

/** The date of the latest event: the postmark's date. */
export function lastActivityOn(application: Application): IsoDate | null {
  return application.events.reduce<IsoDate | null>(
    (latest, event) => (latest === null || event.occurredOn > latest ? event.occurredOn : latest),
    null,
  );
}

function noResponseRow(application: Application): TimelineRow | null {
  const since = application.noResponseSince;
  const lastActivity = lastActivityOn(application);
  if (since === null || lastActivity === null) {
    return null;
  }

  const threshold = Math.round((Date.parse(since) - Date.parse(lastActivity)) / DAY_MS);
  return {
    key: 'no-response',
    date: since,
    tone: 'no-response',
    glyph: 'clock',
    title: noResponseLabel(threshold),
    ...colours('no-response'),
    description: `Marked automatically after ${threshold} days`,
  };
}

function colours(tone: TimelineStampTone): Pick<TimelineRow, 'titleClass' | 'glyphClass'> {
  return { titleClass: TITLE_CLASSES[tone], glyphClass: GLYPH_CLASSES[tone] ?? '' };
}
