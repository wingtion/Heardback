import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { WeeklyCount } from './insights.models';

const HEIGHT = 96;
// Each week gets an equal cell with the bar centred in it, matching the 12-column count row below.
const CELL = 16;
const MIN_BAR = 2;

/**
 * Applications per week as plain SVG bars (no chart library, decided 2026-11-19; docs/design.md §17). One
 * series, so no legend: neutral bars with the current week in the accent, each with a native tooltip, and the
 * same numbers in a visually hidden table for screen readers.
 */
@Component({
  selector: 'app-weekly-chart',
  imports: [DatePipe],
  host: { class: 'flex flex-col gap-1.5' },
  template: `
    <svg
      class="h-24 w-full"
      [attr.viewBox]="'0 0 ' + width() + ' ' + height"
      preserveAspectRatio="none"
      aria-hidden="true"
    >
      <line
        x1="0"
        [attr.x2]="width()"
        [attr.y1]="height - 0.5"
        [attr.y2]="height - 0.5"
        class="stroke-rule"
        stroke-width="1"
      />
      @for (bar of bars(); track bar.week.weekStart) {
        <rect
          [attr.x]="bar.x"
          [attr.y]="height - bar.height"
          [attr.width]="barWidth"
          [attr.height]="bar.height"
          rx="1"
          [class]="
            bar.current ? 'fill-accent' : bar.week.applications ? 'fill-rule-strong' : 'fill-rule'
          "
        >
          <title>
            {{ bar.week.applications }}
            {{ bar.week.applications === 1 ? 'application' : 'applications' }}, week of
            {{ bar.week.weekStart | date: 'd MMM' }}
          </title>
        </rect>
      }
    </svg>
    <!-- One cell per week, the same cells the bars sit in, so each count stands under its bar. -->
    <div
      class="grid grid-cols-12 text-center font-mono text-caption text-ink-2 tabular-nums"
      aria-hidden="true"
    >
      @for (week of weeks(); track week.weekStart) {
        <span>{{ week.applications }}</span>
      }
    </div>
    <div class="flex justify-between font-mono text-caption text-ink-muted" aria-hidden="true">
      <span>{{ first()?.weekStart | date: 'd MMM' }}</span>
      <span>This week</span>
    </div>
    <table class="sr-only">
      <caption>
        Applications per week
      </caption>
      <tr>
        <th scope="col">Week of</th>
        <th scope="col">Applications</th>
      </tr>
      @for (week of weeks(); track week.weekStart) {
        <tr>
          <td>{{ week.weekStart | date: 'd MMM y' }}</td>
          <td>{{ week.applications }}</td>
        </tr>
      }
    </table>
  `,
})
export class WeeklyChart {
  readonly weeks = input.required<readonly WeeklyCount[]>();

  protected readonly height = HEIGHT;
  protected readonly barWidth = 8;

  protected readonly width = computed(() => this.weeks().length * CELL);
  protected readonly first = computed(() => this.weeks().at(0));

  protected readonly bars = computed(() => {
    const weeks = this.weeks();
    const most = Math.max(1, ...weeks.map((week) => week.applications));
    return weeks.map((week, index) => ({
      week,
      x: index * CELL + (CELL - this.barWidth) / 2,
      // Empty weeks keep a thin stub on the baseline, so the gap reads as "none", not "missing".
      height: Math.max(MIN_BAR, (week.applications / most) * (HEIGHT - 8)),
      current: index === weeks.length - 1,
    }));
  });
}
