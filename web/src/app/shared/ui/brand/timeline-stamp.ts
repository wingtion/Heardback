import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { StatusTone } from '../status-label/status-label';

/** A status colour, or "note" (ink-2) for a note-only update. */
export type TimelineStampTone = StatusTone | 'note';

// Full class names so Tailwind finds them in the source.
const TONES: Record<TimelineStampTone, string> = {
  applied: 'border-status-applied text-status-applied',
  'no-response': 'border-status-no-response text-status-no-response',
  interview: 'border-status-interview text-status-interview',
  offer: 'border-status-offer text-status-offer',
  danger: 'border-danger text-danger',
  note: 'border-ink-2 text-ink-2',
};

/**
 * The dated stamp beside a timeline event (docs/design.md §8): 2 px dotted frame and a 1 px inner border
 * in the event's colour, dashed for "No response" and notes, month over day. Decorative; the date is
 * repeated in the event text.
 */
@Component({
  selector: 'app-timeline-stamp',
  imports: [DatePipe],
  host: {
    '[class]': '"block h-11.5 w-10.5 shrink-0 border-2 border-dotted bg-base p-0.5 " + toneClass()',
    'aria-hidden': 'true',
  },
  template: `
    <span
      class="flex h-full flex-col items-center justify-center border font-mono leading-none"
      [class]="dashed() ? 'border-dashed' : 'border-solid'"
    >
      <span class="text-stamp tracking-stamp uppercase">{{ date() | date: 'MMM' : 'UTC' }}</span>
      <span class="mt-0.75 text-ui-lg font-medium max-lg:text-body">
        {{ date() | date: 'dd' : 'UTC' }}
      </span>
    </span>
  `,
})
export class TimelineStamp {
  /** Calendar date, "2026-11-21". */
  readonly date = input.required<string>();
  readonly tone = input.required<TimelineStampTone>();

  protected readonly toneClass = computed(() => TONES[this.tone()]);
  protected readonly dashed = computed(
    () => this.tone() === 'no-response' || this.tone() === 'note',
  );
}
