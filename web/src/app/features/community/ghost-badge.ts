import { Component, computed, input } from '@angular/core';
import { GHOST_MAX_SCORE, GhostLevel } from './community.models';
import { GhostWaves } from './ghost-waves';

// Full class names so Tailwind finds them in the source.
const LEVEL_CLASSES: Readonly<
  Record<GhostLevel, { readonly border: string; readonly text: string }>
> = {
  High: { border: 'border-danger', text: 'text-danger' },
  Medium: { border: 'border-status-no-response', text: 'text-status-no-response' },
  Low: { border: 'border-rule-strong', text: 'text-ink-muted' },
};

/**
 * The ghost-risk badge in lists (docs/design.md §18): dashed border in the level colour, the small wave meter,
 * "Ghost risk" and the level. Callers only show it for Medium and High. Screen readers also hear the score.
 */
@Component({
  selector: 'app-ghost-badge',
  imports: [GhostWaves],
  host: {
    '[class]':
      '"relative inline-flex h-5.5 items-center gap-1.25 rounded-sm border border-dashed pr-1.75 pl-1.5 text-caption whitespace-nowrap text-ink-2 " + classes().border',
  },
  template: `
    <app-ghost-waves [level]="level()" [score]="score()" />
    <span>
      Ghost risk <span [class]="'font-semibold ' + classes().text">{{ level() }}</span><span
        class="sr-only"
        >, {{ score() }} of {{ maxScore }}</span
      >
    </span>
  `,
})
export class GhostBadge {
  readonly level = input.required<GhostLevel>();
  readonly score = input.required<number>();

  protected readonly maxScore = GHOST_MAX_SCORE;
  protected readonly classes = computed(() => LEVEL_CLASSES[this.level()]);
}
