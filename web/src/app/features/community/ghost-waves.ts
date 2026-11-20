import { Component, computed, input } from '@angular/core';
import { GHOST_MAX_SCORE, GhostLevel } from './community.models';

/** "badge" is the 27 × 10 meter in list badges; "box" the 84 × 14 one in the detail (docs/design.md §18). */
export type GhostWavesSize = 'badge' | 'box';

interface WavesGeometry {
  readonly width: number;
  readonly height: number;
  readonly strokeWidth: number;
  readonly paths: readonly string[];
}

const WAVES: Readonly<Record<GhostWavesSize, WavesGeometry>> = {
  badge: {
    width: 27,
    height: 10,
    strokeWidth: 1.4,
    paths: [1, 6.4, 11.8, 17.2, 22.6].map((x) => `M${x} 6.5c1-2.8 2.6-2.8 3.6 0`),
  },
  box: {
    width: 84,
    height: 14,
    strokeWidth: 1.6,
    paths: [1, 18, 35, 52, 69].map(
      (x, index) => `M${x} 7c3-4 6-4 9 0s6 4 ${index === GHOST_MAX_SCORE - 1 ? 5 : 7} 0`,
    ),
  },
};

// Full class names so Tailwind finds them in the source.
const LEVEL_STROKE: Readonly<Record<GhostLevel, string>> = {
  High: 'stroke-danger',
  Medium: 'stroke-status-no-response',
  Low: 'stroke-ink-muted',
};

/**
 * The five-wave ghost-risk meter: the first `score` waves in the level colour, the rest `rule-strong`.
 * Decorative; the level and score are always given as text next to it.
 */
@Component({
  selector: 'app-ghost-waves',
  host: { class: 'inline-flex shrink-0' },
  template: `
    <svg
      aria-hidden="true"
      focusable="false"
      fill="none"
      stroke-linecap="round"
      [attr.width]="geometry().width"
      [attr.height]="geometry().height"
      [attr.viewBox]="'0 0 ' + geometry().width + ' ' + geometry().height"
      [attr.stroke-width]="geometry().strokeWidth"
    >
      @for (path of geometry().paths; track $index) {
        <path [attr.d]="path" [class]="$index < score() ? levelStroke() : 'stroke-rule-strong'" />
      }
    </svg>
  `,
})
export class GhostWaves {
  readonly level = input.required<GhostLevel>();
  readonly score = input.required<number>();
  readonly size = input<GhostWavesSize>('badge');

  protected readonly geometry = computed(() => WAVES[this.size()]);
  protected readonly levelStroke = computed(() => LEVEL_STROKE[this.level()]);
}
